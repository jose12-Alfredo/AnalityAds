namespace AnaliticAsd.Application.Analysis;

public sealed record AnalysisRulesOptions
{
    public int BudgetConcentrationTopCampaigns { get; init; } = 2;
    public decimal BudgetConcentrationPercentage { get; init; } = 40m;
    public decimal MaterialDifferencePercentage { get; init; } = 20m;
    public decimal StableCtrPercentage { get; init; } = 10m;
    public int MinimumComparableCampaigns { get; init; } = 2;
}

public sealed record AnalysisRulesResult(
    IReadOnlyList<AnalysisInsightModel> Insights,
    IReadOnlyList<AnalysisRecommendationModel> Recommendations);

public static class AnalysisRulesEngine
{
    public static AnalysisRulesResult Evaluate(
        IReadOnlyList<AnalysisEntityModel> campaigns,
        AnalysisRulesOptions options)
    {
        Validate(options);
        var insights = new List<AnalysisInsightModel>();
        var recommendations = new List<AnalysisRecommendationModel>();

        BudgetConcentration(campaigns, options, insights, recommendations);
        foreach (var campaign in campaigns.OrderBy(x => x.Id))
        {
            CampaignPerformance(campaign, options, insights, recommendations);
            PostClick(campaign, options, insights, recommendations);
            CreativeResponse(campaign, options, insights, recommendations);
            DeliveryCost(campaign, options, insights, recommendations);
        }

        return new(insights, recommendations);
    }

    private static void BudgetConcentration(IReadOnlyList<AnalysisEntityModel> campaigns, AnalysisRulesOptions options,
        ICollection<AnalysisInsightModel> insights, ICollection<AnalysisRecommendationModel> recommendations)
    {
        var eligible = campaigns.Where(HasSufficientEvidence)
            .Select(x => (Campaign: x, Spend: Metric(x, "spend")))
            .Where(x => Available(x.Spend) && x.Spend!.Value > 0m)
            .OrderByDescending(x => x.Spend!.Value).ThenBy(x => x.Campaign.Id).ToArray();
        if (eligible.Length < 2) return;
        var total = eligible.Sum(x => x.Spend!.Value!.Value);
        var top = eligible.Take(Math.Min(options.BudgetConcentrationTopCampaigns, eligible.Length)).ToArray();
        var topSpend = top.Sum(x => x.Spend!.Value!.Value);
        var percentage = topSpend / total * 100m;
        if (percentage < options.BudgetConcentrationPercentage) return;

        var ids = top.Select(x => x.Campaign.Id).ToArray();
        insights.Add(new("budget_concentration", "Account", ids, "Info", Confidence(eligible.Select(x => x.Campaign)),
            "Sufficient", "A material share of spend is concentrated in a small number of campaigns.",
            [new("topCampaignSpend", topSpend, total, percentage, "Available"),
             new("campaignCount", top.Length, eligible.Length, null, "Available")]));
        recommendations.Add(new("budget_concentration", "Account", ids, "Medium",
            "Review whether the current concentration matches the account's intended allocation.",
            ["Compare the concentrated campaigns by objective and efficiency.", "Confirm that concentration is intentional before reallocating budget."]));
    }

    private static void CampaignPerformance(AnalysisEntityModel campaign, AnalysisRulesOptions options,
        ICollection<AnalysisInsightModel> insights, ICollection<AnalysisRecommendationModel> recommendations)
    {
        if (!HasSufficientEvidence(campaign) || campaign.Benchmarks is null) return;
        var relevant = ObjectiveMetrics(campaign.Objective);
        var favorable = relevant.Select(x => Signal(campaign, x.Metric, x.LowerIsBetter, options, favorable: true)).Where(x => x is not null).Cast<AnalysisEvidenceModel>().ToArray();
        var unfavorable = relevant.Select(x => Signal(campaign, x.Metric, x.LowerIsBetter, options, favorable: false)).Where(x => x is not null).Cast<AnalysisEvidenceModel>().ToArray();
        if (favorable.Length >= 2)
        {
            AddCampaign("standout_campaign", campaign, "Positive", favorable,
                "The campaign outperforms comparable campaigns across multiple objective-relevant signals.",
                "Preserve the campaign's current learning while evaluating a controlled expansion.",
                ["Inspect the contributing ad sets and ads.", "Scale gradually and monitor the same efficiency signals."], insights, recommendations);
        }
        else if (unfavorable.Length >= 2)
        {
            AddCampaign("underperforming_campaign", campaign, "Warning", unfavorable,
                "The campaign shows lower efficiency than its comparable group across multiple signals.",
                "Review the campaign before increasing its budget.",
                ["Identify the ad sets and ads contributing most to the gap.", "Check tracking and offer context before changing delivery."], insights, recommendations);
        }
    }

    private static void PostClick(AnalysisEntityModel campaign, AnalysisRulesOptions options,
        ICollection<AnalysisInsightModel> insights, ICollection<AnalysisRecommendationModel> recommendations)
    {
        var ctr = Benchmark(campaign, "ctr");
        var cpc = Benchmark(campaign, "cpc");
        var outcome = Benchmark(campaign, campaign.Objective?.Contains("LEAD", StringComparison.OrdinalIgnoreCase) == true ? "cpl" : "cpa");
        if (!BenchmarkAvailable(ctr, options) || !BenchmarkAvailable(cpc, options) || !BenchmarkAvailable(outcome, options)) return;
        if (ctr!.PercentageDifference < options.MaterialDifferencePercentage || cpc!.PercentageDifference > 0m ||
            outcome!.PercentageDifference < options.MaterialDifferencePercentage) return;
        AddCampaign("possible_post_click_issue", campaign, "Warning",
            [Evidence("ctr", ctr), Evidence("cpc", cpc), Evidence(campaign.Objective?.Contains("LEAD", StringComparison.OrdinalIgnoreCase) == true ? "cpl" : "cpa", outcome)],
            "Traffic response is competitive while the outcome cost is weaker than the comparable group.",
            "Review the experience and measurement after the click; the available evidence does not identify a cause.",
            ["Validate conversion tracking.", "Review landing page, offer and conversion flow."], insights, recommendations);
    }

    private static void CreativeResponse(AnalysisEntityModel campaign, AnalysisRulesOptions options,
        ICollection<AnalysisInsightModel> insights, ICollection<AnalysisRecommendationModel> recommendations)
    {
        var cpm = Benchmark(campaign, "cpm");
        var ctr = Benchmark(campaign, "ctr");
        if (!BenchmarkAvailable(cpm, options) || !BenchmarkAvailable(ctr, options)) return;
        if (Math.Abs(cpm!.PercentageDifference!.Value) > options.MaterialDifferencePercentage ||
            ctr!.PercentageDifference > -options.MaterialDifferencePercentage) return;
        AddCampaign("weak_ad_response", campaign, "Warning", [Evidence("cpm", cpm), Evidence("ctr", ctr)],
            "Delivery cost is comparable, but response is below the campaign peer benchmark.",
            "Review the campaign's ads and message while preserving the current delivery context.",
            ["Compare ad-level CTR and volume.", "Test one creative variable at a time."], insights, recommendations);
    }

    private static void DeliveryCost(AnalysisEntityModel campaign, AnalysisRulesOptions options,
        ICollection<AnalysisInsightModel> insights, ICollection<AnalysisRecommendationModel> recommendations)
    {
        var cpm = Change(campaign, "cpm");
        var cpc = Change(campaign, "cpc");
        var ctr = Change(campaign, "ctr");
        if (!ChangeAvailable(cpm) || !ChangeAvailable(cpc) || !ChangeAvailable(ctr)) return;
        if (cpm!.PercentageChange < options.MaterialDifferencePercentage || cpc!.PercentageChange < options.MaterialDifferencePercentage ||
            Math.Abs(ctr!.PercentageChange!.Value) > options.StableCtrPercentage) return;
        AddCampaign("more_expensive_delivery", campaign, "Warning",
            [Evidence("cpm", cpm), Evidence("cpc", cpc), Evidence("ctr", ctr)],
            "CPM and CPC increased while CTR remained relatively stable.",
            "Investigate delivery conditions before attributing the cost increase to creative response.",
            ["Review auction, placement and schedule changes when those breakdowns become available.", "Monitor CPM, CPC and CTR together in the next period."], insights, recommendations);
    }

    private static void AddCampaign(string ruleId, AnalysisEntityModel campaign, string severity,
        IReadOnlyList<AnalysisEvidenceModel> evidence, string message, string recommendation,
        IReadOnlyList<string> actions, ICollection<AnalysisInsightModel> insights,
        ICollection<AnalysisRecommendationModel> recommendations)
    {
        insights.Add(new(ruleId, "Campaign", [campaign.Id], severity, Confidence([campaign]), campaign.Sufficiency.Status,
            message, evidence));
        recommendations.Add(new(ruleId, "Campaign", [campaign.Id], severity == "Warning" ? "High" : "Medium",
            recommendation, actions));
    }

    private static AnalysisEvidenceModel? Signal(AnalysisEntityModel campaign, string metric, bool lowerIsBetter,
        AnalysisRulesOptions options, bool favorable)
    {
        var value = Benchmark(campaign, metric);
        if (!BenchmarkAvailable(value, options)) return null;
        var difference = value!.PercentageDifference!.Value;
        var passes = favorable
            ? lowerIsBetter ? difference <= -options.MaterialDifferencePercentage : difference >= options.MaterialDifferencePercentage
            : lowerIsBetter ? difference >= options.MaterialDifferencePercentage : difference <= -options.MaterialDifferencePercentage;
        return passes ? Evidence(metric, value) : null;
    }

    private static (string Metric, bool LowerIsBetter)[] ObjectiveMetrics(string? objective) =>
        objective?.ToUpperInvariant() switch
        {
            var x when x?.Contains("SALE") == true => [("cpa", true), ("roas", false), ("ctr", false)],
            var x when x?.Contains("LEAD") == true => [("cpl", true), ("ctr", false), ("cpc", true)],
            var x when x?.Contains("TRAFFIC") == true => [("cpc", true), ("ctr", false), ("cpm", true)],
            _ => [("cpm", true), ("ctr", false), ("cpc", true)]
        };

    private static bool HasSufficientEvidence(AnalysisEntityModel entity) => entity.Sufficiency.Status == "Sufficient";
    private static string Confidence(IEnumerable<AnalysisEntityModel> entities) => entities.All(x => x.Sufficiency.Status == "Sufficient") ? "High" : "Medium";
    private static AnalysisMetricModel? Metric(AnalysisEntityModel entity, string name) => entity.Metrics.GetValueOrDefault(name);
    private static AnalysisBenchmarkModel? Benchmark(AnalysisEntityModel entity, string name) => entity.Benchmarks?.GetValueOrDefault(name);
    private static AnalysisChangeModel? Change(AnalysisEntityModel entity, string name) => entity.Comparison?.GetValueOrDefault(name);
    private static bool Available(AnalysisMetricModel? metric) => metric is { Availability: "CompleteForSnapshots", Value: not null };
    private static bool BenchmarkAvailable(AnalysisBenchmarkModel? metric, AnalysisRulesOptions options) =>
        metric is { Availability: "Available", EntityValue: not null, BenchmarkValue: not null, PercentageDifference: not null }
        && metric.ComparableEntities >= options.MinimumComparableCampaigns;
    private static bool ChangeAvailable(AnalysisChangeModel? metric) => metric is { Availability: "Available", PercentageChange: not null };
    private static AnalysisEvidenceModel Evidence(string metric, AnalysisBenchmarkModel value) =>
        new(metric, value.EntityValue, value.BenchmarkValue, value.PercentageDifference, value.Availability);
    private static AnalysisEvidenceModel Evidence(string metric, AnalysisChangeModel value) =>
        new(metric, value.Current, value.Baseline, value.PercentageChange, value.Availability);

    private static void Validate(AnalysisRulesOptions options)
    {
        if (options.BudgetConcentrationTopCampaigns < 1) throw new InvalidOperationException("AnalysisRules:BudgetConcentrationTopCampaigns must be positive.");
        if (options.BudgetConcentrationPercentage is <= 0m or > 100m) throw new InvalidOperationException("AnalysisRules:BudgetConcentrationPercentage must be between 0 and 100.");
        if (options.MaterialDifferencePercentage <= 0m || options.StableCtrPercentage < 0m || options.MinimumComparableCampaigns < 1)
            throw new InvalidOperationException("AnalysisRules thresholds must be valid positive values.");
    }
}
