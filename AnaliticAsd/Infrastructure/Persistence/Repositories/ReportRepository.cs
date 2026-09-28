using AnaliticAsd.Application.Identity;
using AnaliticAsd.Application.Reports;
using AnaliticAsd.Domain.Reports;
using Microsoft.EntityFrameworkCore;

namespace AnaliticAsd.Infrastructure.Persistence.Repositories;

public sealed class ReportRepository(AnalitiAdsDbContext db, ICurrentTenant tenant) : IReportRepository
{
    private IQueryable<Report> Scoped
    {
        get
        {
            var clients = new AuthorizedData(db, tenant).Clients;
            return db.Reports.Where(x => x.AgencyId == tenant.AgencyId && clients.Any(c => c.Id == x.ClientId));
        }
    }

    public Task<Report?> FindAsync(Guid id, bool track, CancellationToken ct)
    {
        var query = Scoped.Where(x => x.Id == id);
        return (track ? query : query.AsNoTracking()).SingleOrDefaultAsync(ct);
    }

    public async Task<IReadOnlyList<ReportListItemModel>> ListAsync(Guid clientId, int skip, int take, CancellationToken ct)
    {
        var reports = await Scoped.AsNoTracking().Where(x => x.ClientId == clientId)
            .Select(x => new ReportListItemModel(x.Id, x.Title, x.ClientId, x.AdAccountId, x.Since, x.Until,
                x.CreatedAtUtc, x.SchemaVersion, x.SnapshotHash)).ToArrayAsync(ct);
        return reports.OrderByDescending(x => x.CreatedAtUtc).ThenByDescending(x => x.Id).Skip(skip).Take(take).ToArray();
    }

    public void Add(Report report) => db.Reports.Add(report);
    public void Remove(Report report) => db.Reports.Remove(report);
    public async Task SaveAsync(CancellationToken ct) => await db.SaveChangesAsync(ct);
}
