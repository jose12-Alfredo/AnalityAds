using AnaliticAsd.Application.DashboardQueries;
using AnaliticAsd.Application.Identity;
using AnaliticAsd.Domain.Advertising;
using AnaliticAsd.Domain.DataSources;
using Microsoft.EntityFrameworkCore;

namespace AnaliticAsd.Infrastructure.Persistence.Repositories;

public sealed class DashboardQueryRepository(AnalitiAdsDbContext db, ICurrentTenant tenant)
    : IDashboardQueryRepository
{
    private AuthorizedData Data => new(db, tenant);

    public async Task<IReadOnlyList<DataSource>> ListSourcesAsync(Guid clientId,
        CancellationToken cancellationToken = default) => await Data.DataSources.AsNoTracking()
        .Where(source => source.ClientId == clientId && source.ArchivedAtUtc == null)
        .OrderBy(source => source.Provider).ThenBy(source => source.Name)
        .ToArrayAsync(cancellationToken);

    public Task<DataSource?> GetSourceAsync(Guid clientId, Guid sourceId,
        CancellationToken cancellationToken = default) => Data.DataSources.AsNoTracking()
        .SingleOrDefaultAsync(source => source.Id == sourceId && source.ClientId == clientId,
            cancellationToken);

    public async Task<IReadOnlyList<InsightSnapshot>> ListSnapshotsAsync(Guid sourceId, InsightLevel level,
        DateOnly since, DateOnly until, CancellationToken cancellationToken = default)
    {
        var sources = Data.DataSources;
        return await db.InsightSnapshots.AsNoTracking()
            .Where(snapshot => snapshot.AdAccountId == sourceId && sources.Any(source => source.Id == snapshot.AdAccountId)
                && snapshot.Level == level && snapshot.SnapshotDate >= since && snapshot.SnapshotDate <= until)
            .OrderBy(snapshot => snapshot.SnapshotDate).ToArrayAsync(cancellationToken);
    }

    public async Task<IReadOnlyDictionary<Guid, string>> CampaignNamesAsync(Guid sourceId,
        CancellationToken cancellationToken = default)
    {
        var sources = Data.DataSources;
        return await db.Campaigns.AsNoTracking()
            .Where(campaign => campaign.AdAccountId == sourceId && sources.Any(source => source.Id == campaign.AdAccountId))
            .ToDictionaryAsync(campaign => campaign.Id, campaign => campaign.Name, cancellationToken);
    }

    public async Task<IReadOnlyList<ProviderMetricSnapshot>> ListProviderSnapshotsAsync(Guid sourceId,
        DateOnly since, DateOnly until, CancellationToken cancellationToken = default)
    {
        var sources = Data.DataSources;
        return await db.ProviderMetricSnapshots.AsNoTracking().Where(x => x.DataSourceId == sourceId
            && sources.Any(source => source.Id == x.DataSourceId) && x.Date >= since && x.Date <= until)
            .OrderBy(x => x.Date).ThenBy(x => x.DimensionName).ToArrayAsync(cancellationToken);
    }
}
