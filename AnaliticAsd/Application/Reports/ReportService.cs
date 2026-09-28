using AnaliticAsd.Application.Analysis;
using AnaliticAsd.Application.Common;
using AnaliticAsd.Application.Clients;
using AnaliticAsd.Application.Identity;
using AnaliticAsd.Domain.Identity;
using AnaliticAsd.Domain.Reports;

namespace AnaliticAsd.Application.Reports;

public sealed class ReportService(IAnalysisService analysis, IReportRepository repository,
    IReportPdfRenderer pdf, IClientService clients, ICurrentTenant tenant, TimeProvider clock) : IReportService
{
    public async Task<ReportDataModel> CreateAsync(CreateReportCommand command, CancellationToken ct = default)
    {
        RequireCreator();
        ArgumentException.ThrowIfNullOrWhiteSpace(command.Title);
        if (command.Title.Trim().Length > Report.MaxTitleLength)
            throw new ArgumentException($"Report title cannot exceed {Report.MaxTitleLength} characters.");
        var result = await analysis.AnalyzeAsync(new(command.AdAccountId, command.Since, command.Until,
            command.Comparison, command.ComparisonSince, command.ComparisonUntil, command.CampaignIds, command.SelectedMetrics), ct);
        var now = clock.GetUtcNow();
        var reportId = Guid.NewGuid();
        var provisional = new ReportDataModel(reportId, Report.CurrentSchemaVersion, command.Title.Trim(), result.ClientId,
            result.AdAccountId, tenant.UserId, now, string.Empty, result);
        var hash = ReportSnapshot.HashCanonical(provisional);
        var snapshot = provisional with { SnapshotHash = hash };
        var snapshotJson = ReportSnapshot.Serialize(snapshot);
        var report = Report.Create(reportId, tenant.AgencyId, result.ClientId, result.AdAccountId, tenant.UserId,
            command.Title, command.Since, command.Until, snapshotJson, hash, now);
        repository.Add(report);
        await repository.SaveAsync(ct);
        return Deserialize(report);
    }

    public async Task<IReadOnlyList<ReportListItemModel>> ListAsync(Guid clientId, int page, int pageSize, CancellationToken ct = default)
    {
        if (page < 1) throw new ArgumentException("Page must be at least 1.");
        if (pageSize is < 1 or > 100) throw new ArgumentException("Page size must be between 1 and 100.");
        await clients.GetByIdAsync(clientId, ct);
        return await repository.ListAsync(clientId, (page - 1) * pageSize, pageSize, ct);
    }

    public async Task<ReportDataModel> GetAsync(Guid reportId, CancellationToken ct = default) =>
        Deserialize(await Required(reportId, false, ct));

    public async Task<byte[]> GetPdfAsync(Guid reportId, CancellationToken ct = default) =>
        pdf.Render(Deserialize(await Required(reportId, false, ct)));

    public async Task DeleteAsync(Guid reportId, CancellationToken ct = default)
    {
        if (tenant.Role is not (nameof(AgencyRole.Owner) or nameof(AgencyRole.Admin)))
            throw new ForbiddenException("Only Owner and Admin can delete reports.");
        repository.Remove(await Required(reportId, true, ct));
        await repository.SaveAsync(ct);
    }

    private void RequireCreator()
    {
        if (tenant.Role is not (nameof(AgencyRole.Owner) or nameof(AgencyRole.Admin) or nameof(AgencyRole.Analyst)))
            throw new ForbiddenException("Only Owner, Admin and Analyst can create reports.");
    }

    private async Task<Report> Required(Guid id, bool track, CancellationToken ct) =>
        await repository.FindAsync(id, track, ct) ?? throw new EntityNotFoundException("Report was not found.");
    private static ReportDataModel Deserialize(Report report) => ReportSnapshot.ReadAndVerify(report);
}
