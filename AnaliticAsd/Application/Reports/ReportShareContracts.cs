using AnaliticAsd.Application.Analysis;
using AnaliticAsd.Domain.Reports;

namespace AnaliticAsd.Application.Reports;

public sealed record ReportShareLinkModel(Guid Id, Guid ReportId, string Status,
    DateTimeOffset CreatedAtUtc, DateTimeOffset ExpiresAtUtc, DateTimeOffset? RevokedAtUtc);
public sealed record CreatedReportShareLinkModel(ReportShareLinkModel ShareLink, string AccessToken, string SharePath);
public sealed record SharedReportDataModel(string Title, DateTimeOffset CreatedAtUtc, AnalysisResultModel Analysis);

public interface IReportShareService
{
    Task<IReadOnlyList<ReportShareLinkModel>> ListAsync(Guid reportId, CancellationToken ct = default);
    Task<CreatedReportShareLinkModel> CreateAsync(Guid reportId, int expirationDays, CancellationToken ct = default);
    Task RevokeAsync(Guid reportId, Guid shareLinkId, CancellationToken ct = default);
    Task<SharedReportDataModel> AccessAsync(string token, string operation, CancellationToken ct = default);
    Task<byte[]> PdfAsync(string token, CancellationToken ct = default);
}

public interface IReportShareRepository
{
    Task<IReadOnlyList<ReportShareLink>> ListAsync(Guid agencyId, Guid reportId, CancellationToken ct);
    Task<ReportShareLink?> FindAsync(Guid agencyId, Guid reportId, Guid id, CancellationToken ct);
    Task<(ReportShareLink Link, Report Report)?> FindAvailableByHashAsync(string hash, DateTimeOffset now, CancellationToken ct);
    void Add(ReportShareLink link);
    void Add(ReportShareAuditEvent auditEvent);
    Task SaveAsync(CancellationToken ct);
}
