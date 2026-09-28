using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using AnaliticAsd.Application.Identity;
using AnaliticAsd.Domain.Identity;
using Microsoft.IdentityModel.Tokens;

namespace AnaliticAsd.Infrastructure.Identity;

public sealed class JwtTokenIssuer(IConfiguration configuration, TimeProvider timeProvider) : ITokenIssuer
{
    public (string Token, DateTimeOffset ExpiresAtUtc) Issue(User user, Agency agency, Membership membership)
    {
        var key = configuration["Jwt:SigningKey"] ?? throw new InvalidOperationException("Jwt:SigningKey is required.");
        if (Encoding.UTF8.GetByteCount(key) < 32) throw new InvalidOperationException("Jwt:SigningKey must contain at least 32 bytes.");
        var issuer = configuration["Jwt:Issuer"] ?? "AnalitiAds";
        var audience = configuration["Jwt:Audience"] ?? "AnalitiAds.Frontend";
        var now = timeProvider.GetUtcNow();
        var expires = now.AddHours(8);
        var claims = new[]
        {
            new Claim(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
            new Claim(JwtRegisteredClaimNames.Email, user.Email),
            new Claim("agency_id", agency.Id.ToString()),
            new Claim("agency_name", agency.Name),
            new Claim(ClaimTypes.Role, membership.Role.ToString()),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
        };
        var token = new JwtSecurityToken(issuer, audience, claims, now.UtcDateTime, expires.UtcDateTime,
            new SigningCredentials(new SymmetricSecurityKey(Encoding.UTF8.GetBytes(key)), SecurityAlgorithms.HmacSha256));
        return (new JwtSecurityTokenHandler().WriteToken(token), expires);
    }
}
