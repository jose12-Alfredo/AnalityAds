using AnaliticAsd.Application.AdAccounts;
using AnaliticAsd.Domain.Advertising;
using AnaliticAsd.Application.Identity;
using Microsoft.EntityFrameworkCore;

namespace AnaliticAsd.Infrastructure.Persistence.Repositories;

public sealed class AdAccountRepository(AnalitiAdsDbContext dbContext, ICurrentTenant currentTenant) : IAdAccountRepository
{
    private IQueryable<AdAccount> Scoped => new AuthorizedData(dbContext, currentTenant).Accounts;
    public async Task<IReadOnlyList<AdAccount>> ListByClientAsync(
        Guid clientId,
        CancellationToken cancellationToken = default) =>
        await Scoped
            .Include(account => account.DataSource)
            .AsNoTracking()
            .Where(account => account.ClientId == clientId && dbContext.Clients.Any(client => client.Id == account.ClientId && client.AgencyId == currentTenant.AgencyId))
            .OrderBy(account => account.Name)
            .ToArrayAsync(cancellationToken);

    public Task<AdAccount?> GetByIdAsync(
        Guid id,
        bool trackChanges,
        CancellationToken cancellationToken = default)
    {
        IQueryable<AdAccount> query = Scoped;
        query = query.Include(account => account.DataSource);
        if (!trackChanges)
        {
            query = query.AsNoTracking();
        }

        return query.SingleOrDefaultAsync(account => account.Id == id && dbContext.Clients.Any(client => client.Id == account.ClientId && client.AgencyId == currentTenant.AgencyId), cancellationToken);
    }

    public Task<bool> MetaAccountIdExistsAsync(
        MetaAdAccountId metaAccountId,
        CancellationToken cancellationToken = default) =>
        Scoped.AnyAsync(
            account => account.MetaAccountId == metaAccountId && dbContext.Clients.Any(client => client.Id == account.ClientId && client.AgencyId == currentTenant.AgencyId),
            cancellationToken);

    public Task<AdAccount?> GetByMetaAccountIdAsync(MetaAdAccountId metaAccountId, CancellationToken cancellationToken = default) =>
        Scoped.AsNoTracking().SingleOrDefaultAsync(
            account => account.MetaAccountId == metaAccountId && dbContext.Clients.Any(client => client.Id == account.ClientId && client.AgencyId == currentTenant.AgencyId), cancellationToken);

    public Task<bool> AnyForClientAsync(
        Guid clientId,
        CancellationToken cancellationToken = default) =>
        Scoped.AnyAsync(account => account.ClientId == clientId, cancellationToken);

    public void Add(AdAccount adAccount) => dbContext.AdAccounts.Add(adAccount);
    public void Remove(AdAccount adAccount)
    {
        dbContext.AdAccounts.Remove(adAccount);
        dbContext.DataSources.Remove(adAccount.DataSource);
    }
}
