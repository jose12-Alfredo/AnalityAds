using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using AnaliticAsd.Application.Identity;
using AnaliticAsd.Contracts.Identity;
using AnaliticAsd.Infrastructure.Persistence;
using AnaliticAsd.Domain.Identity;
using AnaliticAsd.Domain.Clients;
using AnaliticAsd.Domain.Advertising;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using ModelContextProtocol.Client;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;

namespace AnaliticAsd.Tests.Mcp;

public sealed class McpIntegrationTests
{
    private const string Password = "SecurePassword123";

    [Fact]
    public async Task Official_client_discovers_and_invokes_read_tools_and_revocation_is_immediate()
    {
        await using var app = await McpApp.Create();
        using var web = app.Authenticated(app.Owner);
        Assert.Equal(HttpStatusCode.BadRequest, (await web.PostAsJsonAsync("/api/v1/mcp/connections", new { name = "No consent", consentAccepted = false })).StatusCode);
        var created = await (await web.PostAsJsonAsync("/api/v1/mcp/connections", new { name = "Codex test", consentAccepted = true })).Content.ReadFromJsonAsync<CreatedMcpConnectionModel>();
        Assert.NotNull(created); Assert.Equal(new[] { McpConnectionService.ReadScope }, created.Connection.Scopes);

        await using var client = await app.Mcp(created.AccessToken);
        var tools = await client.ListToolsAsync();
        Assert.Contains(tools, x => x.Name == "list_authorized_clients");
        Assert.Contains(tools, x => x.Name == "get_daily_metrics");
        Assert.Contains(tools, x => x.Name == "get_metrics_comparison");
        Assert.Contains(tools, x => x.Name == "get_campaign_benchmarks");
        Assert.Equal(12, tools.Count);
        await client.CallToolAsync("list_authorized_clients");

        Assert.Equal(HttpStatusCode.NoContent, (await web.DeleteAsync($"/api/v1/mcp/connections/{created.Connection.Id}")).StatusCode);
        await Assert.ThrowsAnyAsync<Exception>(async () => await client.CallToolAsync("list_authorized_clients"));
        using var scope = app.Services.CreateScope();
        Assert.Contains(await scope.ServiceProvider.GetRequiredService<AnalitiAdsDbContext>().McpAuditEvents.ToArrayAsync(), x => x.Operation == "list_authorized_clients" && x.Succeeded);
    }

    [Fact]
    public async Task Anonymous_frontend_token_and_global_key_cannot_use_mcp()
    {
        await using var app = await McpApp.Create();
        await Assert.ThrowsAnyAsync<Exception>(async () => { await using var c = await app.Mcp(null); await c.ListToolsAsync(); });
        await Assert.ThrowsAnyAsync<Exception>(async () => { await using var c = await app.Mcp(app.Owner.AccessToken); await c.ListToolsAsync(); });
        await Assert.ThrowsAnyAsync<Exception>(async () => { await using var c = await app.Mcp(null, "global-admin-key"); await c.ListToolsAsync(); });
    }

    [Fact]
    public async Task Revoked_membership_is_rejected_on_the_next_mcp_request()
    {
        await using var app = await McpApp.Create(); using var web = app.Authenticated(app.Owner);
        var created = (await (await web.PostAsJsonAsync("/api/v1/mcp/connections", new { name = "Claude", consentAccepted = true })).Content.ReadFromJsonAsync<CreatedMcpConnectionModel>())!;
        await using var client = await app.Mcp(created.AccessToken);
        using (var scope = app.Services.CreateScope()) { var db = scope.ServiceProvider.GetRequiredService<AnalitiAdsDbContext>(); db.Memberships.Remove(await db.Memberships.SingleAsync(x => x.UserId == app.Owner.UserId)); await db.SaveChangesAsync(); }
        await Assert.ThrowsAnyAsync<Exception>(async () => await client.ListToolsAsync());
    }

    [Fact]
    public async Task Protected_resource_metadata_publishes_only_the_read_scope()
    {
        await using var app = await McpApp.Create(); using var http = app.CreateClient(new() { BaseAddress = new Uri("https://localhost") });
        var metadata = await http.GetFromJsonAsync<System.Text.Json.JsonElement>("/.well-known/oauth-protected-resource/mcp");
        Assert.Equal("https://localhost/mcp", metadata.GetProperty("resource").GetString());
        Assert.Equal(McpConnectionService.ReadScope, Assert.Single(metadata.GetProperty("scopes_supported").EnumerateArray()).GetString());
    }

    [Fact]
    public async Task Valid_mcp_audience_without_read_scope_has_insufficient_permissions()
    {
        await using var app = await McpApp.Create();
        await Assert.ThrowsAnyAsync<Exception>(async () => { await using var client = await app.Mcp(app.TokenWithoutReadScope()); await client.ListToolsAsync(); });
    }

    [Fact]
    public async Task ClientViewer_and_agency_boundaries_apply_to_every_mcp_tool_call()
    {
        await using var app = await McpApp.Create();
        var assigned = await app.Seed(app.Owner.AgencyId, "Assigned", "8101");
        var sameAgencyForbidden = await app.Seed(app.Owner.AgencyId, "Forbidden", "8102");
        var otherOwner = await app.Register("foreign@mcp.test");
        var otherAgency = await app.Seed(otherOwner.AgencyId, "Foreign", "8103");
        var viewer = await app.ClientViewer(assigned.ClientId);
        using var web = app.Authenticated(viewer);
        var created = (await (await web.PostAsJsonAsync("/api/v1/mcp/connections", new { name = "Scoped", consentAccepted = true })).Content.ReadFromJsonAsync<CreatedMcpConnectionModel>())!;
        await using var client = await app.Mcp(created.AccessToken);

        await client.CallToolAsync("list_authorized_ad_accounts", new Dictionary<string, object?> { ["clientId"] = assigned.ClientId });
        Assert.True((await client.CallToolAsync("list_authorized_ad_accounts", new Dictionary<string, object?> { ["clientId"] = sameAgencyForbidden.ClientId })).IsError);
        Assert.True((await client.CallToolAsync("list_authorized_ad_accounts", new Dictionary<string, object?> { ["clientId"] = otherAgency.ClientId })).IsError);
    }

    private sealed class McpApp : WebApplicationFactory<Program>
    {
        private readonly Microsoft.Data.Sqlite.SqliteConnection connection = new("Data Source=:memory:");
        public AuthResponse Owner { get; private set; } = null!;
        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            connection.Open(); builder.UseEnvironment("Testing"); builder.ConfigureLogging(x => x.ClearProviders());
            builder.ConfigureServices(services =>
            {
                services.AddDataProtection().UseEphemeralDataProtectionProvider(); services.RemoveAll<DbContextOptions<AnalitiAdsDbContext>>();
                services.RemoveAll<IDbContextOptionsConfiguration<AnalitiAdsDbContext>>(); services.RemoveAll<AnalitiAdsDbContext>();
                services.AddDbContext<AnalitiAdsDbContext>(x => x.UseSqlite(connection));
            });
        }
        public static async Task<McpApp> Create()
        {
            var app = new McpApp(); using var scope = app.Services.CreateScope(); await scope.ServiceProvider.GetRequiredService<AnalitiAdsDbContext>().Database.EnsureCreatedAsync();
            using var http = app.CreateClient(new() { BaseAddress = new Uri("https://localhost") });
            var response = await http.PostAsJsonAsync("/api/v1/auth/register", new { agencyName = "Agency", email = "owner@mcp.test", password = Password }); response.EnsureSuccessStatusCode();
            app.Owner = (await response.Content.ReadFromJsonAsync<AuthResponse>())!; return app;
        }
        public async Task<AuthResponse> Register(string email)
        {
            using var http = CreateClient(new() { BaseAddress = new Uri("https://localhost") });
            var response = await http.PostAsJsonAsync("/api/v1/auth/register", new { agencyName = "Agency", email, password = Password }); response.EnsureSuccessStatusCode();
            return (await response.Content.ReadFromJsonAsync<AuthResponse>())!;
        }
        public async Task<(Guid ClientId, Guid AccountId)> Seed(Guid agencyId, string name, string metaId)
        {
            using var scope = Services.CreateScope(); var db = scope.ServiceProvider.GetRequiredService<AnalitiAdsDbContext>(); var now = DateTimeOffset.UtcNow;
            var client = Client.Create(agencyId, name, now); var account = AdAccount.Create(agencyId, client.Id, MetaAdAccountId.Parse(metaId), name, CurrencyCode.Parse("USD"), MetaTimeZoneId.Parse("America/La_Paz"), now);
            db.AddRange(client, account); await db.SaveChangesAsync(); return (client.Id, account.Id);
        }
        public async Task<AuthResponse> ClientViewer(Guid clientId)
        {
            using var scope = Services.CreateScope(); var db = scope.ServiceProvider.GetRequiredService<AnalitiAdsDbContext>(); var now = DateTimeOffset.UtcNow;
            var user = User.Create("viewer@mcp.test", now); user.SetPasswordHash(scope.ServiceProvider.GetRequiredService<IPasswordHasher<User>>().HashPassword(user, Password));
            var membership = Membership.Create(Owner.AgencyId, user.Id, AgencyRole.ClientViewer, now); db.AddRange(user, membership, ClientAccess.Create(Owner.AgencyId, clientId, user.Id, now)); await db.SaveChangesAsync();
            var agency = await db.Agencies.SingleAsync(x => x.Id == Owner.AgencyId); var token = scope.ServiceProvider.GetRequiredService<ITokenIssuer>().Issue(user, agency, membership);
            return new(token.Token, token.ExpiresAtUtc, user.Id, user.Email, agency.Id, agency.Name, AgencyRole.ClientViewer.ToString());
        }
        public HttpClient Authenticated(AuthResponse auth) { var c = CreateClient(new() { BaseAddress = new Uri("https://localhost") }); c.DefaultRequestHeaders.Authorization = new("Bearer", auth.AccessToken); return c; }
        public string TokenWithoutReadScope()
        {
            var configuration = Services.GetRequiredService<IConfiguration>(); var key = configuration["Jwt:SigningKey"]!; var now = DateTimeOffset.UtcNow;
            var token = new JwtSecurityToken(configuration["Jwt:Issuer"] ?? "AnalitiAds", configuration["Mcp:Audience"] ?? "AnalitiAds.Mcp",
                [new Claim(JwtRegisteredClaimNames.Sub, Owner.UserId.ToString()), new Claim(ClaimTypes.NameIdentifier, Owner.UserId.ToString()), new Claim("agency_id", Owner.AgencyId.ToString()), new Claim("connection_id", Guid.NewGuid().ToString()), new Claim("scope", "analitiads:write")],
                now.UtcDateTime, now.AddMinutes(5).UtcDateTime, new SigningCredentials(new SymmetricSecurityKey(Encoding.UTF8.GetBytes(key)), SecurityAlgorithms.HmacSha256));
            return new JwtSecurityTokenHandler().WriteToken(token);
        }
        public async Task<McpClient> Mcp(string? token, string? apiKey = null)
        {
            var headers = new Dictionary<string, string>(); if (token is not null) headers["Authorization"] = "Bearer " + token; if (apiKey is not null) headers["X-Api-Key"] = apiKey;
            using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(1));
            return await McpClient.CreateAsync(new HttpClientTransport(new HttpClientTransportOptions { Endpoint = new Uri("https://localhost/mcp"), AdditionalHeaders = headers }, CreateClient()), cancellationToken: timeout.Token);
        }
        protected override void Dispose(bool disposing) { base.Dispose(disposing); if (disposing) connection.Dispose(); }
    }
}
