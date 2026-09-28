namespace AnaliticAsd.Application.Metrics;

internal static class RangeComparisonCalculator
{
    public static RangeMetricsComparisonModel Compare(ComparisonPeriodType type, RangeMetricsSummaryModel current, RangeMetricsSummaryModel baseline) => new(type, current, baseline,
        new(Decimal(current.Observed.Spend, baseline.Observed.Spend, MoneyCompatible(current, baseline)), Long(current.Observed.Impressions, baseline.Observed.Impressions), Long(current.Observed.Reach, baseline.Observed.Reach), Long(current.Observed.LinkClicks, baseline.Observed.LinkClicks), Decimal(current.Observed.Leads, baseline.Observed.Leads), Decimal(current.Observed.Purchases, baseline.Observed.Purchases), Decimal(current.Observed.PurchaseValue, baseline.Observed.PurchaseValue, MoneyCompatible(current, baseline))),
        new(Decimal(current.Derived.Frequency, baseline.Derived.Frequency), Decimal(current.Derived.Cpm, baseline.Derived.Cpm, MoneyCompatible(current, baseline)), Decimal(current.Derived.Ctr, baseline.Derived.Ctr), Decimal(current.Derived.Cpc, baseline.Derived.Cpc, MoneyCompatible(current, baseline)), Decimal(current.Derived.Cpl, baseline.Derived.Cpl, MoneyCompatible(current, baseline)), Decimal(current.Derived.Cpa, baseline.Derived.Cpa, MoneyCompatible(current, baseline)), Decimal(current.Derived.Roas, baseline.Derived.Roas, MoneyCompatible(current, baseline))));

    private static bool MoneyCompatible(RangeMetricsSummaryModel a, RangeMetricsSummaryModel b) => a.CurrencyStatus == RangeCurrencyStatus.Single && b.CurrencyStatus == RangeCurrencyStatus.Single && a.Currency == b.Currency;
    private static DecimalMetricComparisonModel Decimal(DecimalRangeMetricModel current, DecimalRangeMetricModel baseline, bool compatible = true)
    {
        if (!compatible) return new(current.Value, baseline.Value, null, null, ComparisonAvailability.CurrencyMismatch);
        if (current.Availability != MetricAvailability.CompleteForSnapshots || current.Value is null) return new(current.Value, baseline.Value, null, null, ComparisonAvailability.CurrentUnavailable);
        if (baseline.Availability != MetricAvailability.CompleteForSnapshots || baseline.Value is null) return new(current.Value, baseline.Value, null, null, ComparisonAvailability.BaselineUnavailable);
        var absolute = current.Value.Value - baseline.Value.Value;
        return baseline.Value.Value == 0m ? new(current.Value, baseline.Value, absolute, null, ComparisonAvailability.UndefinedBaseline) : new(current.Value, baseline.Value, absolute, absolute / Math.Abs(baseline.Value.Value) * 100m, ComparisonAvailability.Available);
    }
    private static LongMetricComparisonModel Long(LongRangeMetricModel current, LongRangeMetricModel baseline)
    {
        if (current.Availability != MetricAvailability.CompleteForSnapshots || current.Value is null) return new(current.Value, baseline.Value, null, null, ComparisonAvailability.CurrentUnavailable);
        if (baseline.Availability != MetricAvailability.CompleteForSnapshots || baseline.Value is null) return new(current.Value, baseline.Value, null, null, ComparisonAvailability.BaselineUnavailable);
        var absolute = current.Value.Value - baseline.Value.Value;
        return baseline.Value.Value == 0 ? new(current.Value, baseline.Value, absolute, null, ComparisonAvailability.UndefinedBaseline) : new(current.Value, baseline.Value, absolute, (decimal)absolute / Math.Abs(baseline.Value.Value) * 100m, ComparisonAvailability.Available);
    }
}
