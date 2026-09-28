using AnaliticAsd.Application.Meta;
using AnaliticAsd.Domain.Meta;
using Microsoft.EntityFrameworkCore;

namespace AnaliticAsd.Infrastructure.Persistence.Repositories;

public sealed class MetaConnectionRepository(AnalitiAdsDbContext dbContext) : IMetaConnectionRepository
{
    public Task<MetaConnection?> GetAsync(Guid agencyId, bool trackChanges, CancellationToken cancellationToken = default)
    {
        IQueryable<MetaConnection> query = dbContext.MetaConnections;
        if (!trackChanges) query = query.AsNoTracking();
        return query.SingleOrDefaultAsync(x => x.AgencyId == agencyId, cancellationToken);
    }
    public void Add(MetaConnection connection) => dbContext.MetaConnections.Add(connection);
}
