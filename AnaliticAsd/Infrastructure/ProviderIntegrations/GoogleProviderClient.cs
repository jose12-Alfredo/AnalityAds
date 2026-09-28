using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json.Serialization;
using System.Text.Json;
using AnaliticAsd.Application.Common;
using AnaliticAsd.Application.ProviderIntegrations;
using AnaliticAsd.Domain.DataSources;

namespace AnaliticAsd.Infrastructure.ProviderIntegrations;

public sealed class GoogleProviderClient(HttpClient http, IConfiguration configuration, TimeProvider clock)
    : IGoogleProviderClient
{
    public async Task<ProviderCredential> ExchangeCodeAsync(string code, DataProvider provider,
        CancellationToken cancellationToken = default)
    {
        var response = await PostToken(new Dictionary<string, string>
        {
            ["code"] = code,
            ["client_id"] = Required("Google:ClientId"),
            ["client_secret"] = Required("Google:ClientSecret"),
            ["redirect_uri"] = Required(provider == DataProvider.GoogleAds
                ? "GoogleAds:RedirectUri" : "GoogleAnalytics4:RedirectUri"),
            ["grant_type"] = "authorization_code"
        }, cancellationToken);
        if (string.IsNullOrWhiteSpace(response.RefreshToken))
            throw new ExternalServiceException("Google did not return offline access. Reconnect and grant consent.");
        return new(response.AccessToken, response.RefreshToken,
            clock.GetUtcNow().AddSeconds(Math.Max(60, response.ExpiresIn)));
    }

    public async Task<ProviderCredential> RefreshAsync(ProviderCredential credential,
        CancellationToken cancellationToken = default)
    {
        var response = await PostToken(new Dictionary<string, string>
        {
            ["refresh_token"] = credential.RefreshToken,
            ["client_id"] = Required("Google:ClientId"),
            ["client_secret"] = Required("Google:ClientSecret"),
            ["grant_type"] = "refresh_token"
        }, cancellationToken);
        return new(response.AccessToken, credential.RefreshToken,
            clock.GetUtcNow().AddSeconds(Math.Max(60, response.ExpiresIn)));
    }

    public async Task<IReadOnlyList<RemoteProviderSource>> ListGoogleAdsCustomersAsync(string accessToken,
        CancellationToken cancellationToken = default)
    {
        using var listRequest = Authorized(HttpMethod.Get,
            "https://googleads.googleapis.com/v25/customers:listAccessibleCustomers", accessToken, ads: true);
        var accessible = await Send<AccessibleCustomers>(listRequest, cancellationToken);
        var results = new List<RemoteProviderSource>();
        foreach (var resource in accessible.ResourceNames ?? [])
        {
            var id = resource.Split('/').LastOrDefault();
            if (string.IsNullOrWhiteSpace(id)) continue;
            using var request = Authorized(HttpMethod.Post,
                $"https://googleads.googleapis.com/v25/customers/{Uri.EscapeDataString(id)}/googleAds:search",
                accessToken, ads: true);
            request.Content = JsonContent.Create(new
            {
                query = "SELECT customer.id, customer.descriptive_name, customer.currency_code, customer.time_zone, customer.manager, customer.test_account FROM customer LIMIT 1"
            });
            var details = await Send<CustomerSearch>(request, cancellationToken);
            var customer = details.Results?.FirstOrDefault()?.Customer;
            if (customer is null) continue;
            results.Add(new(id, string.IsNullOrWhiteSpace(customer.DescriptiveName) ? id : customer.DescriptiveName,
                customer.CurrencyCode, string.IsNullOrWhiteSpace(customer.TimeZone) ? "UTC" : customer.TimeZone,
                DataSourceType.AdvertisingAccount, customer.Manager, customer.TestAccount));
        }
        return results.OrderBy(item => item.Name, StringComparer.OrdinalIgnoreCase).ToArray();
    }

    public async Task<IReadOnlyList<RemoteProviderSource>> ListAnalyticsPropertiesAsync(string accessToken,
        CancellationToken cancellationToken = default)
    {
        var results = new List<RemoteProviderSource>();
        string? pageToken = null;
        do
        {
            var url = "https://analyticsadmin.googleapis.com/v1beta/accountSummaries?pageSize=200";
            if (!string.IsNullOrWhiteSpace(pageToken)) url += $"&pageToken={Uri.EscapeDataString(pageToken)}";
            using var request = Authorized(HttpMethod.Get, url, accessToken);
            var page = await Send<AccountSummaryPage>(request, cancellationToken);
            foreach (var account in page.AccountSummaries ?? [])
            foreach (var property in account.PropertySummaries ?? [])
                results.Add(new(property.Property, property.DisplayName, null, "UTC",
                    DataSourceType.AnalyticsProperty));
            pageToken = page.NextPageToken;
        } while (!string.IsNullOrWhiteSpace(pageToken));
        return results.OrderBy(item => item.Name, StringComparer.OrdinalIgnoreCase).ToArray();
    }

    public async Task<IReadOnlyList<ProviderDailyMetric>> ReadDailyMetricsAsync(string accessToken,
        DataProvider provider, string externalId, DateOnly since, DateOnly until,
        CancellationToken cancellationToken = default)
    {
        if (provider == DataProvider.GoogleAds)
        {
            var id = externalId.Replace("-", "");
            using var request = Authorized(HttpMethod.Post, $"https://googleads.googleapis.com/v25/customers/{Uri.EscapeDataString(id)}/googleAds:searchStream", accessToken, ads: true);
            request.Content = JsonContent.Create(new { query = $"SELECT segments.date, campaign.id, campaign.name, metrics.cost_micros, metrics.impressions, metrics.clicks, metrics.conversions, metrics.conversions_value FROM campaign WHERE segments.date BETWEEN '{since:yyyy-MM-dd}' AND '{until:yyyy-MM-dd}'" });
            using var response = await http.SendAsync(request, cancellationToken);
            if (!response.IsSuccessStatusCode) throw new ExternalServiceException("Google Ads no pudo devolver las métricas solicitadas.");
            using var json = JsonDocument.Parse(await response.Content.ReadAsStreamAsync(cancellationToken));
            var output = new List<ProviderDailyMetric>();
            foreach (var batch in json.RootElement.EnumerateArray())
            foreach (var row in batch.GetProperty("results").EnumerateArray())
            {
                var metrics = row.GetProperty("metrics"); var campaign = row.GetProperty("campaign");
                output.Add(new(DateOnly.Parse(row.GetProperty("segments").GetProperty("date").GetString()!),
                    campaign.GetProperty("id").GetString()!, campaign.GetProperty("name").GetString() ?? "Campaña",
                    Decimal(metrics, "costMicros") / 1_000_000m, Long(metrics, "impressions"), Long(metrics, "clicks"),
                    Decimal(metrics, "conversions"), Decimal(metrics, "conversionsValue")));
            }
            return output;
        }
        using var analytics = Authorized(HttpMethod.Post, $"https://analyticsdata.googleapis.com/v1beta/{externalId}:runReport", accessToken);
        analytics.Content = JsonContent.Create(new { dateRanges = new[] { new { startDate = since.ToString("yyyy-MM-dd"), endDate = until.ToString("yyyy-MM-dd") } }, dimensions = new[] { new { name = "date" } }, metrics = new[] { new { name = "activeUsers" }, new { name = "sessions" }, new { name = "screenPageViews" } }, limit = 100000 });
        using var analyticsResponse = await http.SendAsync(analytics, cancellationToken);
        if (!analyticsResponse.IsSuccessStatusCode) throw new ExternalServiceException("Google Analytics no pudo devolver las métricas solicitadas.");
        using var analyticsJson = JsonDocument.Parse(await analyticsResponse.Content.ReadAsStreamAsync(cancellationToken));
        return analyticsJson.RootElement.TryGetProperty("rows", out var rows) ? rows.EnumerateArray().Select(row =>
        {
            var date = row.GetProperty("dimensionValues")[0].GetProperty("value").GetString()!;
            var values = row.GetProperty("metricValues");
            return new ProviderDailyMetric(DateOnly.ParseExact(date, "yyyyMMdd"), "property", "Propiedad", null, null, null, null, null,
                long.Parse(values[0].GetProperty("value").GetString()!), long.Parse(values[1].GetProperty("value").GetString()!), long.Parse(values[2].GetProperty("value").GetString()!));
        }).ToArray() : [];
    }

    private static decimal? Decimal(JsonElement value, string name) => value.TryGetProperty(name, out var item) && decimal.TryParse(item.ToString(), System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out var number) ? number : null;
    private static long? Long(JsonElement value, string name) => value.TryGetProperty(name, out var item) && long.TryParse(item.ToString(), out var number) ? number : null;

    private async Task<TokenResponse> PostToken(Dictionary<string, string> values, CancellationToken cancellationToken)
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, "https://oauth2.googleapis.com/token")
        {
            Content = new FormUrlEncodedContent(values)
        };
        return await Send<TokenResponse>(request, cancellationToken);
    }

    private HttpRequestMessage Authorized(HttpMethod method, string url, string token, bool ads = false)
    {
        var request = new HttpRequestMessage(method, url);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        // Google Ads stopped requiring developer tokens for new integrations in September 2026.
        // Keep accepting a legacy value for existing deployments, but do not block OAuth,
        // account discovery, or synchronization when it is absent.
        if (ads && configuration["GoogleAds:DeveloperToken"] is { Length: > 0 } developerToken)
            request.Headers.TryAddWithoutValidation("developer-token", developerToken);
        if (ads && configuration["GoogleAds:LoginCustomerId"] is { Length: > 0 } managerId)
            request.Headers.TryAddWithoutValidation("login-customer-id", managerId.Replace("-", ""));
        return request;
    }

    private async Task<T> Send<T>(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        using var response = await http.SendAsync(request, cancellationToken);
        if (!response.IsSuccessStatusCode)
            throw new ExternalServiceException("Google could not complete the request. Reconnect the provider or verify API access.");
        return await response.Content.ReadFromJsonAsync<T>(cancellationToken)
            ?? throw new ExternalServiceException("Google returned an invalid response.");
    }

    private string Required(string key) => configuration[key] is { Length: > 0 } value ? value
        : throw new ServiceConfigurationException($"Configuration '{key}' is required.");

    private sealed record TokenResponse(
        [property: JsonPropertyName("access_token")] string AccessToken,
        [property: JsonPropertyName("refresh_token")] string? RefreshToken,
        [property: JsonPropertyName("expires_in")] long ExpiresIn);
    private sealed record AccessibleCustomers([property: JsonPropertyName("resourceNames")] string[]? ResourceNames);
    private sealed record CustomerSearch([property: JsonPropertyName("results")] CustomerResult[]? Results);
    private sealed record CustomerResult([property: JsonPropertyName("customer")] CustomerItem Customer);
    private sealed record CustomerItem(
        [property: JsonPropertyName("descriptiveName")] string? DescriptiveName,
        [property: JsonPropertyName("currencyCode")] string? CurrencyCode,
        [property: JsonPropertyName("timeZone")] string? TimeZone,
        [property: JsonPropertyName("manager")] bool Manager,
        [property: JsonPropertyName("testAccount")] bool TestAccount);
    private sealed record AccountSummaryPage(
        [property: JsonPropertyName("accountSummaries")] AccountSummary[]? AccountSummaries,
        [property: JsonPropertyName("nextPageToken")] string? NextPageToken);
    private sealed record AccountSummary(
        [property: JsonPropertyName("propertySummaries")] PropertySummary[]? PropertySummaries);
    private sealed record PropertySummary(
        [property: JsonPropertyName("property")] string Property,
        [property: JsonPropertyName("displayName")] string DisplayName);
}
