using System.Security.Claims;
using AnaliticAsd.Application.Identity;
using AnaliticAsd.Contracts.Identity;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AnaliticAsd.Controllers;

[ApiController]
[Route("api/v1/auth")]
public sealed class AuthController(IAuthService authService) : ControllerBase
{
    [AllowAnonymous]
    [HttpPost("register")]
    public async Task<ActionResult<AuthResponse>> Register(RegisterRequest request, CancellationToken cancellationToken) =>
        Ok(Map(await authService.RegisterAsync(new(request.AgencyName, request.Email, request.Password), cancellationToken)));

    [AllowAnonymous]
    [HttpPost("login")]
    public async Task<ActionResult<AuthResponse>> Login(LoginRequest request, CancellationToken cancellationToken) =>
        Ok(Map(await authService.LoginAsync(new(request.Email, request.Password, request.AgencyId), cancellationToken)));

    [Authorize]
    [HttpGet("me")]
    public ActionResult<MeResponse> Me() => Ok(new MeResponse(
        Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!),
        User.FindFirstValue(ClaimTypes.Email) ?? string.Empty,
        Guid.Parse(User.FindFirstValue("agency_id")!),
        User.FindFirstValue("agency_name") ?? string.Empty,
        User.FindFirstValue(ClaimTypes.Role) ?? string.Empty));

    private static AuthResponse Map(AuthModel model) => new(model.AccessToken, model.ExpiresAtUtc, model.UserId, model.Email, model.AgencyId, model.AgencyName, model.Role.ToString());
}
