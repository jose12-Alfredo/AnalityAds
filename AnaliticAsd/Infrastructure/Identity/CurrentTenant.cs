using System.Security.Claims;
using AnaliticAsd.Application.Identity;

namespace AnaliticAsd.Infrastructure.Identity;

public sealed class CurrentTenant(IHttpContextAccessor accessor) : ICurrentTenant
{
    private ClaimsPrincipal User => accessor.HttpContext?.User ?? throw new UnauthorizedAccessException("An authenticated user is required.");
    public Guid AgencyId => Parse("agency_id");
    public Guid UserId => Parse(ClaimTypes.NameIdentifier);
    public string Role => User.FindFirstValue(ClaimTypes.Role) ?? throw new UnauthorizedAccessException("The authenticated user has no role.");
    private Guid Parse(string type) => Guid.TryParse(User.FindFirstValue(type), out var value) ? value : throw new UnauthorizedAccessException("The authentication token is missing required tenant claims.");
}
