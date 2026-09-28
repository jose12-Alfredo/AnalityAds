using System.Security.Claims;
using AnaliticAsd.Application.Identity;
using AnaliticAsd.Domain.Identity;
using AnaliticAsd.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;

namespace AnaliticAsd.Infrastructure.Identity;

public sealed class McpJwtEvents : JwtBearerEvents
{
    public override async Task TokenValidated(TokenValidatedContext context)
    {
        var p = context.Principal; var now = context.HttpContext.RequestServices.GetRequiredService<TimeProvider>().GetUtcNow();
        if (!Guid.TryParse(p?.FindFirstValue(ClaimTypes.NameIdentifier), out var userId) || !Guid.TryParse(p.FindFirstValue("agency_id"), out var agencyId) ||
            !Guid.TryParse(p.FindFirstValue("connection_id"), out var connectionId) || p.FindFirstValue("scope")?.Split(' ').Contains(McpConnectionService.ReadScope) != true)
        { context.Fail("Invalid MCP credential."); return; }
        var db = context.HttpContext.RequestServices.GetRequiredService<AnalitiAdsDbContext>();
        var membership = await db.Memberships.AsNoTracking().SingleOrDefaultAsync(x => x.UserId == userId && x.AgencyId == agencyId, context.HttpContext.RequestAborted);
        var connection = await db.McpConnections.AsNoTracking().SingleOrDefaultAsync(x => x.Id == connectionId && x.UserId == userId && x.AgencyId == agencyId, context.HttpContext.RequestAborted);
        var valid = membership is not null && connection is { RevokedAtUtc: null } && connection.ExpiresAtUtc > now
            && await db.Users.AnyAsync(x => x.Id == userId && x.IsActive, context.HttpContext.RequestAborted) && await db.Agencies.AnyAsync(x => x.Id == agencyId && x.IsActive, context.HttpContext.RequestAborted);
        if (!valid) { context.Fail("MCP connection or membership is no longer active."); return; }
        var identity = (ClaimsIdentity)p.Identity!; identity.AddClaim(new Claim(ClaimTypes.Role, membership!.Role.ToString()));
    }
    public override Task Challenge(JwtBearerChallengeContext context)
    {
        context.Response.Headers.WWWAuthenticate = $"Bearer resource_metadata=\"{context.Request.Scheme}://{context.Request.Host}/.well-known/oauth-protected-resource/mcp\", scope=\"{McpConnectionService.ReadScope}\"";
        return Task.CompletedTask;
    }
}
