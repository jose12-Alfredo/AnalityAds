using AnaliticAsd.Application.Common;
using AnaliticAsd.Application.ProviderIntegrations;
using AnaliticAsd.Contracts.ProviderIntegrations;
using AnaliticAsd.Domain.DataSources;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AnaliticAsd.Controllers;

[ApiController]
[Route("api/v1/integrations/google/{provider}")]
public sealed class GoogleIntegrationsController(IGoogleIntegrationService service) : ControllerBase
{
    [Authorize(Roles = "Owner,Admin")]
    [HttpGet("oauth/start")]
    public ActionResult<ProviderAuthorizationResponse> Start(string provider) =>
        Ok(new ProviderAuthorizationResponse(service.CreateAuthorizationUrl(Parse(provider))));

    [AllowAnonymous]
    [HttpGet("oauth/callback")]
    public async Task<IActionResult> Callback(string provider, [FromQuery] string? code, [FromQuery] string? state,
        [FromQuery] string? error, CancellationToken cancellationToken)
    {
        var parsed = Parse(provider);
        if (!string.IsNullOrWhiteSpace(error)) return Redirect(service.CreateFailureRedirect(parsed, error));
        if (string.IsNullOrWhiteSpace(code) || string.IsNullOrWhiteSpace(state))
            return BadRequest(new ProblemDetails { Status = 400, Title = "Invalid OAuth callback",
                Detail = "Google did not return the required code and state." });
        try { return Redirect(await service.CompleteAsync(parsed, code, state, cancellationToken)); }
        catch (ExternalServiceException) { return Redirect(service.CreateFailureRedirect(parsed, "exchange_failed")); }
    }

    [Authorize(Roles = "Owner,Admin,Analyst,Viewer")]
    [HttpGet("connection")]
    public async Task<ActionResult<ProviderConnectionResponse>> Status(string provider,
        CancellationToken cancellationToken) => Ok(Map(await service.StatusAsync(Parse(provider), cancellationToken)));

    [Authorize(Roles = "Owner,Admin,Analyst")]
    [HttpGet("sources")]
    public async Task<ActionResult<IReadOnlyList<ProviderSourceResponse>>> Sources(string provider,
        CancellationToken cancellationToken) => Ok((await service.DiscoverAsync(Parse(provider), cancellationToken))
        .Select(Map).ToArray());

    [Authorize(Roles = "Owner,Admin,Analyst")]
    [HttpPost("/api/v1/clients/{clientId:guid}/integrations/google/{provider}/sources")]
    public async Task<ActionResult<ProviderSourceResponse>> Associate(Guid clientId, string provider,
        AssociateProviderSourceRequest request, CancellationToken cancellationToken) =>
        Ok(Map(await service.AssociateAsync(Parse(provider), clientId, request.ExternalId, cancellationToken)));

    private static DataProvider Parse(string provider) => provider.ToLowerInvariant() switch
    {
        "google-ads" => DataProvider.GoogleAds,
        "ga4" => DataProvider.GoogleAnalytics4,
        _ => throw new ArgumentException("Provider must be 'google-ads' or 'ga4'.")
    };
    private static ProviderConnectionResponse Map(ProviderConnectionModel value) => new(value.Provider.ToString(),
        value.Status.ToString(), value.ExpiresAtUtc, value.LastSucceededAtUtc, value.LastErrorCode);
    private static ProviderSourceResponse Map(ProviderSourceModel value) => new(value.ExternalId, value.Name,
        value.Currency, value.TimeZone, value.SourceType.ToString(), value.IsAssigned, value.ClientId,
        value.IsManager, value.IsTest, value.DataSourceId);
}
