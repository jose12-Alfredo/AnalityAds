using System.Globalization;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json.Serialization;
using AnaliticAsd.Application.Advertising;
using AnaliticAsd.Application.Common;
using AnaliticAsd.Domain.Advertising;

namespace AnaliticAsd.Infrastructure.Meta;

public sealed class MetaAdvertisingClient(HttpClient httpClient, IConfiguration configuration) : IMetaAdvertisingClient
{
    private string Version => configuration["Meta:GraphVersion"] ?? "v25.0";
    public async Task<MetaHierarchy> GetHierarchyAsync(string metaAccountId, string accessToken, CancellationToken cancellationToken = default)
    {
        var account = $"act_{MetaAdAccountId.Parse(metaAccountId).Value}";
        var campaigns = await ReadAll<CampaignItem>($"{Version}/{account}/campaigns?fields=id,name,objective,status,effective_status,start_time,stop_time,created_time,updated_time&limit=100", accessToken, cancellationToken);
        var adSets = await ReadAll<AdSetItem>($"{Version}/{account}/adsets?fields=id,campaign_id,name,optimization_goal,billing_event,status,effective_status,start_time,end_time,created_time,updated_time&limit=100", accessToken, cancellationToken);
        var ads = await ReadAll<AdItem>($"{Version}/{account}/ads?fields=id,adset_id,name,status,effective_status,created_time,updated_time&limit=100", accessToken, cancellationToken);
        return new(
            campaigns.Select(x => new RemoteCampaign(x.Id, x.Name, x.Objective, x.Status, x.EffectiveStatus, Parse(x.StartTime), Parse(x.StopTime), Parse(x.CreatedTime), Parse(x.UpdatedTime))).ToArray(),
            adSets.Select(x => new RemoteAdSet(x.Id, x.CampaignId, x.Name, x.OptimizationGoal, x.BillingEvent, x.Status, x.EffectiveStatus, Parse(x.StartTime), Parse(x.EndTime), Parse(x.CreatedTime), Parse(x.UpdatedTime))).ToArray(),
            ads.Select(x => new RemoteAd(x.Id, x.AdSetId, x.Name, x.Status, x.EffectiveStatus, Parse(x.CreatedTime), Parse(x.UpdatedTime))).ToArray());
    }

    private async Task<List<T>> ReadAll<T>(string url, string token, CancellationToken cancellationToken)
    {
        var values = new List<T>();
        string? after = null;
        do
        {
            var pageUrl = after is null ? url : $"{url}&after={Uri.EscapeDataString(after)}";
            using var request = new HttpRequestMessage(HttpMethod.Get, pageUrl);
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
            using var response = await httpClient.SendAsync(request, cancellationToken);
            if (!response.IsSuccessStatusCode) throw new ExternalServiceException("Meta could not synchronize the advertising hierarchy. Reconnect the account or try again later.");
            var page = await response.Content.ReadFromJsonAsync<Page<T>>(cancellationToken) ?? throw new ExternalServiceException("Meta returned an invalid synchronization response.");
            values.AddRange(page.Data ?? []);
            after = page.Paging?.Next is null ? null : page.Paging.Cursors?.After;
        }
        while (after is not null);
        return values;
    }

    private static DateTimeOffset? Parse(string? value) => DateTimeOffset.TryParse(value, CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal, out var result) ? result.ToUniversalTime() : null;
    private sealed record Page<T>([property: JsonPropertyName("data")] T[]? Data, [property: JsonPropertyName("paging")] Paging? Paging);
    private sealed record Paging([property: JsonPropertyName("cursors")] Cursors? Cursors, [property: JsonPropertyName("next")] string? Next);
    private sealed record Cursors([property: JsonPropertyName("after")] string? After);
    private sealed record CampaignItem([property: JsonPropertyName("id")] string Id, [property: JsonPropertyName("name")] string Name, [property: JsonPropertyName("objective")] string? Objective, [property: JsonPropertyName("status")] string? Status, [property: JsonPropertyName("effective_status")] string? EffectiveStatus, [property: JsonPropertyName("start_time")] string? StartTime, [property: JsonPropertyName("stop_time")] string? StopTime, [property: JsonPropertyName("created_time")] string? CreatedTime, [property: JsonPropertyName("updated_time")] string? UpdatedTime);
    private sealed record AdSetItem([property: JsonPropertyName("id")] string Id, [property: JsonPropertyName("campaign_id")] string CampaignId, [property: JsonPropertyName("name")] string Name, [property: JsonPropertyName("optimization_goal")] string? OptimizationGoal, [property: JsonPropertyName("billing_event")] string? BillingEvent, [property: JsonPropertyName("status")] string? Status, [property: JsonPropertyName("effective_status")] string? EffectiveStatus, [property: JsonPropertyName("start_time")] string? StartTime, [property: JsonPropertyName("end_time")] string? EndTime, [property: JsonPropertyName("created_time")] string? CreatedTime, [property: JsonPropertyName("updated_time")] string? UpdatedTime);
    private sealed record AdItem([property: JsonPropertyName("id")] string Id, [property: JsonPropertyName("adset_id")] string AdSetId, [property: JsonPropertyName("name")] string Name, [property: JsonPropertyName("status")] string? Status, [property: JsonPropertyName("effective_status")] string? EffectiveStatus, [property: JsonPropertyName("created_time")] string? CreatedTime, [property: JsonPropertyName("updated_time")] string? UpdatedTime);
}
