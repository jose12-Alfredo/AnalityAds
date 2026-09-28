using AnaliticAsd.Application.Common;
using AnaliticAsd.Application.ProviderIntegrations;
using AnaliticAsd.Contracts.ProviderIntegrations;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AnaliticAsd.Controllers;

[ApiController]
[Route("api/v1/integrations/tiktok-ads")]
public sealed class TikTokIntegrationsController(ITikTokIntegrationService service) : ControllerBase
{
    [Authorize(Roles = "Owner,Admin")]
    [HttpGet("oauth/start")]
    public ActionResult<ProviderAuthorizationResponse> Start() => Ok(new ProviderAuthorizationResponse(service.CreateAuthorizationUrl()));

    [AllowAnonymous]
    [HttpGet("oauth/callback")]
    public async Task<IActionResult> Callback([FromQuery] string? auth_code, [FromQuery] string? state,
        [FromQuery] string? error, CancellationToken ct)
    {
        if (!string.IsNullOrWhiteSpace(error)) return Redirect(service.CreateFailureRedirect(error));
        if (string.IsNullOrWhiteSpace(auth_code) || string.IsNullOrWhiteSpace(state)) return BadRequest(new ProblemDetails { Status = 400, Title = "Invalid OAuth callback" });
        try { return Redirect(await service.CompleteAsync(auth_code, state, ct)); }
        catch (ExternalServiceException) { return Redirect(service.CreateFailureRedirect("exchange_failed")); }
    }

    [Authorize(Roles = "Owner,Admin,Analyst,Viewer")]
    [HttpGet("connection")]
    public async Task<ActionResult<ProviderConnectionResponse>> Status(CancellationToken ct) => Ok(Map(await service.StatusAsync(ct)));

    [Authorize(Roles = "Owner,Admin,Analyst")]
    [HttpGet("sources")]
    public async Task<ActionResult<IReadOnlyList<ProviderSourceResponse>>> Sources(CancellationToken ct) => Ok((await service.DiscoverAsync(ct)).Select(Map));

    [Authorize(Roles = "Owner,Admin,Analyst")]
    [HttpPost("/api/v1/clients/{clientId:guid}/integrations/tiktok-ads/sources")]
    public async Task<ActionResult<ProviderSourceResponse>> Associate(Guid clientId, AssociateProviderSourceRequest request, CancellationToken ct) => Ok(Map(await service.AssociateAsync(clientId, request.ExternalId, ct)));

    private static ProviderConnectionResponse Map(ProviderConnectionModel x) => new(x.Provider.ToString(), x.Status.ToString(), x.ExpiresAtUtc, x.LastSucceededAtUtc, x.LastErrorCode);
    private static ProviderSourceResponse Map(ProviderSourceModel x) => new(x.ExternalId, x.Name, x.Currency, x.TimeZone, x.SourceType.ToString(), x.IsAssigned, x.ClientId, x.IsManager, x.IsTest, x.DataSourceId);
}
