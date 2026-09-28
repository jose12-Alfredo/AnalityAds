using AnaliticAsd.Application.Metrics;
using AnaliticAsd.Domain.Advertising;

namespace AnaliticAsd.Tests.Metrics;

public sealed class RangeComparisonCalculatorTests
{
    [Fact]
    public void Calculates_absolute_and_percentage_change_and_keeps_zero_baseline_explicit()
    {
        var current = Summary(new DateOnly(2026, 9, 2), "USD", 150m, 150, 15);
        var baseline = Summary(new DateOnly(2026, 9, 1), "USD", 100m, 0, 10);
        var result = RangeComparisonCalculator.Compare(ComparisonPeriodType.PreviousPeriod, current, baseline);

        Assert.Equal(50m, result.Observed.Spend.AbsoluteChange);
        Assert.Equal(50m, result.Observed.Spend.PercentageChange);
        Assert.Equal(ComparisonAvailability.UndefinedBaseline, result.Observed.Impressions.Availability);
        Assert.Equal(150, result.Observed.Impressions.AbsoluteChange);
        Assert.Equal(ComparisonAvailability.BaselineUnavailable, result.Derived.Ctr.Availability);
        Assert.Equal(ComparisonAvailability.CurrentUnavailable, result.Observed.Reach.Availability);
    }

    [Fact]
    public void Refuses_legacy_incomplete_and_cross_currency_comparisons()
    {
        var current = Summary(new DateOnly(2026, 9, 2), "USD", 100m, 100, 10);
        var legacy = Snapshot(new DateOnly(2026, 9, 1), "BOB", 100m, 100, 10); legacy.MarkLegacyZeroNormalized();
        var baseline = RangeMetricsCalculator.Summarize(current.AdAccountId, null, null, null, InsightLevel.Account, legacy.SnapshotDate, legacy.SnapshotDate, [legacy]);
        var result = RangeComparisonCalculator.Compare(ComparisonPeriodType.Custom, current, baseline);

        Assert.Equal(ComparisonAvailability.CurrencyMismatch, result.Observed.Spend.Availability);
        Assert.Equal(ComparisonAvailability.BaselineUnavailable, result.Derived.Ctr.Availability);
        Assert.Null(result.Derived.Ctr.PercentageChange);
    }

    [Fact]
    public void Campaign_benchmark_uses_median_of_at_least_two_comparable_peers()
    {
        var campaign = new DecimalRangeMetricModel(30m, MetricAvailability.CompleteForSnapshots);
        var result = CampaignBenchmarkCalculator.Calculate(campaign,
            [(new(10m, MetricAvailability.CompleteForSnapshots), "USD"), (new(20m, MetricAvailability.CompleteForSnapshots), "USD"), (new(100m, MetricAvailability.CompleteForSnapshots), "BOB")], "USD", true);

        Assert.Equal(15m, result.BenchmarkValue);
        Assert.Equal(15m, result.AbsoluteDifference);
        Assert.Equal(100m, result.PercentageDifference);
        Assert.Equal(2, result.ComparableCampaigns);
        Assert.Equal(BenchmarkAvailability.Available, result.Availability);
        Assert.Equal(BenchmarkAvailability.InsufficientComparableCampaigns, CampaignBenchmarkCalculator.Calculate(campaign, [(new(10m, MetricAvailability.CompleteForSnapshots), "USD")], "USD", true).Availability);
    }

    private static RangeMetricsSummaryModel Summary(DateOnly day, string currency, decimal spend, long impressions, long clicks)
    {
        var snapshot = Snapshot(day, currency, spend, impressions, clicks);
        return RangeMetricsCalculator.Summarize(snapshot.AdAccountId, null, null, null, InsightLevel.Account, day, day, [snapshot]);
    }
    private static InsightSnapshot Snapshot(DateOnly day, string currency, decimal spend, long impressions, long clicks) => InsightSnapshot.Create(Guid.NewGuid(), null, null, null, InsightLevel.Account, new(InsightLevel.Account, day, null, null, null, spend, impressions, null, clicks, 1, 1, spend), currency, DateTimeOffset.UtcNow);
}
