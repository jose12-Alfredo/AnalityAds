using AnaliticAsd.Application.Metrics;

namespace AnaliticAsd.Application.Analysis;

public sealed record CreateAnalysisCommand(Guid AdAccountId, DateOnly Since, DateOnly Until,
    ComparisonPeriodType? Comparison, DateOnly? ComparisonSince, DateOnly? ComparisonUntil,
    IReadOnlyList<Guid>? CampaignIds, IReadOnlyList<string>? SelectedMetrics);

public sealed record AnalysisMetricModel(decimal? Value, string Availability, string Source);
public sealed record AnalysisChangeModel(decimal? Current, decimal? Baseline, decimal? AbsoluteChange,
    decimal? PercentageChange, string Availability);
public sealed record AnalysisBenchmarkModel(decimal? EntityValue, decimal? BenchmarkValue,
    decimal? AbsoluteDifference, decimal? PercentageDifference, int ComparableEntities, string Availability);
public sealed record AnalysisCoverageModel(int RequestedDays, int SnapshotDays,
    int LegacyZeroNormalizedSnapshotDays, DateOnly? FirstSnapshotDate, DateOnly? LastSnapshotDate);
public sealed record AnalysisSufficiencyModel(string Status, IReadOnlyList<string> Reasons);
public sealed record AnalysisEntityModel(string Level, Guid Id, Guid? ParentId, string Name, string? Objective,
    string? Currency, AnalysisCoverageModel Coverage, AnalysisSufficiencyModel Sufficiency,
    IReadOnlyDictionary<string, AnalysisMetricModel> Metrics,
    IReadOnlyDictionary<string, AnalysisChangeModel>? Comparison,
    IReadOnlyDictionary<string, AnalysisBenchmarkModel>? Benchmarks);
public sealed record AnalysisUnavailableSectionModel(string Section, string Reason);
public sealed record AnalysisEvidenceModel(string Metric, decimal? Value, decimal? ReferenceValue,
    decimal? PercentageDifference, string Availability);
public sealed record AnalysisInsightModel(string RuleId, string Level, IReadOnlyList<Guid> EntityIds,
    string Severity, string Confidence, string Sufficiency, string Message,
    IReadOnlyList<AnalysisEvidenceModel> Evidence);
public sealed record AnalysisRecommendationModel(string RuleId, string Level, IReadOnlyList<Guid> EntityIds,
    string Priority, string Message, IReadOnlyList<string> Actions);
public sealed record AnalysisResultModel(Guid AdAccountId, Guid ClientId, DateOnly Since, DateOnly Until,
    string? ComparisonType, IReadOnlyList<string> SelectedMetrics, DateTimeOffset EvaluatedAtUtc,
    AnalysisEntityModel Account, IReadOnlyList<AnalysisEntityModel> Campaigns,
    IReadOnlyList<AnalysisEntityModel> AdSets, IReadOnlyList<AnalysisEntityModel> Ads,
    IReadOnlyList<AnalysisUnavailableSectionModel> UnavailableSections,
    IReadOnlyList<AnalysisInsightModel> Insights,
    IReadOnlyList<AnalysisRecommendationModel> Recommendations);

public interface IAnalysisService
{
    Task<AnalysisResultModel> AnalyzeAsync(CreateAnalysisCommand command, CancellationToken cancellationToken = default);
}
