using System.Net.Http.Json;
using System.Net.Http.Headers;
using System.Text.Json.Serialization;
using AnaliticAsd.Application.Common;
using AnaliticAsd.Application.Meta;

namespace AnaliticAsd.Infrastructure.Meta;

public sealed class MetaGraphClient(HttpClient httpClient, IConfiguration configuration, TimeProvider timeProvider) : IMetaGraphClient
{
    private string Version => configuration["Meta:GraphVersion"] ?? "v25.0";

    public async Task<MetaToken> ExchangeCodeAsync(string code, CancellationToken cancellationToken = default)
    {
        var appId = Required("Meta:AppId");
        var secret = Required("Meta:AppSecret");
        var redirectUri = Required("Meta:RedirectUri");
        var shortToken = await PostFormAsync<TokenResponse>($"{Version}/oauth/access_token", new Dictionary<string, string> { ["client_id"] = appId, ["client_secret"] = secret, ["redirect_uri"] = redirectUri, ["code"] = code }, cancellationToken);
        var longToken = await PostFormAsync<TokenResponse>($"{Version}/oauth/access_token", new Dictionary<string, string> { ["grant_type"] = "fb_exchange_token", ["client_id"] = appId, ["client_secret"] = secret, ["fb_exchange_token"] = shortToken.AccessToken }, cancellationToken);
        DateTimeOffset? expires = longToken.ExpiresIn is > 0 ? timeProvider.GetUtcNow().AddSeconds(longToken.ExpiresIn.Value) : null;
        return new(longToken.AccessToken, expires);
    }

    public async Task<IReadOnlyList<MetaRemoteAccount>> ListAdAccountsAsync(string accessToken, CancellationToken cancellationToken = default)
    {
        var results = new List<MetaRemoteAccount>();
        var url = $"{Version}/me/adaccounts?fields=account_id,name,currency,timezone_name,account_status&limit=100";
        string? after = null;
        do
        {
            var pageUrl = after is null ? url : $"{url}&after={Uri.EscapeDataString(after)}";
            using var request = new HttpRequestMessage(HttpMethod.Get, pageUrl);
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
            var page = await SendAsync<AccountPage>(request, cancellationToken);
            results.AddRange(page.Data.Select(x => new MetaRemoteAccount(x.AccountId, x.Name, x.Currency, x.TimeZoneName, x.AccountStatus)));
            after = page.Paging?.Next is null ? null : page.Paging.Cursors?.After;
        }
        while (after is not null);
        return results;
    }

    private async Task<T> GetAsync<T>(string url, CancellationToken cancellationToken)
    {
        using var response = await httpClient.GetAsync(url, cancellationToken);
        if (!response.IsSuccessStatusCode) throw new ExternalServiceException("Meta could not complete the request. Reconnect the account or try again later.");
        return await response.Content.ReadFromJsonAsync<T>(cancellationToken) ?? throw new ExternalServiceException("Meta returned an invalid response.");
    }

    private async Task<T> PostFormAsync<T>(string url, Dictionary<string, string> values, CancellationToken cancellationToken)
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, url) { Content = new FormUrlEncodedContent(values) };
        return await SendAsync<T>(request, cancellationToken);
    }

    private async Task<T> SendAsync<T>(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        using var response = await httpClient.SendAsync(request, cancellationToken);
        if (!response.IsSuccessStatusCode) throw new ExternalServiceException("Meta could not complete the request. Reconnect the account or try again later.");
        return await response.Content.ReadFromJsonAsync<T>(cancellationToken) ?? throw new ExternalServiceException("Meta returned an invalid response.");
    }

    private string Required(string key) => configuration[key] is { Length: > 0 } value ? value : throw new ServiceConfigurationException($"Configuration '{key}' is required.");
    private sealed record TokenResponse([property: JsonPropertyName("access_token")] string AccessToken, [property: JsonPropertyName("expires_in")] long? ExpiresIn);
    private sealed record AccountPage([property: JsonPropertyName("data")] AccountItem[] Data, [property: JsonPropertyName("paging")] Paging? Paging);
    private sealed record AccountItem([property: JsonPropertyName("account_id")] string AccountId, [property: JsonPropertyName("name")] string Name, [property: JsonPropertyName("currency")] string Currency, [property: JsonPropertyName("timezone_name")] string TimeZoneName, [property: JsonPropertyName("account_status")] int AccountStatus);
    private sealed record Paging([property: JsonPropertyName("cursors")] Cursors? Cursors, [property: JsonPropertyName("next")] string? Next);
    private sealed record Cursors([property: JsonPropertyName("after")] string? After);
}
