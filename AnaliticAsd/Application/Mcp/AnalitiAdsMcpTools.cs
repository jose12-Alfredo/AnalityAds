using System.ComponentModel;
using System.Security.Claims;
using AnaliticAsd.Application.AdAccounts;
using AnaliticAsd.Application.Advertising;
using AnaliticAsd.Application.Clients;
using AnaliticAsd.Application.Identity;
using AnaliticAsd.Application.Metrics;
using AnaliticAsd.Domain.Identity;
using AnaliticAsd.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authorization;
using ModelContextProtocol.Server;

namespace AnaliticAsd.Application.Mcp;

[McpServerToolType, Authorize(AuthenticationSchemes = "McpBearer", Policy = "McpRead")]
public sealed class AnalitiAdsMcpTools(IClientService clients, IAdAccountService accounts, IAdvertisingService advertising, IMetricsService metrics, AnalitiAdsDbContext db, ICurrentTenant tenant, TimeProvider clock, IHttpContextAccessor http)
{
    [McpServerTool(Name = "list_authorized_clients"), Description("Lists the AnalitiAds clients the current user is authorized to read.")]
    public Task<IReadOnlyList<ClientModel>> ListClients(CancellationToken ct) => Audited("list_authorized_clients", () => clients.ListAsync(ct), ct);

    [McpServerTool(Name = "list_authorized_ad_accounts"), Description("Lists authorized ad accounts for one authorized client.")]
    public Task<IReadOnlyList<AdAccountModel>> ListAccounts(Guid clientId, CancellationToken ct) => Audited("list_authorized_ad_accounts", () => accounts.ListByClientAsync(clientId, ct), ct);

    [McpServerTool(Name = "list_campaigns"), Description("Lists campaigns for an authorized ad account.")]
    public Task<IReadOnlyList<CampaignModel>> ListCampaigns(Guid adAccountId, bool includeMissing = false, CancellationToken ct = default) => Audited("list_campaigns", () => advertising.ListCampaignsAsync(adAccountId, includeMissing, ct), ct);

    [McpServerTool(Name = "get_campaign"), Description("Gets one authorized campaign by its AnalitiAds identifier.")]
    public Task<CampaignModel> GetCampaign(Guid campaignId, CancellationToken ct) => Audited("get_campaign", () => advertising.GetCampaignAsync(campaignId, ct), ct);

    [McpServerTool(Name = "list_ad_sets"), Description("Lists ad sets for an authorized campaign.")]
    public Task<IReadOnlyList<AdSetModel>> ListAdSets(Guid campaignId, bool includeMissing = false, CancellationToken ct = default) => Audited("list_ad_sets", () => advertising.ListAdSetsAsync(campaignId, includeMissing, ct), ct);

    [McpServerTool(Name = "get_ad_set"), Description("Gets one authorized ad set by its AnalitiAds identifier.")]
    public Task<AdSetModel> GetAdSet(Guid adSetId, CancellationToken ct) => Audited("get_ad_set", () => advertising.GetAdSetAsync(adSetId, ct), ct);

    [McpServerTool(Name = "list_ads"), Description("Lists ads for an authorized ad set.")]
    public Task<IReadOnlyList<AdModel>> ListAds(Guid adSetId, bool includeMissing = false, CancellationToken ct = default) => Audited("list_ads", () => advertising.ListAdsAsync(adSetId, includeMissing, ct), ct);

    [McpServerTool(Name = "get_ad"), Description("Gets one authorized ad by its AnalitiAds identifier.")]
    public Task<AdModel> GetAd(Guid adId, CancellationToken ct) => Audited("get_ad", () => advertising.GetAdAsync(adId, ct), ct);

    [McpServerTool(Name = "get_daily_metrics"), Description("Gets existing daily metric snapshots for account, campaign, ad set, or ad. Level must be account, campaign, adset, or ad.")]
    public Task<IReadOnlyList<InsightSnapshotModel>> GetDailyMetrics(string level, Guid id, DateOnly since, DateOnly until, CancellationToken ct) => Audited("get_daily_metrics", () => level.ToLowerInvariant() switch
    {
        "account" => metrics.ListAccountAsync(id, since, until, ct),
        "campaign" => metrics.ListCampaignAsync(id, since, until, ct),
        "adset" => metrics.ListAdSetAsync(id, since, until, ct),
        "ad" => metrics.ListAdAsync(id, since, until, ct),
        _ => throw new ArgumentException("level must be account, campaign, adset, or ad.")
    }, ct);

    [McpServerTool(Name = "get_metrics_summary"), Description("Gets an existing safe range summary for account, campaign, ad set, or ad. Level must be account, campaign, adset, or ad.")]
    public Task<RangeMetricsSummaryModel> GetSummary(string level, Guid id, DateOnly since, DateOnly until, CancellationToken ct) => Audited("get_metrics_summary", () => level.ToLowerInvariant() switch
    {
        "account" => metrics.GetAccountSummaryAsync(id, since, until, ct),
        "campaign" => metrics.GetCampaignSummaryAsync(id, since, until, ct),
        "adset" => metrics.GetAdSetSummaryAsync(id, since, until, ct),
        "ad" => metrics.GetAdSummaryAsync(id, since, until, ct),
        _ => throw new ArgumentException("level must be account, campaign, adset, or ad.")
    }, ct);

    [McpServerTool(Name = "get_metrics_comparison"), Description("Compares safe metrics for account, campaign, ad set, or ad against a previous period, previous month, previous year, or custom earlier range.")]
    public Task<RangeMetricsComparisonModel> GetComparison(string level, Guid id, DateOnly since, DateOnly until, ComparisonPeriodType comparison, DateOnly? comparisonSince = null, DateOnly? comparisonUntil = null, CancellationToken ct = default) => Audited("get_metrics_comparison", () => level.ToLowerInvariant() switch
    {
        "account" => metrics.CompareAccountAsync(id, since, until, comparison, comparisonSince, comparisonUntil, ct),
        "campaign" => metrics.CompareCampaignAsync(id, since, until, comparison, comparisonSince, comparisonUntil, ct),
        "adset" => metrics.CompareAdSetAsync(id, since, until, comparison, comparisonSince, comparisonUntil, ct),
        "ad" => metrics.CompareAdAsync(id, since, until, comparison, comparisonSince, comparisonUntil, ct),
        _ => throw new ArgumentException("level must be account, campaign, adset, or ad.")
    }, ct);

    [McpServerTool(Name = "get_campaign_benchmarks"), Description("Gets internal campaign benchmarks using the median of other campaigns with the same objective.")]
    public Task<CampaignBenchmarksModel> GetCampaignBenchmarks(Guid adAccountId, DateOnly since, DateOnly until, CancellationToken ct) => Audited("get_campaign_benchmarks", () => metrics.GetCampaignBenchmarksAsync(adAccountId, since, until, ct), ct);

    private async Task<T> Audited<T>(string operation, Func<Task<T>> action, CancellationToken ct)
    {
        var connectionId = Guid.Parse(http.HttpContext?.User.FindFirstValue("connection_id") ?? throw new UnauthorizedAccessException());
        try { var result = await action(); db.McpAuditEvents.Add(McpAuditEvent.Create(connectionId, tenant.AgencyId, tenant.UserId, operation, true, clock.GetUtcNow())); await db.SaveChangesAsync(ct); return result; }
        catch { db.McpAuditEvents.Add(McpAuditEvent.Create(connectionId, tenant.AgencyId, tenant.UserId, operation, false, clock.GetUtcNow())); await db.SaveChangesAsync(CancellationToken.None); throw; }
    }
}
