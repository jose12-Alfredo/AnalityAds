using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using AnaliticAsd.Application.Common;
using AnaliticAsd.Domain.Identity;
using AnaliticAsd.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;

namespace AnaliticAsd.Application.Identity;

public sealed record McpConnectionModel(Guid Id, string Name, string[] Scopes, DateTimeOffset CreatedAtUtc, DateTimeOffset ExpiresAtUtc, DateTimeOffset? RevokedAtUtc);
public sealed record CreatedMcpConnectionModel(McpConnectionModel Connection, string AccessToken);

public interface IMcpConnectionService
{
    Task<IReadOnlyList<McpConnectionModel>> ListAsync(CancellationToken ct = default);
    Task<CreatedMcpConnectionModel> CreateAsync(string name, CancellationToken ct = default);
    Task RevokeAsync(Guid id, CancellationToken ct = default);
}

public sealed class McpConnectionService(AnalitiAdsDbContext db, ICurrentTenant tenant, IConfiguration configuration, TimeProvider clock) : IMcpConnectionService
{
    public const string ReadScope = "analitiads:read";
    public async Task<IReadOnlyList<McpConnectionModel>> ListAsync(CancellationToken ct = default) =>
        (await db.McpConnections.AsNoTracking().Where(x => x.AgencyId == tenant.AgencyId && x.UserId == tenant.UserId).OrderByDescending(x => x.CreatedAtUtc).ToArrayAsync(ct)).Select(Map).ToArray();

    public async Task<CreatedMcpConnectionModel> CreateAsync(string name, CancellationToken ct = default)
    {
        var now = clock.GetUtcNow(); var expires = now.AddDays(30);
        var connection = McpConnection.Create(tenant.AgencyId, tenant.UserId, name, ReadScope, now, expires);
        db.McpConnections.Add(connection); await db.SaveChangesAsync(ct);
        return new(Map(connection), Issue(connection, now));
    }

    public async Task RevokeAsync(Guid id, CancellationToken ct = default)
    {
        var connection = await db.McpConnections.SingleOrDefaultAsync(x => x.Id == id && x.AgencyId == tenant.AgencyId && x.UserId == tenant.UserId, ct)
            ?? throw new EntityNotFoundException($"MCP connection '{id}' was not found.");
        connection.Revoke(clock.GetUtcNow()); await db.SaveChangesAsync(ct);
    }

    private string Issue(McpConnection connection, DateTimeOffset now)
    {
        var key = configuration["Jwt:SigningKey"] ?? throw new InvalidOperationException("Jwt:SigningKey is required.");
        var claims = new[] { new Claim(JwtRegisteredClaimNames.Sub, connection.UserId.ToString()), new Claim(ClaimTypes.NameIdentifier, connection.UserId.ToString()), new Claim("agency_id", connection.AgencyId.ToString()), new Claim("connection_id", connection.Id.ToString()), new Claim("scope", connection.Scopes) };
        var token = new JwtSecurityToken(configuration["Jwt:Issuer"] ?? "AnalitiAds", configuration["Mcp:Audience"] ?? "AnalitiAds.Mcp", claims, now.UtcDateTime, connection.ExpiresAtUtc.UtcDateTime, new SigningCredentials(new SymmetricSecurityKey(Encoding.UTF8.GetBytes(key)), SecurityAlgorithms.HmacSha256));
        return new JwtSecurityTokenHandler().WriteToken(token);
    }
    private static McpConnectionModel Map(McpConnection x) => new(x.Id, x.Name, x.Scopes.Split(' ', StringSplitOptions.RemoveEmptyEntries), x.CreatedAtUtc, x.ExpiresAtUtc, x.RevokedAtUtc);
}
