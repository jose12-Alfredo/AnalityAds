namespace AnaliticAsd.Application.Metrics;

internal static class CampaignBenchmarkCalculator
{
    public static BenchmarkMetricModel Calculate(DecimalRangeMetricModel campaign, IReadOnlyList<(DecimalRangeMetricModel Metric, string? Currency)> peers, string? currency, bool monetary)
    {
        if (campaign.Availability != MetricAvailability.CompleteForSnapshots || campaign.Value is null) return new(campaign.Value, null, null, null, 0, BenchmarkAvailability.MetricUnavailable);
        var metrics = peers.Where(x => x.Metric.Availability == MetricAvailability.CompleteForSnapshots && x.Metric.Value.HasValue).ToArray();
        var available = metrics.Where(x => !monetary || x.Currency == currency).Select(x => x.Metric.Value!.Value).OrderBy(x => x).ToArray();
        if (available.Length < 2) return new(campaign.Value, null, null, null, available.Length, monetary && metrics.Length >= 2 && metrics.Any(x => x.Currency != currency) ? BenchmarkAvailability.CurrencyMismatch : BenchmarkAvailability.InsufficientComparableCampaigns);
        var median = available.Length % 2 == 1 ? available[available.Length / 2] : (available[available.Length / 2 - 1] + available[available.Length / 2]) / 2m;
        var difference = campaign.Value.Value - median;
        return median == 0m ? new(campaign.Value, median, difference, null, available.Length, BenchmarkAvailability.UndefinedBenchmark) : new(campaign.Value, median, difference, difference / Math.Abs(median) * 100m, available.Length, BenchmarkAvailability.Available);
    }
}
