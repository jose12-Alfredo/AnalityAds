using AnaliticAsd.Domain.Advertising;

namespace AnaliticAsd.Application.Metrics;

public sealed record SyncMetricsCommand(DateOnly Since, DateOnly Until);
public sealed record MetricsSyncModel(Guid AdAccountId, DateOnly Since, DateOnly Until, int AccountSnapshots, int CampaignSnapshots, int AdSetSnapshots, int AdSnapshots, DateTimeOffset CompletedAtUtc);
public sealed record InsightSnapshotModel(Guid Id, Guid AdAccountId, Guid? CampaignId, Guid? AdSetId, Guid? AdId, string Level, DateOnly Date, string Currency, InsightSnapshotDataQuality ObservedDataQuality, ObservedMetricsModel Observed, DerivedMetricsModel Derived, DateTimeOffset ObservedAtUtc);
public sealed record ObservedMetricsModel(decimal? Spend, long? Impressions, long? Reach, long? LinkClicks, decimal? Leads, decimal? Purchases, decimal? PurchaseValue);
public sealed record DerivedMetricsModel(decimal? Frequency, decimal? Cpm, decimal? Ctr, decimal? Cpc, decimal? Cpl, decimal? Cpa, decimal? Roas);
public sealed record MetaInsights(IReadOnlyList<RemoteInsight> Values);

public enum MetricAvailability { NoData, CompleteForSnapshots, Incomplete, LegacyZeroNormalized, MixedCurrency, NotAvailableForRange, Undefined }
public enum RangeCurrencyStatus { NoData, Single, Mixed }
public enum CampaignActivityFilter { WithActivity, WithSpend, All }
public enum ComparisonPeriodType { PreviousPeriod, PreviousMonth, PreviousYear, Custom }
public enum ComparisonAvailability { Available, CurrentUnavailable, BaselineUnavailable, UndefinedBaseline, CurrencyMismatch }
public enum BenchmarkAvailability { Available, MetricUnavailable, InsufficientComparableCampaigns, CurrencyMismatch, UndefinedBenchmark }

public sealed record DecimalRangeMetricModel(decimal? Value, MetricAvailability Availability);
public sealed record LongRangeMetricModel(long? Value, MetricAvailability Availability);
public sealed record RangeObservedMetricsModel(DecimalRangeMetricModel Spend, LongRangeMetricModel Impressions, LongRangeMetricModel Reach, LongRangeMetricModel LinkClicks, DecimalRangeMetricModel Leads, DecimalRangeMetricModel Purchases, DecimalRangeMetricModel PurchaseValue);
public sealed record RangeDerivedMetricsModel(DecimalRangeMetricModel Frequency, DecimalRangeMetricModel Cpm, DecimalRangeMetricModel Ctr, DecimalRangeMetricModel Cpc, DecimalRangeMetricModel Cpl, DecimalRangeMetricModel Cpa, DecimalRangeMetricModel Roas);
public sealed record RangeMetricsCoverageModel(int RequestedDays, int SnapshotDays, int LegacyZeroNormalizedSnapshotDays, DateOnly? FirstSnapshotDate, DateOnly? LastSnapshotDate);
public sealed record RangeMetricsSummaryModel(Guid AdAccountId, Guid? CampaignId, Guid? AdSetId, Guid? AdId, InsightLevel Level, DateOnly Since, DateOnly Until, string? Currency, RangeCurrencyStatus CurrencyStatus, RangeMetricsCoverageModel Coverage, RangeObservedMetricsModel Observed, RangeDerivedMetricsModel Derived);
public sealed record CampaignSelectorItemModel(Guid Id, string Name, string Objective, string ConfiguredStatus, string EffectiveStatus, bool IsPresentOnMeta, bool HasActivity, bool HasObservedSpend, string? Currency, RangeCurrencyStatus CurrencyStatus, RangeMetricsCoverageModel Coverage, DecimalRangeMetricModel Spend);
public sealed record CampaignSelectorModel(Guid AdAccountId, DateOnly Since, DateOnly Until, CampaignActivityFilter ActivityFilter, IReadOnlyList<CampaignSelectorItemModel> Campaigns);
public sealed record DecimalMetricComparisonModel(decimal? Current, decimal? Baseline, decimal? AbsoluteChange, decimal? PercentageChange, ComparisonAvailability Availability);
public sealed record LongMetricComparisonModel(long? Current, long? Baseline, long? AbsoluteChange, decimal? PercentageChange, ComparisonAvailability Availability);
public sealed record ComparedObservedMetricsModel(DecimalMetricComparisonModel Spend, LongMetricComparisonModel Impressions, LongMetricComparisonModel Reach, LongMetricComparisonModel LinkClicks, DecimalMetricComparisonModel Leads, DecimalMetricComparisonModel Purchases, DecimalMetricComparisonModel PurchaseValue);
public sealed record ComparedDerivedMetricsModel(DecimalMetricComparisonModel Frequency, DecimalMetricComparisonModel Cpm, DecimalMetricComparisonModel Ctr, DecimalMetricComparisonModel Cpc, DecimalMetricComparisonModel Cpl, DecimalMetricComparisonModel Cpa, DecimalMetricComparisonModel Roas);
public sealed record RangeMetricsComparisonModel(ComparisonPeriodType ComparisonType, RangeMetricsSummaryModel Current, RangeMetricsSummaryModel Baseline, ComparedObservedMetricsModel Observed, ComparedDerivedMetricsModel Derived);
public sealed record BenchmarkMetricModel(decimal? CampaignValue, decimal? BenchmarkValue, decimal? AbsoluteDifference, decimal? PercentageDifference, int ComparableCampaigns, BenchmarkAvailability Availability);
public sealed record CampaignBenchmarkItemModel(Guid CampaignId, string Name, string Objective, string? Currency, BenchmarkMetricModel Cpm, BenchmarkMetricModel Ctr, BenchmarkMetricModel Cpc, BenchmarkMetricModel Cpl, BenchmarkMetricModel Cpa, BenchmarkMetricModel Roas);
public sealed record CampaignBenchmarksModel(Guid AdAccountId, DateOnly Since, DateOnly Until, string Method, IReadOnlyList<CampaignBenchmarkItemModel> Campaigns);

public interface IMetaInsightsClient { Task<MetaInsights> GetDailyAsync(string metaAccountId, string accessToken, DateOnly since, DateOnly until, CancellationToken cancellationToken = default); }
public interface IMetricsRepository
{
    Task<Dictionary<string, Campaign>> CampaignsAsync(Guid accountId, CancellationToken ct = default);
    Task<Dictionary<string, AdSet>> AdSetsAsync(Guid accountId, CancellationToken ct = default);
    Task<Dictionary<string, Ad>> AdsAsync(Guid accountId, CancellationToken ct = default);
    Task<InsightSnapshot?> FindAsync(Guid accountId, InsightLevel level, Guid? entityId, DateOnly date, CancellationToken ct = default);
    Task<IReadOnlyList<InsightSnapshot>> ListAsync(Guid accountId, InsightLevel level, Guid? entityId, DateOnly since, DateOnly until, CancellationToken ct = default);
    Task<IReadOnlyList<InsightSnapshot>> ListForAccountAsync(Guid accountId, InsightLevel level, DateOnly since, DateOnly until, CancellationToken ct = default);
    void Add(InsightSnapshot value);
}
public interface IMetricsService
{
    Task<MetricsSyncModel> SyncAsync(Guid accountId, SyncMetricsCommand command, CancellationToken ct = default);
    Task<IReadOnlyList<InsightSnapshotModel>> ListAccountAsync(Guid accountId, DateOnly since, DateOnly until, CancellationToken ct = default);
    Task<IReadOnlyList<InsightSnapshotModel>> ListCampaignAsync(Guid id, DateOnly since, DateOnly until, CancellationToken ct = default);
    Task<IReadOnlyList<InsightSnapshotModel>> ListAdSetAsync(Guid id, DateOnly since, DateOnly until, CancellationToken ct = default);
    Task<IReadOnlyList<InsightSnapshotModel>> ListAdAsync(Guid id, DateOnly since, DateOnly until, CancellationToken ct = default);
    Task<RangeMetricsSummaryModel> GetAccountSummaryAsync(Guid id, DateOnly since, DateOnly until, CancellationToken ct = default);
    Task<RangeMetricsSummaryModel> GetCampaignSummaryAsync(Guid id, DateOnly since, DateOnly until, CancellationToken ct = default);
    Task<RangeMetricsSummaryModel> GetAdSetSummaryAsync(Guid id, DateOnly since, DateOnly until, CancellationToken ct = default);
    Task<RangeMetricsSummaryModel> GetAdSummaryAsync(Guid id, DateOnly since, DateOnly until, CancellationToken ct = default);
    Task<CampaignSelectorModel> ListCampaignSelectorAsync(Guid accountId, DateOnly since, DateOnly until, CampaignActivityFilter activityFilter, CancellationToken ct = default);
    Task<RangeMetricsComparisonModel> CompareAccountAsync(Guid id, DateOnly since, DateOnly until, ComparisonPeriodType type, DateOnly? comparisonSince, DateOnly? comparisonUntil, CancellationToken ct = default);
    Task<RangeMetricsComparisonModel> CompareCampaignAsync(Guid id, DateOnly since, DateOnly until, ComparisonPeriodType type, DateOnly? comparisonSince, DateOnly? comparisonUntil, CancellationToken ct = default);
    Task<RangeMetricsComparisonModel> CompareAdSetAsync(Guid id, DateOnly since, DateOnly until, ComparisonPeriodType type, DateOnly? comparisonSince, DateOnly? comparisonUntil, CancellationToken ct = default);
    Task<RangeMetricsComparisonModel> CompareAdAsync(Guid id, DateOnly since, DateOnly until, ComparisonPeriodType type, DateOnly? comparisonSince, DateOnly? comparisonUntil, CancellationToken ct = default);
    Task<CampaignBenchmarksModel> GetCampaignBenchmarksAsync(Guid accountId, DateOnly since, DateOnly until, CancellationToken ct = default);
}
