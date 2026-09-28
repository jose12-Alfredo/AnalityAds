using AnaliticAsd.Application.Metrics;
using AnaliticAsd.Contracts.Metrics;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AnaliticAsd.Controllers;

[ApiController, Authorize, Route("api/v1")]
public sealed class MetricsController(IMetricsService service) : ControllerBase
{
    [HttpPost("ad-accounts/{adAccountId:guid}/metrics/sync"), Authorize(Roles = "Owner,Admin,Analyst")]
    public async Task<ActionResult<MetricsSyncResponse>> Sync(Guid adAccountId, SyncMetricsRequest request, CancellationToken ct) { var x = await service.SyncAsync(adAccountId, new(request.Since, request.Until), ct); return Ok(new MetricsSyncResponse(x.AdAccountId, x.Since, x.Until, x.AccountSnapshots, x.CampaignSnapshots, x.AdSetSnapshots, x.AdSnapshots, x.CompletedAtUtc)); }
    [HttpGet("ad-accounts/{adAccountId:guid}/metrics")]
    public async Task<ActionResult<IReadOnlyList<InsightSnapshotResponse>>> Account(Guid adAccountId, [FromQuery] DateOnly since, [FromQuery] DateOnly until, CancellationToken ct) => Ok(Map(await service.ListAccountAsync(adAccountId, since, until, ct)));
    [HttpGet("campaigns/{campaignId:guid}/metrics")]
    public async Task<ActionResult<IReadOnlyList<InsightSnapshotResponse>>> Campaign(Guid campaignId, [FromQuery] DateOnly since, [FromQuery] DateOnly until, CancellationToken ct) => Ok(Map(await service.ListCampaignAsync(campaignId, since, until, ct)));
    [HttpGet("ad-sets/{adSetId:guid}/metrics")]
    public async Task<ActionResult<IReadOnlyList<InsightSnapshotResponse>>> AdSet(Guid adSetId, [FromQuery] DateOnly since, [FromQuery] DateOnly until, CancellationToken ct) => Ok(Map(await service.ListAdSetAsync(adSetId, since, until, ct)));
    [HttpGet("ads/{adId:guid}/metrics")]
    public async Task<ActionResult<IReadOnlyList<InsightSnapshotResponse>>> Ad(Guid adId, [FromQuery] DateOnly since, [FromQuery] DateOnly until, CancellationToken ct) => Ok(Map(await service.ListAdAsync(adId, since, until, ct)));
    [HttpGet("ad-accounts/{adAccountId:guid}/metrics/summary")]
    public async Task<ActionResult<RangeMetricsSummaryResponse>> AccountSummary(Guid adAccountId, [FromQuery] DateOnly since, [FromQuery] DateOnly until, CancellationToken ct) => Ok(Map(await service.GetAccountSummaryAsync(adAccountId, since, until, ct)));
    [HttpGet("campaigns/{campaignId:guid}/metrics/summary")]
    public async Task<ActionResult<RangeMetricsSummaryResponse>> CampaignSummary(Guid campaignId, [FromQuery] DateOnly since, [FromQuery] DateOnly until, CancellationToken ct) => Ok(Map(await service.GetCampaignSummaryAsync(campaignId, since, until, ct)));
    [HttpGet("ad-sets/{adSetId:guid}/metrics/summary")]
    public async Task<ActionResult<RangeMetricsSummaryResponse>> AdSetSummary(Guid adSetId, [FromQuery] DateOnly since, [FromQuery] DateOnly until, CancellationToken ct) => Ok(Map(await service.GetAdSetSummaryAsync(adSetId, since, until, ct)));
    [HttpGet("ads/{adId:guid}/metrics/summary")]
    public async Task<ActionResult<RangeMetricsSummaryResponse>> AdSummary(Guid adId, [FromQuery] DateOnly since, [FromQuery] DateOnly until, CancellationToken ct) => Ok(Map(await service.GetAdSummaryAsync(adId, since, until, ct)));
    [HttpGet("ad-accounts/{adAccountId:guid}/campaigns/selector")]
    public async Task<ActionResult<CampaignSelectorResponse>> CampaignSelector(Guid adAccountId, [FromQuery] DateOnly since, [FromQuery] DateOnly until, [FromQuery] string? activity, CancellationToken ct)
    {
        var filter = ParseActivityFilter(activity);
        return Ok(Map(await service.ListCampaignSelectorAsync(adAccountId, since, until, filter, ct)));
    }
    [HttpGet("ad-accounts/{id:guid}/metrics/comparison")] public async Task<ActionResult<RangeMetricsComparisonResponse>> AccountComparison(Guid id, [FromQuery] DateOnly since, [FromQuery] DateOnly until, [FromQuery] string comparison, [FromQuery] DateOnly? comparisonSince, [FromQuery] DateOnly? comparisonUntil, CancellationToken ct) => Ok(Map(await service.CompareAccountAsync(id, since, until, ParseComparison(comparison), comparisonSince, comparisonUntil, ct)));
    [HttpGet("campaigns/{id:guid}/metrics/comparison")] public async Task<ActionResult<RangeMetricsComparisonResponse>> CampaignComparison(Guid id, [FromQuery] DateOnly since, [FromQuery] DateOnly until, [FromQuery] string comparison, [FromQuery] DateOnly? comparisonSince, [FromQuery] DateOnly? comparisonUntil, CancellationToken ct) => Ok(Map(await service.CompareCampaignAsync(id, since, until, ParseComparison(comparison), comparisonSince, comparisonUntil, ct)));
    [HttpGet("ad-sets/{id:guid}/metrics/comparison")] public async Task<ActionResult<RangeMetricsComparisonResponse>> AdSetComparison(Guid id, [FromQuery] DateOnly since, [FromQuery] DateOnly until, [FromQuery] string comparison, [FromQuery] DateOnly? comparisonSince, [FromQuery] DateOnly? comparisonUntil, CancellationToken ct) => Ok(Map(await service.CompareAdSetAsync(id, since, until, ParseComparison(comparison), comparisonSince, comparisonUntil, ct)));
    [HttpGet("ads/{id:guid}/metrics/comparison")] public async Task<ActionResult<RangeMetricsComparisonResponse>> AdComparison(Guid id, [FromQuery] DateOnly since, [FromQuery] DateOnly until, [FromQuery] string comparison, [FromQuery] DateOnly? comparisonSince, [FromQuery] DateOnly? comparisonUntil, CancellationToken ct) => Ok(Map(await service.CompareAdAsync(id, since, until, ParseComparison(comparison), comparisonSince, comparisonUntil, ct)));
    [HttpGet("ad-accounts/{id:guid}/campaigns/benchmarks")] public async Task<ActionResult<CampaignBenchmarksResponse>> CampaignBenchmarks(Guid id, [FromQuery] DateOnly since, [FromQuery] DateOnly until, CancellationToken ct) => Ok(Map(await service.GetCampaignBenchmarksAsync(id, since, until, ct)));

    private static CampaignActivityFilter ParseActivityFilter(string? activity)
    {
        if (string.IsNullOrWhiteSpace(activity)) return CampaignActivityFilter.WithActivity;
        if (Enum.TryParse<CampaignActivityFilter>(activity, true, out var filter) && Enum.IsDefined(filter)) return filter;
        throw new ArgumentException("'activity' must be WithActivity, WithSpend, or All.");
    }
    private static ComparisonPeriodType ParseComparison(string value) => Enum.TryParse<ComparisonPeriodType>(value, true, out var parsed) && Enum.IsDefined(parsed) ? parsed : throw new ArgumentException("'comparison' must be PreviousPeriod, PreviousMonth, PreviousYear, or Custom.");

    private static IReadOnlyList<InsightSnapshotResponse> Map(IReadOnlyList<InsightSnapshotModel> xs) => xs.Select(x => new InsightSnapshotResponse(x.Id, x.AdAccountId, x.CampaignId, x.AdSetId, x.AdId, x.Level, x.Date, x.Currency, x.ObservedDataQuality.ToString(), new(x.Observed.Spend, x.Observed.Impressions, x.Observed.Reach, x.Observed.LinkClicks, x.Observed.Leads, x.Observed.Purchases, x.Observed.PurchaseValue), new(x.Derived.Frequency, x.Derived.Cpm, x.Derived.Ctr, x.Derived.Cpc, x.Derived.Cpl, x.Derived.Cpa, x.Derived.Roas), x.ObservedAtUtc)).ToArray();
    private static RangeMetricsSummaryResponse Map(RangeMetricsSummaryModel x) => new(x.AdAccountId, x.CampaignId, x.AdSetId, x.AdId, x.Level.ToString(), x.Since, x.Until, x.Currency, x.CurrencyStatus.ToString(), Map(x.Coverage), new(Map(x.Observed.Spend), Map(x.Observed.Impressions), Map(x.Observed.Reach), Map(x.Observed.LinkClicks), Map(x.Observed.Leads), Map(x.Observed.Purchases), Map(x.Observed.PurchaseValue)), new(Map(x.Derived.Frequency), Map(x.Derived.Cpm), Map(x.Derived.Ctr), Map(x.Derived.Cpc), Map(x.Derived.Cpl), Map(x.Derived.Cpa), Map(x.Derived.Roas)));
    private static CampaignSelectorResponse Map(CampaignSelectorModel x) => new(x.AdAccountId, x.Since, x.Until, x.ActivityFilter.ToString(), x.Campaigns.Select(y => new CampaignSelectorItemResponse(y.Id, y.Name, y.Objective, y.ConfiguredStatus, y.EffectiveStatus, y.IsPresentOnMeta, y.HasActivity, y.HasObservedSpend, y.Currency, y.CurrencyStatus.ToString(), Map(y.Coverage), Map(y.Spend))).ToArray());
    private static RangeMetricsCoverageResponse Map(RangeMetricsCoverageModel x) => new(x.RequestedDays, x.SnapshotDays, x.LegacyZeroNormalizedSnapshotDays, x.FirstSnapshotDate, x.LastSnapshotDate);
    private static DecimalRangeMetricResponse Map(DecimalRangeMetricModel x) => new(x.Value, x.Availability.ToString());
    private static LongRangeMetricResponse Map(LongRangeMetricModel x) => new(x.Value, x.Availability.ToString());
    private static RangeMetricsComparisonResponse Map(RangeMetricsComparisonModel x) => new(x.ComparisonType.ToString(), Map(x.Current), Map(x.Baseline), new(Map(x.Observed.Spend), Map(x.Observed.Impressions), Map(x.Observed.Reach), Map(x.Observed.LinkClicks), Map(x.Observed.Leads), Map(x.Observed.Purchases), Map(x.Observed.PurchaseValue)), new(Map(x.Derived.Frequency), Map(x.Derived.Cpm), Map(x.Derived.Ctr), Map(x.Derived.Cpc), Map(x.Derived.Cpl), Map(x.Derived.Cpa), Map(x.Derived.Roas)));
    private static DecimalMetricComparisonResponse Map(DecimalMetricComparisonModel x) => new(x.Current, x.Baseline, x.AbsoluteChange, x.PercentageChange, x.Availability.ToString());
    private static LongMetricComparisonResponse Map(LongMetricComparisonModel x) => new(x.Current, x.Baseline, x.AbsoluteChange, x.PercentageChange, x.Availability.ToString());
    private static CampaignBenchmarksResponse Map(CampaignBenchmarksModel x) => new(x.AdAccountId, x.Since, x.Until, x.Method, x.Campaigns.Select(y => new CampaignBenchmarkItemResponse(y.CampaignId, y.Name, y.Objective, y.Currency, Map(y.Cpm), Map(y.Ctr), Map(y.Cpc), Map(y.Cpl), Map(y.Cpa), Map(y.Roas))).ToArray());
    private static BenchmarkMetricResponse Map(BenchmarkMetricModel x) => new(x.CampaignValue, x.BenchmarkValue, x.AbsoluteDifference, x.PercentageDifference, x.ComparableCampaigns, x.Availability.ToString());
}
