using AnaliticAsd.Domain.Advertising;

namespace AnaliticAsd.Application.Metrics;

internal static class RangeMetricsCalculator
{
    public static RangeMetricsSummaryModel Summarize(
        Guid adAccountId,
        Guid? campaignId,
        Guid? adSetId,
        Guid? adId,
        InsightLevel level,
        DateOnly since,
        DateOnly until,
        IReadOnlyList<InsightSnapshot> snapshots)
    {
        var coverage = new RangeMetricsCoverageModel(
            until.DayNumber - since.DayNumber + 1,
            snapshots.Count,
            snapshots.Count(x => x.ObservedDataQuality == InsightSnapshotDataQuality.LegacyZeroNormalized),
            snapshots.Count == 0 ? null : snapshots.Min(x => x.SnapshotDate),
            snapshots.Count == 0 ? null : snapshots.Max(x => x.SnapshotDate));
        var currencies = snapshots.Select(x => x.Currency).Distinct(StringComparer.Ordinal).ToArray();
        var currencyStatus = currencies.Length switch
        {
            0 => RangeCurrencyStatus.NoData,
            1 => RangeCurrencyStatus.Single,
            _ => RangeCurrencyStatus.Mixed
        };

        var spend = Decimal(snapshots, x => x.Spend, currencyStatus, requiresSingleCurrency: true);
        var impressions = Long(snapshots, x => x.Impressions);
        var clicks = Long(snapshots, x => x.LinkClicks);
        var leads = Decimal(snapshots, x => x.Leads, currencyStatus);
        var purchases = Decimal(snapshots, x => x.Purchases, currencyStatus);
        var purchaseValue = Decimal(snapshots, x => x.PurchaseValue, currencyStatus, requiresSingleCurrency: true);
        var reach = new LongRangeMetricModel(null, MetricAvailability.NotAvailableForRange);
        var frequency = new DecimalRangeMetricModel(null, MetricAvailability.NotAvailableForRange);

        return new(
            adAccountId,
            campaignId,
            adSetId,
            adId,
            level,
            since,
            until,
            currencyStatus == RangeCurrencyStatus.Single ? currencies[0] : null,
            currencyStatus,
            coverage,
            new(spend, impressions, reach, clicks, leads, purchases, purchaseValue),
            new(
                frequency,
                Ratio(spend, impressions, 1000m),
                Ratio(clicks, impressions, 100m),
                Ratio(spend, clicks),
                Ratio(spend, leads),
                Ratio(spend, purchases),
                Ratio(purchaseValue, spend)));
    }

    public static bool HasActivity(IReadOnlyList<InsightSnapshot> snapshots) => snapshots.Any(x =>
        Positive(x.Spend) || Positive(x.Impressions) || Positive(x.Reach) || Positive(x.LinkClicks) ||
        Positive(x.Leads) || Positive(x.Purchases) || Positive(x.PurchaseValue));

    public static bool HasObservedSpend(IReadOnlyList<InsightSnapshot> snapshots) => snapshots.Any(x => Positive(x.Spend));

    private static DecimalRangeMetricModel Decimal(
        IReadOnlyList<InsightSnapshot> snapshots,
        Func<InsightSnapshot, decimal?> select,
        RangeCurrencyStatus currencyStatus,
        bool requiresSingleCurrency = false)
    {
        var availability = Availability(snapshots, currencyStatus, requiresSingleCurrency, x => select(x).HasValue);
        return availability == MetricAvailability.CompleteForSnapshots
            ? new(snapshots.Sum(x => select(x)!.Value), availability)
            : new(null, availability);
    }

    private static LongRangeMetricModel Long(IReadOnlyList<InsightSnapshot> snapshots, Func<InsightSnapshot, long?> select)
    {
        var availability = Availability(snapshots, RangeCurrencyStatus.Single, false, x => select(x).HasValue);
        return availability == MetricAvailability.CompleteForSnapshots
            ? new(snapshots.Sum(x => select(x)!.Value), availability)
            : new(null, availability);
    }

    private static MetricAvailability Availability(
        IReadOnlyList<InsightSnapshot> snapshots,
        RangeCurrencyStatus currencyStatus,
        bool requiresSingleCurrency,
        Func<InsightSnapshot, bool> hasValue)
    {
        if (snapshots.Count == 0) return MetricAvailability.NoData;
        if (snapshots.Any(x => x.ObservedDataQuality == InsightSnapshotDataQuality.LegacyZeroNormalized)) return MetricAvailability.LegacyZeroNormalized;
        if (requiresSingleCurrency && currencyStatus == RangeCurrencyStatus.Mixed) return MetricAvailability.MixedCurrency;
        return snapshots.All(hasValue) ? MetricAvailability.CompleteForSnapshots : MetricAvailability.Incomplete;
    }

    private static DecimalRangeMetricModel Ratio(DecimalRangeMetricModel numerator, LongRangeMetricModel denominator, decimal multiplier = 1m) =>
        Ratio(numerator.Value, numerator.Availability, denominator.Value, denominator.Availability, multiplier);

    private static DecimalRangeMetricModel Ratio(LongRangeMetricModel numerator, LongRangeMetricModel denominator, decimal multiplier = 1m) =>
        Ratio(numerator.Value, numerator.Availability, denominator.Value, denominator.Availability, multiplier);

    private static DecimalRangeMetricModel Ratio(DecimalRangeMetricModel numerator, DecimalRangeMetricModel denominator, decimal multiplier = 1m) =>
        Ratio(numerator.Value, numerator.Availability, denominator.Value, denominator.Availability, multiplier);

    private static DecimalRangeMetricModel Ratio(decimal? numerator, MetricAvailability numeratorAvailability, long? denominator, MetricAvailability denominatorAvailability, decimal multiplier) =>
        Ratio(numerator, numeratorAvailability, denominator is null ? null : (decimal?)denominator.Value, denominatorAvailability, multiplier);

    private static DecimalRangeMetricModel Ratio(long? numerator, MetricAvailability numeratorAvailability, long? denominator, MetricAvailability denominatorAvailability, decimal multiplier) =>
        Ratio(numerator is null ? null : (decimal?)numerator.Value, numeratorAvailability, denominator is null ? null : (decimal?)denominator.Value, denominatorAvailability, multiplier);

    private static DecimalRangeMetricModel Ratio(decimal? numerator, MetricAvailability numeratorAvailability, decimal? denominator, MetricAvailability denominatorAvailability, decimal multiplier)
    {
        var availability = Combine(numeratorAvailability, denominatorAvailability);
        if (availability != MetricAvailability.CompleteForSnapshots) return new(null, availability);
        if (!numerator.HasValue || !denominator.HasValue) return new(null, MetricAvailability.NoData);
        return denominator.Value > 0m
            ? new(numerator.Value / denominator.Value * multiplier, MetricAvailability.CompleteForSnapshots)
            : new(null, MetricAvailability.Undefined);
    }

    private static MetricAvailability Combine(MetricAvailability first, MetricAvailability second)
    {
        if (first == MetricAvailability.LegacyZeroNormalized || second == MetricAvailability.LegacyZeroNormalized) return MetricAvailability.LegacyZeroNormalized;
        if (first == MetricAvailability.MixedCurrency || second == MetricAvailability.MixedCurrency) return MetricAvailability.MixedCurrency;
        if (first == MetricAvailability.Incomplete || second == MetricAvailability.Incomplete) return MetricAvailability.Incomplete;
        if (first == MetricAvailability.NoData || second == MetricAvailability.NoData) return MetricAvailability.NoData;
        if (first == MetricAvailability.NotAvailableForRange || second == MetricAvailability.NotAvailableForRange) return MetricAvailability.NotAvailableForRange;
        return MetricAvailability.CompleteForSnapshots;
    }

    private static bool Positive(decimal? value) => value is > 0m;
    private static bool Positive(long? value) => value is > 0;
}
