namespace AnaliticAsd.Contracts.Analysis;

public sealed record CreateAnalysisRequest(Guid AdAccountId, DateOnly Since, DateOnly Until,
    string? Comparison, DateOnly? ComparisonSince, DateOnly? ComparisonUntil,
    IReadOnlyList<Guid>? CampaignIds, IReadOnlyList<string>? SelectedMetrics);
