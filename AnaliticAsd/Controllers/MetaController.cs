using AnaliticAsd.Application.Meta;
using AnaliticAsd.Contracts.Meta;
using AnaliticAsd.Application.Common;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AnaliticAsd.Controllers;

[ApiController]
[Route("api/v1/meta")]
public sealed class MetaController(IMetaOAuthService service) : ControllerBase
{
    [Authorize(Roles = "Owner,Admin")]
    [HttpGet("oauth/start")]
    public ActionResult<MetaAuthorizationResponse> Start() => Ok(new MetaAuthorizationResponse(service.CreateAuthorizationUrl()));

    [AllowAnonymous]
    [HttpGet("oauth/callback")]
    public async Task<IActionResult> Callback([FromQuery] string? code, [FromQuery] string? state, [FromQuery] string? error, CancellationToken cancellationToken)
    {
        if (!string.IsNullOrWhiteSpace(error)) return Redirect(service.CreateFailureRedirect(error));
        if (string.IsNullOrWhiteSpace(code) || string.IsNullOrWhiteSpace(state)) return BadRequest(new ProblemDetails { Status = 400, Title = "Invalid OAuth callback", Detail = "Meta did not return the required code and state." });
        try
        {
            return Redirect(await service.CompleteAsync(code, state, cancellationToken));
        }
        catch (ExternalServiceException)
        {
            return Redirect(service.CreateFailureRedirect("exchange_failed"));
        }
    }

    [Authorize(Roles = "Owner,Admin,Analyst,Viewer")]
    [HttpGet("connection")]
    public async Task<ActionResult<MetaConnectionStatusResponse>> Status(CancellationToken cancellationToken)
    {
        var result = await service.GetStatusAsync(cancellationToken);
        return Ok(new MetaConnectionStatusResponse(result.IsConnected, result.ExpiresAtUtc));
    }

    [Authorize(Roles = "Owner,Admin,Analyst")]
    [HttpGet("ad-accounts")]
    public async Task<ActionResult<IReadOnlyList<MetaAccountResponse>>> Accounts(CancellationToken cancellationToken) =>
        Ok((await service.ListAccountsAsync(cancellationToken)).Select(Map).ToArray());

    [Authorize(Roles = "Owner,Admin,Analyst")]
    [HttpPost("/api/v1/clients/{clientId:guid}/meta-ad-accounts")]
    public async Task<ActionResult<MetaAccountResponse>> Associate(Guid clientId, AssociateMetaAccountRequest request, CancellationToken cancellationToken) =>
        Ok(Map(await service.AssociateAsync(clientId, request.MetaAccountId, cancellationToken)));

    private static MetaAccountResponse Map(MetaAccountModel value) => new(value.MetaAccountId, value.Name, value.Currency, value.TimeZone, value.AccountStatus, value.IsLinked, value.ClientId);
}
