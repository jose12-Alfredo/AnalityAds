using AnaliticAsd.Application.Abstractions;
using AnaliticAsd.Application.AdAccounts;
using AnaliticAsd.Application.Clients;
using AnaliticAsd.Domain.Advertising;
using AnaliticAsd.Domain.Clients;

namespace AnaliticAsd.Tests.Api;

internal sealed class InMemoryDataStore : IClientRepository, IAdAccountRepository, IUnitOfWork
{
    private readonly Dictionary<Guid, Client> clients = [];
    private readonly Dictionary<Guid, AdAccount> accounts = [];

    public Task<IReadOnlyList<Client>> ListAsync(CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyList<Client>>(clients.Values.OrderBy(client => client.Name).ToArray());

    Task<Client?> IClientRepository.GetByIdAsync(
        Guid id,
        bool trackChanges,
        CancellationToken cancellationToken)
    {
        clients.TryGetValue(id, out var client);
        return Task.FromResult(client);
    }

    public void Add(Client client) => clients.Add(client.Id, client);
    public void Remove(Client client) => clients.Remove(client.Id);

    public Task<IReadOnlyList<AdAccount>> ListByClientAsync(
        Guid clientId,
        CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyList<AdAccount>>(accounts.Values
            .Where(account => account.ClientId == clientId)
            .OrderBy(account => account.Name)
            .ToArray());

    Task<AdAccount?> IAdAccountRepository.GetByIdAsync(
        Guid id,
        bool trackChanges,
        CancellationToken cancellationToken)
    {
        accounts.TryGetValue(id, out var account);
        return Task.FromResult(account);
    }

    public Task<bool> MetaAccountIdExistsAsync(
        MetaAdAccountId metaAccountId,
        CancellationToken cancellationToken = default) =>
        Task.FromResult(accounts.Values.Any(account => account.MetaAccountId == metaAccountId));

    public Task<AdAccount?> GetByMetaAccountIdAsync(MetaAdAccountId metaAccountId, CancellationToken cancellationToken = default) =>
        Task.FromResult(accounts.Values.SingleOrDefault(account => account.MetaAccountId == metaAccountId));

    public Task<bool> AnyForClientAsync(
        Guid clientId,
        CancellationToken cancellationToken = default) =>
        Task.FromResult(accounts.Values.Any(account => account.ClientId == clientId));

    public void Add(AdAccount account) => accounts.Add(account.Id, account);
    public void Remove(AdAccount account) => accounts.Remove(account.Id);
    public Task SaveChangesAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;
}
