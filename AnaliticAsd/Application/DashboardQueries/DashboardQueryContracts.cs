using AnaliticAsd.Domain.Advertising;
using AnaliticAsd.Domain.DataSources;

namespace AnaliticAsd.Application.DashboardQueries;

public sealed record MetricCatalogItemModel(string Key, string Name, string Description, string Provider,
    string Unit, string Aggregation, string ValueType, IReadOnlyList<string> SupportedDimensions,
    string? Limitation);
public sealed record DimensionCatalogItemModel(string Key, string Name, string Provider,
    string Granularity, IReadOnlyList<string> CompatibleMetrics, string? Limitation);
public sealed record DashboardDataCatalogModel(int SchemaVersion, IReadOnlyList<string> Providers,
    IReadOnlyList<MetricCatalogItemModel> Metrics, IReadOnlyList<DimensionCatalogItemModel> Dimensions);

public sealed record DashboardDataSourceModel(Guid Id, Guid ClientId, string Provider, string SourceType,
    string Name, string? Currency, string TimeZone, bool IsActive, DateOnly? AvailableSince,
    DateOnly? AvailableUntil, DateTimeOffset? LastSyncedAtUtc);

public sealed record DashboardQueryCommand(Guid ClientId, Guid DataSourceId, DateOnly Since, DateOnly Until,
    string? Dimension, IReadOnlyList<string> Metrics, IReadOnlyList<Guid>? CampaignIds, int? Limit,
    string? SortMetric, string? SortDirection, IReadOnlyList<string>? DimensionValues = null);
public sealed record DashboardQueryMetricModel(decimal? Value, string Availability, string Unit);
public sealed record DashboardQueryRowModel(string Key, string Label, string? DimensionValue,
    IReadOnlyDictionary<string, DashboardQueryMetricModel> Metrics);
public sealed record DashboardQueryCoverageModel(int RequestedDays, int SnapshotDays,
    DateOnly? FirstSnapshotDate, DateOnly? LastSnapshotDate);
public sealed record DashboardQueryResultModel(Guid ClientId, Guid DataSourceId, string Provider,
    string Dimension, DateOnly Since, DateOnly Until, string? Currency, string TimeZone,
    DateTimeOffset? LastSyncedAtUtc, DashboardQueryCoverageModel Coverage,
    IReadOnlyList<DashboardQueryRowModel> Rows);

public sealed record DashboardQuerySource(DataSource Source);

public interface IDashboardQueryRepository
{
    Task<IReadOnlyList<DataSource>> ListSourcesAsync(Guid clientId, CancellationToken cancellationToken = default);
    Task<DataSource?> GetSourceAsync(Guid clientId, Guid sourceId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<InsightSnapshot>> ListSnapshotsAsync(Guid sourceId, InsightLevel level,
        DateOnly since, DateOnly until, CancellationToken cancellationToken = default);
    Task<IReadOnlyDictionary<Guid, string>> CampaignNamesAsync(Guid sourceId,
        CancellationToken cancellationToken = default);
    Task<IReadOnlyList<ProviderMetricSnapshot>> ListProviderSnapshotsAsync(Guid sourceId,
        DateOnly since, DateOnly until, CancellationToken cancellationToken = default);
}

public interface IDashboardQueryService
{
    DashboardDataCatalogModel Catalog(string? provider = null);
    Task<IReadOnlyList<DashboardDataSourceModel>> ListSourcesAsync(Guid clientId,
        CancellationToken cancellationToken = default);
    Task<DashboardQueryResultModel> QueryAsync(DashboardQueryCommand command,
        CancellationToken cancellationToken = default);
}
