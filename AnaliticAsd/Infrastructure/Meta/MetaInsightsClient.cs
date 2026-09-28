using System.Globalization;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using AnaliticAsd.Application.Common;
using AnaliticAsd.Application.Metrics;
using AnaliticAsd.Domain.Advertising;

namespace AnaliticAsd.Infrastructure.Meta;

public sealed class MetaInsightsClient(HttpClient http, IConfiguration configuration) : IMetaInsightsClient
{
    private string Version => configuration["Meta:GraphVersion"] ?? "v25.0";
    public async Task<MetaInsights> GetDailyAsync(string metaAccountId, string token, DateOnly since, DateOnly until, CancellationToken ct = default)
    {
        var values = new List<RemoteInsight>();
        foreach (var level in Enum.GetValues<InsightLevel>()) values.AddRange(await ReadLevel(metaAccountId, token, since, until, level, ct));
        return new(values);
    }

    private async Task<IReadOnlyList<RemoteInsight>> ReadLevel(string accountId, string token, DateOnly since, DateOnly until, InsightLevel level, CancellationToken ct)
    {
        var metaLevel = level switch { InsightLevel.Account => "account", InsightLevel.Campaign => "campaign", InsightLevel.AdSet => "adset", InsightLevel.Ad => "ad", _ => throw new ArgumentOutOfRangeException(nameof(level)) };
        var range = Uri.EscapeDataString(JsonSerializer.Serialize(new { since = since.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture), until = until.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture) }));
        var fields = "date_start,date_stop,account_id,campaign_id,adset_id,ad_id,spend,impressions,reach,inline_link_clicks,actions,action_values";
        var baseUrl = $"{Version}/act_{MetaAdAccountId.Parse(accountId).Value}/insights?level={metaLevel}&time_range={range}&time_increment=1&fields={fields}&limit=100";
        var result = new List<RemoteInsight>();
        var cursors = new HashSet<string>(StringComparer.Ordinal);
        string? after = null;
        do
        {
            using var request = new HttpRequestMessage(HttpMethod.Get, after is null ? baseUrl : $"{baseUrl}&after={Uri.EscapeDataString(after)}"); request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
            using var response = await http.SendAsync(request, ct);
            if (!response.IsSuccessStatusCode) throw new ExternalServiceException("Meta could not return daily insights. Reconnect the account, reduce the date range, or try again later.");
            var page = await response.Content.ReadFromJsonAsync<Page>(ct) ?? throw new ExternalServiceException("Meta returned an invalid insights response.");
            foreach (var item in page.Data ?? []) result.Add(Map(item, level));
            if (string.IsNullOrWhiteSpace(page.Paging?.Next))
            {
                after = null;
            }
            else
            {
                after = page.Paging.Cursors?.After;
                if (string.IsNullOrWhiteSpace(after) || !cursors.Add(after))
                    throw new ExternalServiceException("Meta returned an invalid insights pagination response.");
            }
        } while (after is not null);
        return result;
    }

    private static RemoteInsight Map(Item x, InsightLevel level) => new(level, ParseDate(x.DateStart), x.CampaignId, x.AdSetId, x.AdId, Decimal(x.Spend, "spend"), Integer(x.Impressions, "impressions"), Integer(x.Reach, "reach"), Integer(x.InlineLinkClicks, "inline_link_clicks"), Action(x.Actions, LeadTypes), Action(x.Actions, PurchaseTypes), Action(x.ActionValues, PurchaseTypes));
    private static readonly string[] LeadTypes = ["lead", "offsite_conversion.fb_pixel_lead", "onsite_conversion.lead_grouped"];
    private static readonly string[] PurchaseTypes = ["omni_purchase", "purchase", "offsite_conversion.fb_pixel_purchase"];
    private static decimal? Action(ActionValue[]? values, string[] priority) { foreach (var type in priority) { var found = values?.FirstOrDefault(x => string.Equals(x.ActionType, type, StringComparison.Ordinal)); if (found is not null) return Decimal(found.Value, type); } return null; }
    private static decimal? Decimal(string? value, string field) { if (string.IsNullOrWhiteSpace(value)) return null; if (!decimal.TryParse(value, NumberStyles.Number, CultureInfo.InvariantCulture, out var result) || result < 0) throw new ExternalServiceException($"Meta returned an invalid non-negative value for '{field}'."); return result; }
    private static long? Integer(string? value, string field) { if (string.IsNullOrWhiteSpace(value)) return null; if (!long.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var result) || result < 0) throw new ExternalServiceException($"Meta returned an invalid non-negative value for '{field}'."); return result; }
    private static DateOnly ParseDate(string value) => DateOnly.TryParseExact(value, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var date) ? date : throw new ExternalServiceException("Meta returned an invalid insight date.");
    private sealed record Page([property: JsonPropertyName("data")] Item[]? Data, [property: JsonPropertyName("paging")] Paging? Paging);
    private sealed record Paging([property: JsonPropertyName("cursors")] Cursors? Cursors, [property: JsonPropertyName("next")] string? Next);
    private sealed record Cursors([property: JsonPropertyName("after")] string? After);
    private sealed record ActionValue([property: JsonPropertyName("action_type")] string ActionType, [property: JsonPropertyName("value")] string? Value);
    private sealed record Item([property: JsonPropertyName("date_start")] string DateStart, [property: JsonPropertyName("campaign_id")] string? CampaignId, [property: JsonPropertyName("adset_id")] string? AdSetId, [property: JsonPropertyName("ad_id")] string? AdId, [property: JsonPropertyName("spend")] string? Spend, [property: JsonPropertyName("impressions")] string? Impressions, [property: JsonPropertyName("reach")] string? Reach, [property: JsonPropertyName("inline_link_clicks")] string? InlineLinkClicks, [property: JsonPropertyName("actions")] ActionValue[]? Actions, [property: JsonPropertyName("action_values")] ActionValue[]? ActionValues);
}
