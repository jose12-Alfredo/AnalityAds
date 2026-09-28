using System.Net.Http.Json;
using System.Text.Json.Serialization;
using System.Text.Json;
using AnaliticAsd.Application.Common;
using AnaliticAsd.Application.ProviderIntegrations;
using AnaliticAsd.Domain.DataSources;

namespace AnaliticAsd.Infrastructure.ProviderIntegrations;

public sealed class TikTokProviderClient(HttpClient http, IConfiguration configuration, TimeProvider clock)
    : ITikTokProviderClient
{
    private string BaseUrl => (configuration["TikTokAds:ApiBaseUrl"] ?? "https://business-api.tiktok.com/open_api/v1.3").TrimEnd('/');

    public async Task<ProviderCredential> ExchangeCodeAsync(string code, CancellationToken cancellationToken = default)
    {
        using var response = await http.PostAsJsonAsync($"{BaseUrl}/oauth2/access_token/", new
        {
            app_id = Required("TikTokAds:AppId"), secret = Required("TikTokAds:AppSecret"), auth_code = code
        }, cancellationToken);
        var payload = await Read<TikTokResponse<TokenData>>(response, cancellationToken);
        if (string.IsNullOrWhiteSpace(payload.Data?.AccessToken))
            throw new ExternalServiceException("TikTok no devolvió un token de acceso válido.");
        var lifetimeDays = int.TryParse(configuration["TikTokAds:TokenLifetimeDays"], out var days) ? days : 365;
        return new(payload.Data.AccessToken, string.Empty, clock.GetUtcNow().AddDays(lifetimeDays));
    }

    public async Task<IReadOnlyList<RemoteProviderSource>> ListAdvertisersAsync(string accessToken,
        CancellationToken cancellationToken = default)
    {
        var url = $"{BaseUrl}/oauth2/advertiser/get/?app_id={Uri.EscapeDataString(Required("TikTokAds:AppId"))}&secret={Uri.EscapeDataString(Required("TikTokAds:AppSecret"))}";
        using var request = new HttpRequestMessage(HttpMethod.Get, url);
        request.Headers.TryAddWithoutValidation("Access-Token", accessToken);
        using var response = await http.SendAsync(request, cancellationToken);
        var payload = await Read<TikTokResponse<AdvertiserData>>(response, cancellationToken);
        return (payload.Data?.List ?? []).Select(item => new RemoteProviderSource(item.AdvertiserId,
            string.IsNullOrWhiteSpace(item.AdvertiserName) ? item.AdvertiserId : item.AdvertiserName,
            item.Currency, string.IsNullOrWhiteSpace(item.TimeZone) ? "UTC" : item.TimeZone,
            DataSourceType.AdvertisingAccount)).OrderBy(x => x.Name, StringComparer.OrdinalIgnoreCase).ToArray();
    }

    public async Task<IReadOnlyList<ProviderDailyMetric>> ReadDailyMetricsAsync(string accessToken,
        string advertiserId, DateOnly since, DateOnly until, CancellationToken cancellationToken = default)
    {
        var dimensions = Uri.EscapeDataString("[\"stat_time_day\",\"campaign_id\"]");
        var metrics = Uri.EscapeDataString("[\"campaign_name\",\"spend\",\"impressions\",\"clicks\",\"conversion\",\"total_purchase_value\"]");
        var url = $"{BaseUrl}/report/integrated/get/?advertiser_id={Uri.EscapeDataString(advertiserId)}&report_type=BASIC&data_level=AUCTION_CAMPAIGN&dimensions={dimensions}&metrics={metrics}&start_date={since:yyyy-MM-dd}&end_date={until:yyyy-MM-dd}&page_size=1000";
        using var request = new HttpRequestMessage(HttpMethod.Get, url); request.Headers.TryAddWithoutValidation("Access-Token", accessToken);
        using var response = await http.SendAsync(request, cancellationToken);
        if (!response.IsSuccessStatusCode) throw new ExternalServiceException("TikTok Ads no pudo devolver las métricas solicitadas.");
        using var json = JsonDocument.Parse(await response.Content.ReadAsStreamAsync(cancellationToken));
        var code = json.RootElement.GetProperty("code").GetInt32(); if (code != 0) throw new ExternalServiceException("TikTok Ads rechazó la consulta de métricas.");
        if (!json.RootElement.GetProperty("data").TryGetProperty("list", out var list)) return [];
        return list.EnumerateArray().Select(row => { var d = row.GetProperty("dimensions"); var m = row.GetProperty("metrics");
            return new ProviderDailyMetric(DateOnly.Parse(d.GetProperty("stat_time_day").GetString()![..10]), d.GetProperty("campaign_id").GetString()!, m.GetProperty("campaign_name").GetString() ?? "Campaña",
                Number(m, "spend"), Integer(m, "impressions"), Integer(m, "clicks"), Number(m, "conversion"), Number(m, "total_purchase_value")); }).ToArray();
    }
    private static decimal? Number(JsonElement e, string key) => e.TryGetProperty(key, out var x) && decimal.TryParse(x.ToString(), System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out var n) ? n : null;
    private static long? Integer(JsonElement e, string key) => e.TryGetProperty(key, out var x) && long.TryParse(x.ToString(), out var n) ? n : null;

    private static async Task<T> Read<T>(HttpResponseMessage response, CancellationToken ct)
    {
        if (!response.IsSuccessStatusCode) throw new ExternalServiceException("TikTok no pudo completar la solicitud. Verifica el acceso de la aplicación.");
        var value = await response.Content.ReadFromJsonAsync<T>(cancellationToken: ct);
        return value ?? throw new ExternalServiceException("TikTok devolvió una respuesta inválida.");
    }
    private string Required(string key) => configuration[key] is { Length: > 0 } value ? value : throw new ServiceConfigurationException($"Configuration '{key}' is required.");
    private sealed record TikTokResponse<T>([property: JsonPropertyName("code")] int Code, [property: JsonPropertyName("message")] string? Message, [property: JsonPropertyName("data")] T? Data);
    private sealed record TokenData([property: JsonPropertyName("access_token")] string AccessToken);
    private sealed record AdvertiserData([property: JsonPropertyName("list")] Advertiser[]? List);
    private sealed record Advertiser([property: JsonPropertyName("advertiser_id")] string AdvertiserId,
        [property: JsonPropertyName("advertiser_name")] string AdvertiserName,
        [property: JsonPropertyName("currency")] string? Currency,
        [property: JsonPropertyName("timezone")] string? TimeZone);
}
