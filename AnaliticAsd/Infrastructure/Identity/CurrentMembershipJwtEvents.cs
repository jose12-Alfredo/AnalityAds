using System.Security.Claims;
using AnaliticAsd.Domain.Identity;
using AnaliticAsd.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AnaliticAsd.Infrastructure.Identity;

// Signature validity alone does not keep revoked memberships or changed roles valid.
public sealed class CurrentMembershipJwtEvents : JwtBearerEvents
{
    public override async Task TokenValidated(TokenValidatedContext context)
    {
        var principal = context.Principal;
        if (!Guid.TryParse(principal?.FindFirstValue(ClaimTypes.NameIdentifier), out var userId)
            || !Guid.TryParse(principal.FindFirstValue("agency_id"), out var agencyId)
            || !Enum.TryParse<AgencyRole>(principal.FindFirstValue(ClaimTypes.Role), out var role)
            || principal.FindFirstValue(ClaimTypes.Role) != role.ToString()
            || !Enum.IsDefined(role))
        {
            context.Fail("The authenticated membership is no longer available.");
            return;
        }
        var db = context.HttpContext.RequestServices.GetRequiredService<AnalitiAdsDbContext>();
        if (!await db.Memberships.AnyAsync(m => m.UserId == userId && m.AgencyId == agencyId && m.Role == role
                && db.Users.Any(u => u.Id == userId && u.IsActive)
                && db.Agencies.Any(a => a.Id == agencyId && a.IsActive), context.HttpContext.RequestAborted))
            context.Fail("The authenticated membership is no longer available.");
    }

    public override Task Challenge(JwtBearerChallengeContext context)
    {
        context.HandleResponse();
        context.Response.Headers.WWWAuthenticate = "Bearer";
        return Problem(context.HttpContext, 401, "Authentication required");
    }
    public override Task Forbidden(ForbiddenContext context) => Problem(context.HttpContext, 403, "Insufficient permissions");
    private static Task Problem(HttpContext context, int status, string title)
    {
        context.Response.StatusCode = status;
        return context.Response.WriteAsJsonAsync(new ProblemDetails { Status = status, Title = title, Instance = context.Request.Path },
            options: null, contentType: "application/problem+json", cancellationToken: context.RequestAborted);
    }
}
