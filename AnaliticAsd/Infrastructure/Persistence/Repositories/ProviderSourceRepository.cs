using AnaliticAsd.Application.ProviderIntegrations;
using AnaliticAsd.Domain.DataSources;
using Microsoft.EntityFrameworkCore;

namespace AnaliticAsd.Infrastructure.Persistence.Repositories;

public sealed class ProviderSourceRepository(AnalitiAdsDbContext db) : IProviderSourceRepository
{
    public Task<DataSource?> FindAsync(Guid agencyId, DataProvider provider, string externalId, bool trackChanges,
        CancellationToken cancellationToken = default)
    {
        IQueryable<DataSource> query = db.DataSources;
        if (!trackChanges) query = query.AsNoTracking();
        return query.SingleOrDefaultAsync(source => source.AgencyId == agencyId && source.Provider == provider
            && source.ExternalId == externalId, cancellationToken);
    }

    public void Add(DataSource source) => db.DataSources.Add(source);

    public Task<DataSource?> GetByIdAsync(Guid agencyId, Guid sourceId, bool trackChanges,
        CancellationToken cancellationToken = default)
    {
        var query = db.DataSources.Where(x => x.AgencyId == agencyId && x.Id == sourceId);
        return (trackChanges ? query : query.AsNoTracking()).SingleOrDefaultAsync(cancellationToken);
    }
}
