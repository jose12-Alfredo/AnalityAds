using AnaliticAsd.Application.Common;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;

namespace AnaliticAsd.ErrorHandling;

public sealed class ApiExceptionHandler(ILogger<ApiExceptionHandler> logger) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken)
    {
        var (statusCode, title) = exception switch
        {
            EntityNotFoundException => (StatusCodes.Status404NotFound, "Resource not found"),
            ConflictException => (StatusCodes.Status409Conflict, "Request conflict"),
            ForbiddenException => (StatusCodes.Status403Forbidden, "Insufficient permissions"),
            ArgumentException => (StatusCodes.Status400BadRequest, "Invalid request"),
            UnauthorizedAccessException => (StatusCodes.Status401Unauthorized, "Authentication failed"),
            ExternalServiceException => (StatusCodes.Status502BadGateway, "Meta service error"),
            ServiceConfigurationException => (StatusCodes.Status503ServiceUnavailable, "Service not configured"),
            _ => (StatusCodes.Status500InternalServerError, "Unexpected server error")
        };
        var isClientError = statusCode != StatusCodes.Status500InternalServerError;

        if (isClientError)
        {
            logger.LogInformation(
                "Request failed with status code {StatusCode}: {Message}",
                statusCode,
                exception.Message);
        }
        else
        {
            logger.LogError(exception, "An unhandled exception occurred.");
        }

        httpContext.Response.StatusCode = statusCode;
        await httpContext.Response.WriteAsJsonAsync(
            new ProblemDetails
            {
                Status = statusCode,
                Title = title,
                Detail = isClientError
                    ? exception.Message
                    : "The server could not complete the request.",
                Instance = httpContext.Request.Path
            },
            options: null, contentType: "application/problem+json", cancellationToken: cancellationToken);

        return true;
    }
}
