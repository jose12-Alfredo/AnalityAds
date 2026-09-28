namespace AnaliticAsd.Contracts.DashboardQueries;

public sealed record DashboardQueryRequest(Guid ClientId, Guid DataSourceId, DateOnly Since, DateOnly Until,
    string? Dimension, IReadOnlyList<string> Metrics, IReadOnlyList<Guid>? CampaignIds, int? Limit,
    string? SortMetric, string? SortDirection, IReadOnlyList<string>? DimensionValues = null);
