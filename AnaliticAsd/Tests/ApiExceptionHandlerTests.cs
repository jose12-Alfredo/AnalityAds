using System.Text.Json;
using AnaliticAsd.ErrorHandling;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging.Abstractions;

namespace AnaliticAsd.Tests;

public sealed class ApiExceptionHandlerTests
{
    [Fact]
    public async Task Argument_exception_returns_problem_details_with_bad_request()
    {
        var context = CreateContext();
        var handler = new ApiExceptionHandler(NullLogger<ApiExceptionHandler>.Instance);

        var handled = await handler.TryHandleAsync(
            context,
            new ArgumentException("Invalid value."),
            CancellationToken.None);

        Assert.True(handled);
        Assert.Equal(StatusCodes.Status400BadRequest, context.Response.StatusCode);
        var document = await ReadResponseAsync(context);
        Assert.Equal("Invalid request", document.RootElement.GetProperty("title").GetString());
        Assert.Equal("Invalid value.", document.RootElement.GetProperty("detail").GetString());
    }

    [Fact]
    public async Task Unexpected_exception_does_not_expose_internal_message()
    {
        var context = CreateContext();
        var handler = new ApiExceptionHandler(NullLogger<ApiExceptionHandler>.Instance);

        await handler.TryHandleAsync(
            context,
            new InvalidOperationException("Sensitive internal detail."),
            CancellationToken.None);

        Assert.Equal(StatusCodes.Status500InternalServerError, context.Response.StatusCode);
        var document = await ReadResponseAsync(context);
        var body = document.RootElement.ToString();
        Assert.DoesNotContain("Sensitive internal detail", body, StringComparison.Ordinal);
        Assert.Equal(
            "The server could not complete the request.",
            document.RootElement.GetProperty("detail").GetString());
    }

    private static DefaultHttpContext CreateContext()
    {
        var context = new DefaultHttpContext();
        context.Request.Path = "/test";
        context.Response.Body = new MemoryStream();
        return context;
    }

    private static async Task<JsonDocument> ReadResponseAsync(HttpContext context)
    {
        context.Response.Body.Position = 0;
        return await JsonDocument.ParseAsync(context.Response.Body);
    }
}
