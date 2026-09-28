using AnaliticAsd.Application.Analysis;
using System.Text.Json;

namespace AnaliticAsd.Tests.Analysis;

public sealed class AnalysisRulesEngineTests
{
    private static readonly AnalysisRulesOptions Options = new();

    [Fact]
    public void Budget_concentration_is_structured_and_deterministic()
    {
        var campaigns = new[]
        {
            Campaign(Guid.Parse("00000000-0000-0000-0000-000000000001"), 70m),
            Campaign(Guid.Parse("00000000-0000-0000-0000-000000000002"), 20m),
            Campaign(Guid.Parse("00000000-0000-0000-0000-000000000003"), 10m)
        };

        var first = AnalysisRulesEngine.Evaluate(campaigns, Options);
        var second = AnalysisRulesEngine.Evaluate(campaigns, Options);

        var insight = Assert.Single(first.Insights, x => x.RuleId == "budget_concentration");
        Assert.Equal("Account", insight.Level);
        Assert.Equal(90m, insight.Evidence.Single(x => x.Metric == "topCampaignSpend").Value);
        Assert.Equal(90m, insight.Evidence.Single(x => x.Metric == "topCampaignSpend").PercentageDifference);
        Assert.Equal(JsonSerializer.Serialize(first), JsonSerializer.Serialize(second));
        Assert.Single(first.Recommendations, x => x.RuleId == insight.RuleId);
    }

    [Fact]
    public void Underperformance_requires_multiple_available_objective_signals()
    {
        var campaign = Campaign(Guid.NewGuid(), 100m, "SALES", new Dictionary<string, AnalysisBenchmarkModel>
        {
            ["cpa"] = Benchmark(30m, 20m, 50m),
            ["roas"] = Benchmark(1m, 2m, -50m),
            ["ctr"] = Benchmark(2m, 2m, 0m)
        });

        var result = AnalysisRulesEngine.Evaluate([campaign], Options);

        var insight = Assert.Single(result.Insights, x => x.RuleId == "underperforming_campaign");
        Assert.Equal(["cpa", "roas"], insight.Evidence.Select(x => x.Metric).ToArray());
        Assert.Single(result.Recommendations, x => x.RuleId == insight.RuleId);
    }

    [Fact]
    public void Partial_or_unavailable_evidence_does_not_generate_a_conclusion()
    {
        var partial = Campaign(Guid.NewGuid(), 100m, "SALES", new Dictionary<string, AnalysisBenchmarkModel>
        {
            ["cpa"] = Benchmark(30m, 20m, 50m),
            ["roas"] = Benchmark(1m, 2m, -50m)
        }, "Partial");
        var unavailable = Campaign(Guid.NewGuid(), 100m, "SALES", new Dictionary<string, AnalysisBenchmarkModel>
        {
            ["cpa"] = new(30m, 20m, 10m, 50m, 2, "MetricUnavailable"),
            ["roas"] = Benchmark(1m, 2m, -50m)
        });

        var result = AnalysisRulesEngine.Evaluate([partial, unavailable], Options);

        Assert.DoesNotContain(result.Insights, x => x.RuleId is "standout_campaign" or "underperforming_campaign");
    }

    [Fact]
    public void More_expensive_delivery_requires_stable_ctr_and_available_comparison()
    {
        var comparison = new Dictionary<string, AnalysisChangeModel>
        {
            ["cpm"] = Change(12m, 10m, 20m),
            ["cpc"] = Change(1.3m, 1m, 30m),
            ["ctr"] = Change(2.1m, 2m, 5m)
        };
        var campaign = Campaign(Guid.NewGuid(), 100m) with { Comparison = comparison };

        var result = AnalysisRulesEngine.Evaluate([campaign], Options);

        Assert.Single(result.Insights, x => x.RuleId == "more_expensive_delivery");
    }

    private static AnalysisEntityModel Campaign(Guid id, decimal spend, string objective = "LEADS",
        IReadOnlyDictionary<string, AnalysisBenchmarkModel>? benchmarks = null, string sufficiency = "Sufficient") =>
        new("Campaign", id, Guid.Empty, $"Campaign {id}", objective, "USD",
            new(5, 5, 0, new DateOnly(2026, 9, 1), new DateOnly(2026, 9, 5)),
            new(sufficiency, sufficiency == "Sufficient" ? [] : ["PartialDateCoverage"]),
            new Dictionary<string, AnalysisMetricModel> { ["spend"] = new(spend, "CompleteForSnapshots", "Observed") },
            null, benchmarks);

    private static AnalysisBenchmarkModel Benchmark(decimal value, decimal reference, decimal difference) =>
        new(value, reference, value - reference, difference, 2, "Available");

    private static AnalysisChangeModel Change(decimal current, decimal baseline, decimal percentage) =>
        new(current, baseline, current - baseline, percentage, "Available");
}
