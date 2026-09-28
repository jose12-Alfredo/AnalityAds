using AnaliticAsd.Application.Reports;
using AnaliticAsd.Domain.Reports;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using AnaliticAsd.Application.Common;

namespace AnaliticAsd.Infrastructure.Persistence.Repositories;

public sealed class ReportShareRepository(AnalitiAdsDbContext db) : IReportShareRepository
{
    public async Task<IReadOnlyList<ReportShareLink>> ListAsync(Guid agencyId, Guid reportId, CancellationToken ct)
    {
        var links = await db.ReportShareLinks.AsNoTracking()
            .Where(x => x.AgencyId == agencyId && x.ReportId == reportId).ToArrayAsync(ct);
        return links.OrderByDescending(x => x.CreatedAtUtc).ThenByDescending(x => x.Id).ToArray();
    }

    public Task<ReportShareLink?> FindAsync(Guid agencyId, Guid reportId, Guid id, CancellationToken ct) =>
        db.ReportShareLinks.SingleOrDefaultAsync(x => x.AgencyId == agencyId && x.ReportId == reportId && x.Id == id, ct);

    public async Task<(ReportShareLink Link, Report Report)?> FindAvailableByHashAsync(string hash, DateTimeOffset now, CancellationToken ct)
    {
        var link = await db.ReportShareLinks.SingleOrDefaultAsync(x => x.TokenHash == hash, ct);
        if (link is null || !link.IsAvailable(now)) return null;
        var report = await db.Reports.AsNoTracking().SingleOrDefaultAsync(x => x.Id == link.ReportId, ct);
        if (report is null || !await db.Clients.AsNoTracking().AnyAsync(x => x.Id == report.ClientId
                && x.AgencyId == report.AgencyId && x.IsActive, ct)
            || !await db.Agencies.AsNoTracking().AnyAsync(x => x.Id == report.AgencyId && x.IsActive, ct)) return null;
        return (link, report);
    }

    public void Add(ReportShareLink link) => db.ReportShareLinks.Add(link);
    public void Add(ReportShareAuditEvent auditEvent) => db.ReportShareAuditEvents.Add(auditEvent);
    public async Task SaveAsync(CancellationToken ct)
    {
        try { await db.SaveChangesAsync(ct); }
        catch (DbUpdateConcurrencyException) { throw new ConflictException("The report share link changed concurrently. Refresh and try again."); }
        catch (DbUpdateException ex) when (ex.InnerException is PostgresException { SqlState: PostgresErrorCodes.UniqueViolation }
            or Microsoft.Data.Sqlite.SqliteException { SqliteExtendedErrorCode: 1555 or 2067 })
        { throw new ConflictException("The report share link already exists. Refresh and try again."); }
    }
}
