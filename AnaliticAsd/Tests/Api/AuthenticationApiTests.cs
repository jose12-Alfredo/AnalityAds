using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using AnaliticAsd.Contracts.Identity;
using AnaliticAsd.Infrastructure.Persistence;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;

namespace AnaliticAsd.Tests.Api;

public sealed class AuthenticationApiTests
{
    [Fact]
    public async Task Register_token_authenticates_and_scopes_client_to_created_agency()
    {
        await using var factory = new AuthApiFactory();
        using var client = factory.CreateClient();
        using (var scope = factory.Services.CreateScope())
            await scope.ServiceProvider.GetRequiredService<AnalitiAdsDbContext>().Database.EnsureCreatedAsync();

        var register = await client.PostAsJsonAsync("/api/v1/auth/register", new
        {
            agencyName = "Agency A",
            email = "owner@example.com",
            password = "SecurePassword123"
        });
        Assert.Equal(HttpStatusCode.OK, register.StatusCode);
        var auth = await register.Content.ReadFromJsonAsync<AuthResponse>();
        Assert.NotNull(auth);

        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", auth.AccessToken);
        var created = await client.PostAsJsonAsync("/api/v1/clients", new { name = "Tenant Client" });
        Assert.Equal(HttpStatusCode.Created, created.StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await client.GetAsync("/api/v1/auth/me")).StatusCode);
    }

    private sealed class AuthApiFactory : WebApplicationFactory<Program>
    {
        private readonly SqliteConnection connection = new("Data Source=:memory:");
        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            connection.Open();
            builder.UseEnvironment("Testing");
            builder.ConfigureLogging(logging => logging.ClearProviders());
            builder.ConfigureServices(services =>
            {
                services.RemoveAll<DbContextOptions<AnalitiAdsDbContext>>();
                services.RemoveAll<IDbContextOptionsConfiguration<AnalitiAdsDbContext>>();
                services.RemoveAll<AnalitiAdsDbContext>();
                services.AddDbContext<AnalitiAdsDbContext>(options => options.UseSqlite(connection));
            });
        }

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);
            if (disposing) connection.Dispose();
        }
    }
}
