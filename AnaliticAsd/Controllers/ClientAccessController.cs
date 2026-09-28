using AnaliticAsd.Application.Identity;
using AnaliticAsd.Contracts.Identity;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace AnaliticAsd.Controllers;

[ApiController]
[Authorize(Roles = "Owner,Admin")]
[Route("api/v1/clients/{clientId:guid}")]
public sealed class ClientAccessController(IClientAccessService service) : ControllerBase
{
    [HttpGet("users")]
    public async Task<ActionResult<IReadOnlyList<ClientAccessModel>>> Users(Guid clientId, CancellationToken ct) => Ok(await service.ListAccessAsync(clientId, ct));

    [HttpDelete("users/{userId:guid}")]
    public async Task<IActionResult> RevokeAccess(Guid clientId, Guid userId, CancellationToken ct)
    {
        await service.RevokeAccessAsync(clientId, userId, ct);
        return NoContent();
    }

    [HttpGet("invitations")]
    public async Task<ActionResult<IReadOnlyList<ClientInvitationModel>>> Invitations(Guid clientId, CancellationToken ct) => Ok(await service.ListInvitationsAsync(clientId, ct));

    [HttpPost("invitations")]
    public async Task<ActionResult<CreatedClientInvitationModel>> Invite(Guid clientId, CreateClientInvitationRequest request, CancellationToken ct) =>
        StatusCode(StatusCodes.Status201Created, await service.InviteAsync(clientId, request.Email, ct));

    [HttpDelete("invitations/{invitationId:guid}")]
    public async Task<IActionResult> RevokeInvitation(Guid clientId, Guid invitationId, CancellationToken ct)
    {
        await service.RevokeInvitationAsync(clientId, invitationId, ct);
        return NoContent();
    }

    [AllowAnonymous]
    [EnableRateLimiting("ClientInvitationAcceptance")]
    [HttpPost("/api/v1/auth/client-invitations/accept")]
    public async Task<ActionResult<AuthResponse>> Accept(AcceptClientInvitationRequest request, CancellationToken ct)
    {
        var auth = await service.AcceptAsync(new(request.InvitationToken, request.Email, request.Password), ct);
        return Ok(new AuthResponse(auth.AccessToken, auth.ExpiresAtUtc, auth.UserId, auth.Email, auth.AgencyId, auth.AgencyName, auth.Role.ToString()));
    }
}
