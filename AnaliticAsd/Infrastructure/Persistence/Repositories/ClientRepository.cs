using AnaliticAsd.Application.Clients;
using AnaliticAsd.Domain.Clients;
using AnaliticAsd.Application.Identity;
using Microsoft.EntityFrameworkCore;

namespace AnaliticAsd.Infrastructure.Persistence.Repositories;

public sealed class ClientRepository(AnalitiAdsDbContext dbContext, ICurrentTenant currentTenant) : IClientRepository
{
    private IQueryable<Client> Scoped => new AuthorizedData(dbContext, currentTenant).Clients;
    public async Task<IReadOnlyList<Client>> ListAsync(
        CancellationToken cancellationToken = default) =>
        await Scoped.AsNoTracking().OrderBy(client => client.Name).ToArrayAsync(cancellationToken);

    public Task<Client?> GetByIdAsync(
        Guid id,
        bool trackChanges,
        CancellationToken cancellationToken = default)
    {
        IQueryable<Client> query = Scoped;
        if (!trackChanges)
        {
            query = query.AsNoTracking();
        }

        return query.SingleOrDefaultAsync(client => client.Id == id && client.AgencyId == currentTenant.AgencyId, cancellationToken);
    }

    public void Add(Client client) => dbContext.Clients.Add(client);
    public void Remove(Client client) => dbContext.Clients.Remove(client);
}
