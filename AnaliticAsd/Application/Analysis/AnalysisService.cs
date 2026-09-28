using AnaliticAsd.Application.AdAccounts;
using AnaliticAsd.Application.Advertising;
using AnaliticAsd.Application.Metrics;
using AnaliticAsd.Domain.Advertising;

namespace AnaliticAsd.Application.Analysis;

public sealed class AnalysisService(IAdAccountService accounts, IAdvertisingRepository advertising,
    IMetricsRepository metrics, TimeProvider clock, AnalysisRulesOptions rulesOptions) : IAnalysisService
{
    private static readonly string[] DefaultMetrics =
        ["spend", "impressions", "reach", "linkClicks", "leads", "purchases", "purchaseValue", "frequency", "cpm", "ctr", "cpc", "cpl", "cpa", "roas"];
    private static readonly HashSet<string> SupportedMetrics = new(DefaultMetrics, StringComparer.OrdinalIgnoreCase);

    public async Task<AnalysisResultModel> AnalyzeAsync(CreateAnalysisCommand command, CancellationToken ct = default)
    {
        var selectedMetrics = NormalizeMetrics(command.SelectedMetrics);
        ValidateRange(command.Since, command.Until);
        var account = await accounts.GetByIdAsync(command.AdAccountId, ct);
        var allCampaigns = (await advertising.CampaignsForSyncAsync(account.Id, ct)).Values
            .Where(x => x.IsPresentOnMeta).OrderBy(x => x.Name).ToArray();
        var selectedCampaigns = SelectCampaigns(allCampaigns, command.CampaignIds);
        var selectedCampaignIds = selectedCampaigns.Select(x => x.Id).ToHashSet();
        var allAdSets = (await advertising.AdSetsForSyncAsync(account.Id, ct)).Values
            .Where(x => x.IsPresentOnMeta && selectedCampaignIds.Contains(x.CampaignId)).OrderBy(x => x.Name).ToArray();
        var selectedAdSetIds = allAdSets.Select(x => x.Id).ToHashSet();
        var allAds = (await advertising.AdsForSyncAsync(account.Id, ct)).Values
            .Where(x => x.IsPresentOnMeta && selectedAdSetIds.Contains(x.AdSetId)).OrderBy(x => x.Name).ToArray();

        var currentSnapshots = await LoadSnapshots(account.Id, command.Since, command.Until, ct);
        (DateOnly Since, DateOnly Until)? baselineRange = command.Comparison is null ? null : ResolveComparisonRange(command);
        var baselineSnapshots = baselineRange is null
            ? null
            : await LoadSnapshots(account.Id, baselineRange.Value.Since, baselineRange.Value.Until, ct);

        RangeMetricsSummaryModel Summary(InsightLevel level, Guid? campaignId, Guid? adSetId, Guid? adId,
            DateOnly since, DateOnly until, IReadOnlyDictionary<InsightLevel, IReadOnlyList<InsightSnapshot>> snapshots)
        {
            var entityId = level switch { InsightLevel.Campaign => campaignId, InsightLevel.AdSet => adSetId, InsightLevel.Ad => adId, _ => null };
            var rows = snapshots[level].Where(x => level == InsightLevel.Account ||
                level == InsightLevel.Campaign && x.CampaignId == entityId ||
                level == InsightLevel.AdSet && x.AdSetId == entityId ||
                level == InsightLevel.Ad && x.AdId == entityId).ToArray();
            return RangeMetricsCalculator.Summarize(account.Id, campaignId, adSetId, adId, level, since, until, rows);
        }

        RangeMetricsComparisonModel? Compare(RangeMetricsSummaryModel current, InsightLevel level,
            Guid? campaignId, Guid? adSetId, Guid? adId)
        {
            if (command.Comparison is null || baselineRange is null || baselineSnapshots is null) return null;
            var baseline = Summary(level, campaignId, adSetId, adId, baselineRange.Value.Since,
                baselineRange.Value.Until, baselineSnapshots);
            return RangeComparisonCalculator.Compare(command.Comparison.Value, current, baseline);
        }

        var campaignSummaries = allCampaigns.ToDictionary(x => x.Id,
            x => Summary(InsightLevel.Campaign, x.Id, null, null, command.Since, command.Until, currentSnapshots));
        var benchmarks = BuildBenchmarks(allCampaigns, campaignSummaries);

        var accountSummary = Summary(InsightLevel.Account, null, null, null, command.Since, command.Until, currentSnapshots);
        var accountComparison = Compare(accountSummary, InsightLevel.Account, null, null, null);
        var accountAnalysis = Entity("Account", account.Id, account.ClientId, account.Name, null,
            accountSummary, accountComparison, null, selectedMetrics);

        var campaignResults = new List<AnalysisEntityModel>();
        var adSetResults = new List<AnalysisEntityModel>();
        var adResults = new List<AnalysisEntityModel>();
        foreach (var campaign in selectedCampaigns)
        {
            var summary = campaignSummaries[campaign.Id];
            var comparison = Compare(summary, InsightLevel.Campaign, campaign.Id, null, null);
            var benchmark = benchmarks.GetValueOrDefault(campaign.Id);
            campaignResults.Add(Entity("Campaign", campaign.Id, account.Id, campaign.Name, campaign.Objective,
                summary, comparison, benchmark, selectedMetrics));

            foreach (var adSet in allAdSets.Where(x => x.CampaignId == campaign.Id))
            {
                var adSetSummary = Summary(InsightLevel.AdSet, campaign.Id, adSet.Id, null,
                    command.Since, command.Until, currentSnapshots);
                adSetResults.Add(Entity("AdSet", adSet.Id, campaign.Id, adSet.Name, campaign.Objective,
                    adSetSummary, Compare(adSetSummary, InsightLevel.AdSet, campaign.Id, adSet.Id, null), null, selectedMetrics));
                foreach (var ad in allAds.Where(x => x.AdSetId == adSet.Id))
                {
                    var adSummary = Summary(InsightLevel.Ad, campaign.Id, adSet.Id, ad.Id,
                        command.Since, command.Until, currentSnapshots);
                    adResults.Add(Entity("Ad", ad.Id, adSet.Id, ad.Name, campaign.Objective,
                        adSummary, Compare(adSummary, InsightLevel.Ad, campaign.Id, adSet.Id, ad.Id), null, selectedMetrics));
                }
            }
        }

        var rules = AnalysisRulesEngine.Evaluate(campaignResults, rulesOptions);
        return new(account.Id, account.ClientId, command.Since, command.Until,
            command.Comparison?.ToString(), selectedMetrics, clock.GetUtcNow(), accountAnalysis,
            campaignResults, adSetResults, adResults,
            [new("Audiences", "Breakdown observations are not available yet."),
             new("Placements", "Breakdown observations are not available yet."),
             new("Creatives", "Creative assets and creative-level attributes are not available yet.")],
            rules.Insights, rules.Recommendations);
    }

    private async Task<IReadOnlyDictionary<InsightLevel, IReadOnlyList<InsightSnapshot>>> LoadSnapshots(
        Guid accountId, DateOnly since, DateOnly until, CancellationToken ct)
    {
        var result = new Dictionary<InsightLevel, IReadOnlyList<InsightSnapshot>>();
        foreach (var level in Enum.GetValues<InsightLevel>())
            result[level] = await metrics.ListForAccountAsync(accountId, level, since, until, ct);
        return result;
    }

    private static (DateOnly Since, DateOnly Until) ResolveComparisonRange(CreateAnalysisCommand command)
    {
        var days = command.Until.DayNumber - command.Since.DayNumber + 1;
        var range = command.Comparison switch
        {
            ComparisonPeriodType.PreviousPeriod => (command.Since.AddDays(-days), command.Since.AddDays(-1)),
            ComparisonPeriodType.PreviousMonth => (new DateOnly(command.Since.Year, command.Since.Month, 1).AddMonths(-1), new DateOnly(command.Since.Year, command.Since.Month, 1).AddDays(-1)),
            ComparisonPeriodType.PreviousYear => (command.Since.AddYears(-1), command.Until.AddYears(-1)),
            ComparisonPeriodType.Custom when command.ComparisonSince.HasValue && command.ComparisonUntil.HasValue => (command.ComparisonSince.Value, command.ComparisonUntil.Value),
            ComparisonPeriodType.Custom => throw new ArgumentException("'comparisonSince' and 'comparisonUntil' are required for Custom comparison."),
            _ => throw new ArgumentException("Unsupported comparison type.")
        };
        if (range.Item1 > range.Item2) throw new ArgumentException("'comparisonSince' must be on or before 'comparisonUntil'.");
        if (range.Item2 >= command.Since) throw new ArgumentException("The comparison range must end before the current range starts.");
        if (range.Item2.DayNumber - range.Item1.DayNumber + 1 > 90) throw new ArgumentException("The date range cannot exceed 90 days.");
        return range;
    }

    private static Dictionary<Guid, CampaignBenchmarkItemModel> BuildBenchmarks(
        IReadOnlyList<Campaign> campaigns, IReadOnlyDictionary<Guid, RangeMetricsSummaryModel> summaries) =>
        campaigns.ToDictionary(c => c.Id, c =>
        {
            var own = summaries[c.Id];
            var peers = campaigns.Where(x => x.Id != c.Id && string.Equals(x.Objective, c.Objective, StringComparison.OrdinalIgnoreCase))
                .Select(x => summaries[x.Id]).ToArray();
            IReadOnlyList<(DecimalRangeMetricModel Metric, string? Currency)> Values(Func<RangeMetricsSummaryModel, DecimalRangeMetricModel> select) =>
                peers.Select(x => (select(x), x.Currency)).ToArray();
            return new CampaignBenchmarkItemModel(c.Id, c.Name, c.Objective, own.Currency,
                CampaignBenchmarkCalculator.Calculate(own.Derived.Cpm, Values(x => x.Derived.Cpm), own.Currency, true),
                CampaignBenchmarkCalculator.Calculate(own.Derived.Ctr, Values(x => x.Derived.Ctr), own.Currency, false),
                CampaignBenchmarkCalculator.Calculate(own.Derived.Cpc, Values(x => x.Derived.Cpc), own.Currency, true),
                CampaignBenchmarkCalculator.Calculate(own.Derived.Cpl, Values(x => x.Derived.Cpl), own.Currency, true),
                CampaignBenchmarkCalculator.Calculate(own.Derived.Cpa, Values(x => x.Derived.Cpa), own.Currency, true),
                CampaignBenchmarkCalculator.Calculate(own.Derived.Roas, Values(x => x.Derived.Roas), own.Currency, false));
        });

    private static IReadOnlyList<Campaign> SelectCampaigns(IReadOnlyList<Campaign> campaigns, IReadOnlyList<Guid>? ids)
    {
        if (ids is null) return campaigns;
        if (ids.Count != ids.Distinct().Count()) throw new ArgumentException("Campaign IDs must be unique.");
        var selected = campaigns.Where(x => ids.Contains(x.Id)).ToArray();
        if (selected.Length != ids.Count) throw new ArgumentException("Campaign selection contains an unavailable campaign.");
        return selected;
    }

    private static IReadOnlyList<string> NormalizeMetrics(IReadOnlyList<string>? requested)
    {
        if (requested is null || requested.Count == 0) return DefaultMetrics;
        var result = requested.Select(x => x?.Trim() ?? string.Empty).ToArray();
        if (result.Any(string.IsNullOrWhiteSpace) || result.Any(x => !SupportedMetrics.Contains(x)))
            throw new ArgumentException($"Selected metrics must be one of: {string.Join(", ", DefaultMetrics)}.");
        if (result.Distinct(StringComparer.OrdinalIgnoreCase).Count() != result.Length)
            throw new ArgumentException("Selected metrics must be unique.");
        return result.Select(x => DefaultMetrics.Single(y => y.Equals(x, StringComparison.OrdinalIgnoreCase))).ToArray();
    }

    private void ValidateRange(DateOnly since, DateOnly until)
    {
        if (since == DateOnly.MinValue || until == DateOnly.MinValue) throw new ArgumentException("'since' and 'until' are required.");
        if (since > until) throw new ArgumentException("'since' must be on or before 'until'.");
        if (until.DayNumber - since.DayNumber + 1 > 90) throw new ArgumentException("The date range cannot exceed 90 days.");
        if (until > DateOnly.FromDateTime(clock.GetUtcNow().UtcDateTime)) throw new ArgumentException("The date range cannot end in the future.");
    }

    private static AnalysisEntityModel Entity(string level, Guid id, Guid? parentId, string name, string? objective,
        RangeMetricsSummaryModel summary, RangeMetricsComparisonModel? comparison,
        CampaignBenchmarkItemModel? benchmark, IReadOnlyList<string> selected)
    {
        var values = MetricValues(summary).Where(x => selected.Contains(x.Key)).ToDictionary();
        var changes = comparison is null ? null : ComparisonValues(comparison).Where(x => selected.Contains(x.Key)).ToDictionary();
        var benchmarkValues = benchmark is null ? null : BenchmarkValues(benchmark).Where(x => selected.Contains(x.Key)).ToDictionary();
        return new(level, id, parentId, name, objective, summary.Currency,
            new(summary.Coverage.RequestedDays, summary.Coverage.SnapshotDays,
                summary.Coverage.LegacyZeroNormalizedSnapshotDays, summary.Coverage.FirstSnapshotDate, summary.Coverage.LastSnapshotDate),
            Sufficiency(summary, values), values, changes, benchmarkValues);
    }

    private static AnalysisSufficiencyModel Sufficiency(RangeMetricsSummaryModel summary, IReadOnlyDictionary<string, AnalysisMetricModel> values)
    {
        var reasons = new List<string>();
        if (summary.Coverage.SnapshotDays == 0) reasons.Add("NoSnapshots");
        if (summary.Coverage.SnapshotDays < summary.Coverage.RequestedDays) reasons.Add("PartialDateCoverage");
        if (summary.Coverage.LegacyZeroNormalizedSnapshotDays > 0) reasons.Add("LegacyZeroNormalized");
        if (summary.CurrencyStatus == RangeCurrencyStatus.Mixed) reasons.Add("MixedCurrency");
        if (values.Values.Any(x => x.Availability is not "CompleteForSnapshots" and not "NotAvailableForRange")) reasons.Add("UnavailableSelectedMetrics");
        var status = reasons.Contains("NoSnapshots") ? "Insufficient" : reasons.Count == 0 ? "Sufficient" : "Partial";
        return new(status, reasons);
    }

    private static Dictionary<string, AnalysisMetricModel> MetricValues(RangeMetricsSummaryModel x) => new(StringComparer.OrdinalIgnoreCase)
    {
        ["spend"] = M(x.Observed.Spend, "Observed"),
        ["impressions"] = M(x.Observed.Impressions, "Observed"),
        ["reach"] = M(x.Observed.Reach, "Observed"),
        ["linkClicks"] = M(x.Observed.LinkClicks, "Observed"),
        ["leads"] = M(x.Observed.Leads, "Observed"),
        ["purchases"] = M(x.Observed.Purchases, "Observed"),
        ["purchaseValue"] = M(x.Observed.PurchaseValue, "Observed"),
        ["frequency"] = M(x.Derived.Frequency, "Derived"),
        ["cpm"] = M(x.Derived.Cpm, "Derived"),
        ["ctr"] = M(x.Derived.Ctr, "Derived"),
        ["cpc"] = M(x.Derived.Cpc, "Derived"),
        ["cpl"] = M(x.Derived.Cpl, "Derived"),
        ["cpa"] = M(x.Derived.Cpa, "Derived"),
        ["roas"] = M(x.Derived.Roas, "Derived")
    };
    private static AnalysisMetricModel M(DecimalRangeMetricModel x, string source) => new(x.Value, x.Availability.ToString(), source);
    private static AnalysisMetricModel M(LongRangeMetricModel x, string source) => new(x.Value, x.Availability.ToString(), source);

    private static Dictionary<string, AnalysisChangeModel> ComparisonValues(RangeMetricsComparisonModel x) => new(StringComparer.OrdinalIgnoreCase)
    {
        ["spend"] = C(x.Observed.Spend),
        ["impressions"] = C(x.Observed.Impressions),
        ["reach"] = C(x.Observed.Reach),
        ["linkClicks"] = C(x.Observed.LinkClicks),
        ["leads"] = C(x.Observed.Leads),
        ["purchases"] = C(x.Observed.Purchases),
        ["purchaseValue"] = C(x.Observed.PurchaseValue),
        ["frequency"] = C(x.Derived.Frequency),
        ["cpm"] = C(x.Derived.Cpm),
        ["ctr"] = C(x.Derived.Ctr),
        ["cpc"] = C(x.Derived.Cpc),
        ["cpl"] = C(x.Derived.Cpl),
        ["cpa"] = C(x.Derived.Cpa),
        ["roas"] = C(x.Derived.Roas)
    };
    private static AnalysisChangeModel C(DecimalMetricComparisonModel x) => new(x.Current, x.Baseline, x.AbsoluteChange, x.PercentageChange, x.Availability.ToString());
    private static AnalysisChangeModel C(LongMetricComparisonModel x) => new(x.Current, x.Baseline, x.AbsoluteChange, x.PercentageChange, x.Availability.ToString());

    private static Dictionary<string, AnalysisBenchmarkModel> BenchmarkValues(CampaignBenchmarkItemModel x) => new(StringComparer.OrdinalIgnoreCase)
    {
        ["cpm"] = B(x.Cpm),
        ["ctr"] = B(x.Ctr),
        ["cpc"] = B(x.Cpc),
        ["cpl"] = B(x.Cpl),
        ["cpa"] = B(x.Cpa),
        ["roas"] = B(x.Roas)
    };
    private static AnalysisBenchmarkModel B(BenchmarkMetricModel x) => new(x.CampaignValue, x.BenchmarkValue, x.AbsoluteDifference, x.PercentageDifference, x.ComparableCampaigns, x.Availability.ToString());
}
