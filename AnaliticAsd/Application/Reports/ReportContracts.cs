using AnaliticAsd.Application.Analysis;
using AnaliticAsd.Application.Metrics;
using AnaliticAsd.Domain.Reports;

namespace AnaliticAsd.Application.Reports;

public sealed record CreateReportCommand(string Title, Guid AdAccountId, DateOnly Since, DateOnly Until,
    ComparisonPeriodType? Comparison, DateOnly? ComparisonSince, DateOnly? ComparisonUntil,
    IReadOnlyList<Guid>? CampaignIds, IReadOnlyList<string>? SelectedMetrics);
public sealed record ReportDataModel(Guid ReportId, int SchemaVersion, string Title, Guid ClientId,
    Guid AdAccountId, Guid CreatedByUserId, DateTimeOffset CreatedAtUtc, string SnapshotHash,
    AnalysisResultModel Analysis);
public sealed record ReportListItemModel(Guid Id, string Title, Guid ClientId, Guid AdAccountId,
    DateOnly Since, DateOnly Until, DateTimeOffset CreatedAtUtc, int SchemaVersion, string SnapshotHash);

public interface IReportService
{
    Task<ReportDataModel> CreateAsync(CreateReportCommand command, CancellationToken ct = default);
    Task<IReadOnlyList<ReportListItemModel>> ListAsync(Guid clientId, int page, int pageSize, CancellationToken ct = default);
    Task<ReportDataModel> GetAsync(Guid reportId, CancellationToken ct = default);
    Task<byte[]> GetPdfAsync(Guid reportId, CancellationToken ct = default);
    Task DeleteAsync(Guid reportId, CancellationToken ct = default);
}

public interface IReportRepository
{
    Task<Report?> FindAsync(Guid id, bool track, CancellationToken ct);
    Task<IReadOnlyList<ReportListItemModel>> ListAsync(Guid clientId, int skip, int take, CancellationToken ct);
    void Add(Report report);
    void Remove(Report report);
    Task SaveAsync(CancellationToken ct);
}

public interface IReportPdfRenderer
{
    byte[] Render(ReportDataModel report);
}
