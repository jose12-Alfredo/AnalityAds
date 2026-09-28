using AnaliticAsd.Application.DataSources;
using AnaliticAsd.Domain.DataSources;
using Microsoft.EntityFrameworkCore;

namespace AnaliticAsd.Infrastructure.Persistence.Repositories;

public sealed class ProviderConnectionRepository(AnalitiAdsDbContext dbContext) : IProviderConnectionRepository
{
    public Task<ProviderConnection?> GetLatestAsync(Guid agencyId, DataProvider provider, bool trackChanges,
        CancellationToken cancellationToken = default)
    {
        IQueryable<ProviderConnection> query = dbContext.ProviderConnections;
        if (!trackChanges) query = query.AsNoTracking();

        return query
            .Where(connection => connection.AgencyId == agencyId && connection.Provider == provider)
            .OrderByDescending(connection => connection.UpdatedAtUtc)
            .FirstOrDefaultAsync(cancellationToken);
    }

    public void Add(ProviderConnection connection) => dbContext.ProviderConnections.Add(connection);
}
