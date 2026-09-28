using System.Net;
using System.Text;
using AnaliticAsd.Infrastructure.Meta;
using Microsoft.Extensions.Configuration;

namespace AnaliticAsd.Tests.Meta;

public sealed class MetaGraphClientTests
{
    [Fact]
    public async Task Exchanges_code_without_putting_secrets_in_url_and_lists_accounts_with_bearer()
    {
        var handler = new RecordingHandler();
        var http = new HttpClient(handler) { BaseAddress = new Uri("https://graph.facebook.com/") };
        var configuration = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["Meta:AppId"] = "app-id",
            ["Meta:AppSecret"] = "very-secret",
            ["Meta:RedirectUri"] = "https://api.example.com/api/v1/meta/oauth/callback",
            ["Meta:GraphVersion"] = "v25.0"
        }).Build();
        var client = new MetaGraphClient(http, configuration, TimeProvider.System);

        var token = await client.ExchangeCodeAsync("oauth-code");
        var accounts = await client.ListAdAccountsAsync(token.AccessToken);

        Assert.Equal("long-token", token.AccessToken);
        Assert.Single(accounts);
        Assert.All(handler.RequestUris, uri => Assert.DoesNotContain("very-secret", uri, StringComparison.Ordinal));
        Assert.All(handler.RequestUris, uri => Assert.DoesNotContain("long-token", uri, StringComparison.Ordinal));
        Assert.Equal("Bearer", handler.LastAuthorizationScheme);
        Assert.Contains("fields=account_id,name,currency,timezone_name,account_status", Uri.UnescapeDataString(handler.RequestUris.Last()), StringComparison.Ordinal);
    }

    private sealed class RecordingHandler : HttpMessageHandler
    {
        private int calls;
        public List<string> RequestUris { get; } = [];
        public string? LastAuthorizationScheme { get; private set; }
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            calls++;
            RequestUris.Add(request.RequestUri!.ToString());
            LastAuthorizationScheme = request.Headers.Authorization?.Scheme ?? LastAuthorizationScheme;
            var json = calls switch
            {
                1 => "{\"access_token\":\"short-token\",\"expires_in\":3600}",
                2 => "{\"access_token\":\"long-token\",\"expires_in\":5184000}",
                _ => "{\"data\":[{\"account_id\":\"123\",\"name\":\"Account\",\"currency\":\"USD\",\"timezone_name\":\"America/La_Paz\",\"account_status\":1}]}"
            };
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent(json, Encoding.UTF8, "application/json") });
        }
    }
}
