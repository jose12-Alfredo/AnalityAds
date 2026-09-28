using AnaliticAsd.Application.Abstractions;
using AnaliticAsd.Application.AdAccounts;
using AnaliticAsd.Application.Clients;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.Authentication;

namespace AnaliticAsd.Tests.Api;

public sealed class AnaliticAsdApiFactory : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
        builder.ConfigureLogging(logging => logging.ClearProviders());
        builder.ConfigureAppConfiguration((_, configuration) =>
            configuration.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:AnalitiAds"] = "Data Source=unused"
            }));
        builder.ConfigureServices(services =>
        {
            services.AddAuthentication(options =>
            {
                options.DefaultAuthenticateScheme = "Test";
                options.DefaultChallengeScheme = "Test";
            }).AddScheme<AuthenticationSchemeOptions, TestAuthHandler>("Test", _ => { });
            services.RemoveAll<IClientRepository>();
            services.RemoveAll<IAdAccountRepository>();
            services.RemoveAll<IUnitOfWork>();
            services.AddSingleton<InMemoryDataStore>();
            services.AddSingleton<IClientRepository>(provider => provider.GetRequiredService<InMemoryDataStore>());
            services.AddSingleton<IAdAccountRepository>(provider => provider.GetRequiredService<InMemoryDataStore>());
            services.AddSingleton<IUnitOfWork>(provider => provider.GetRequiredService<InMemoryDataStore>());
        });
    }
}
