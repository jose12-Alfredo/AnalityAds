using AnaliticAsd.Application.Metrics;
using AnaliticAsd.Domain.Advertising;

namespace AnaliticAsd.Tests.Metrics;

public sealed class RangeMetricsCalculatorTests
{
    private static readonly Guid AccountId = Guid.NewGuid();
    private static readonly DateOnly DayOne = new(2026, 9, 1);
    private static readonly DateOnly DayTwo = new(2026, 9, 2);

    [Fact]
    public void Summarizes_known_snapshots_recalculates_ratios_and_never_aggregates_reach()
    {
        var result = Summary(
            Snapshot(DayOne, "USD", 10m, 100, 50, 5, 2m, 1m, 10m),
            Snapshot(DayTwo, "USD", 20m, 200, 80, 10, 1m, 1m, 20m));

        Assert.Equal(2, result.Coverage.SnapshotDays);
        Assert.Equal(30m, result.Observed.Spend.Value);
        Assert.Equal(MetricAvailability.CompleteForSnapshots, result.Observed.Spend.Availability);
        Assert.Equal(300, result.Observed.Impressions.Value);
        Assert.Null(result.Observed.Reach.Value);
        Assert.Equal(MetricAvailability.NotAvailableForRange, result.Observed.Reach.Availability);
        Assert.Null(result.Derived.Frequency.Value);
        Assert.Equal(MetricAvailability.NotAvailableForRange, result.Derived.Frequency.Availability);
        Assert.Equal(100m, result.Derived.Cpm.Value);
        Assert.Equal(5m, result.Derived.Ctr.Value);
        Assert.Equal(2m, result.Derived.Cpc.Value);
        Assert.Equal(10m, result.Derived.Cpl.Value);
        Assert.Equal(15m, result.Derived.Cpa.Value);
        Assert.Equal(1m, result.Derived.Roas.Value);
    }

    [Fact]
    public void Does_not_publish_partial_totals_or_ratios_that_depend_on_them()
    {
        var result = Summary(
            Snapshot(DayOne, "USD", 10m, 100, 50, 5, 2m, 1m, 10m),
            Snapshot(DayTwo, "USD", null, 100, 50, 5, 2m, 1m, 10m));

        Assert.Null(result.Observed.Spend.Value);
        Assert.Equal(MetricAvailability.Incomplete, result.Observed.Spend.Availability);
        Assert.Null(result.Derived.Cpm.Value);
        Assert.Equal(MetricAvailability.Incomplete, result.Derived.Cpm.Availability);
        Assert.Equal(5m, result.Derived.Ctr.Value);
        Assert.Equal(MetricAvailability.CompleteForSnapshots, result.Derived.Ctr.Availability);
    }

    [Fact]
    public void Keeps_legacy_zero_normalized_snapshots_out_of_consolidated_metrics()
    {
        var legacy = Snapshot(DayOne, "USD", 10m, 100, 50, 5, 2m, 1m, 10m);
        legacy.MarkLegacyZeroNormalized();

        var result = Summary(legacy);

        Assert.Equal(1, result.Coverage.LegacyZeroNormalizedSnapshotDays);
        Assert.Null(result.Observed.Spend.Value);
        Assert.Equal(MetricAvailability.LegacyZeroNormalized, result.Observed.Spend.Availability);
        Assert.Null(result.Derived.Cpm.Value);
        Assert.Equal(MetricAvailability.LegacyZeroNormalized, result.Derived.Cpm.Availability);
    }

    [Fact]
    public void Avoids_monetary_totals_in_mixed_currencies_but_keeps_safe_count_ratios()
    {
        var result = Summary(
            Snapshot(DayOne, "USD", 10m, 100, 50, 5, 2m, 1m, 10m),
            Snapshot(DayTwo, "BOB", 20m, 100, 50, 5, 2m, 1m, 20m));

        Assert.Equal(RangeCurrencyStatus.Mixed, result.CurrencyStatus);
        Assert.Null(result.Observed.Spend.Value);
        Assert.Equal(MetricAvailability.MixedCurrency, result.Observed.Spend.Availability);
        Assert.Equal(200, result.Observed.Impressions.Value);
        Assert.Equal(5m, result.Derived.Ctr.Value);
        Assert.Equal(MetricAvailability.MixedCurrency, result.Derived.Cpm.Availability);
        Assert.Equal(MetricAvailability.MixedCurrency, result.Derived.Roas.Availability);
    }

    private static RangeMetricsSummaryModel Summary(params InsightSnapshot[] snapshots) =>
        RangeMetricsCalculator.Summarize(AccountId, null, null, null, InsightLevel.Account, DayOne, DayTwo, snapshots);

    private static InsightSnapshot Snapshot(DateOnly day, string currency, decimal? spend, long? impressions, long? reach, long? clicks, decimal? leads, decimal? purchases, decimal? purchaseValue) =>
        InsightSnapshot.Create(AccountId, null, null, null, InsightLevel.Account,
            new(InsightLevel.Account, day, null, null, null, spend, impressions, reach, clicks, leads, purchases, purchaseValue),
            currency, DateTimeOffset.UtcNow);
}
