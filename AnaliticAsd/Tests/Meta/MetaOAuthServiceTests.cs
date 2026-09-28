using AnaliticAsd.Application.Abstractions;
using AnaliticAsd.Application.AdAccounts;
using AnaliticAsd.Application.Clients;
using AnaliticAsd.Application.DataSources;
using AnaliticAsd.Application.Identity;
using AnaliticAsd.Application.Meta;
using AnaliticAsd.Domain.Advertising;
using AnaliticAsd.Domain.Clients;
using AnaliticAsd.Domain.DataSources;
using AnaliticAsd.Domain.Meta;
using Microsoft.Extensions.Configuration;

namespace AnaliticAsd.Tests.Meta;

public sealed class MetaOAuthServiceTests
{
    [Fact]
    public async Task Callback_stores_protected_token_and_available_account_can_be_associated()
    {
        var tenant = new FixedTenant(Guid.NewGuid(), Guid.NewGuid());
        var store = new Store(Client.Create(tenant.AgencyId, "Client", DateTimeOffset.UtcNow));
        var configuration = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["Meta:AppId"] = "app-id",
            ["Meta:RedirectUri"] = "https://api.example.com/callback",
            ["Meta:FrontendCallbackUrl"] = "http://localhost:3000/meta"
        }).Build();
        var service = new MetaOAuthService(configuration, tenant, new StateProtector(tenant), new TokenProtector(),
            new GraphClient(), store, store, store, store, store, TimeProvider.System);

        var redirect = await service.CompleteAsync("code", "state");
        var available = await service.ListAccountsAsync();
        var associated = await service.AssociateAsync(store.Client.Id, available[0].MetaAccountId);

        Assert.Equal("http://localhost:3000/meta?meta=success", redirect);
        Assert.StartsWith("protected:", store.Connection!.ProtectedAccessToken, StringComparison.Ordinal);
        Assert.Equal(DataProvider.MetaAds, store.ProviderConnection!.Provider);
        Assert.Equal(store.ProviderConnection.Id, store.Account!.DataSource.ProviderConnectionId);
        Assert.True(associated.IsLinked);
        Assert.Equal(AdAccountConnectionStatus.Connected, store.Account!.ConnectionStatus);
        Assert.Equal(store.Client.Id, associated.ClientId);
    }

    private sealed record FixedTenant(Guid AgencyId, Guid UserId) : ICurrentTenant { public string Role => "Owner"; }
    private sealed class StateProtector(FixedTenant tenant) : IMetaOAuthStateProtector
    {
        public string Protect(MetaOAuthState state) => "state";
        public MetaOAuthState Unprotect(string state) => new(tenant.AgencyId, tenant.UserId);
    }
    private sealed class TokenProtector : IMetaTokenProtector
    {
        public string Protect(string token) => $"protected:{token}";
        public string Unprotect(string protectedToken) => protectedToken[10..];
    }
    private sealed class GraphClient : IMetaGraphClient
    {
        public Task<MetaToken> ExchangeCodeAsync(string code, CancellationToken cancellationToken = default) => Task.FromResult(new MetaToken("meta-token", DateTimeOffset.UtcNow.AddDays(60)));
        public Task<IReadOnlyList<MetaRemoteAccount>> ListAdAccountsAsync(string accessToken, CancellationToken cancellationToken = default) => Task.FromResult<IReadOnlyList<MetaRemoteAccount>>([new("act_123456", "Meta Account", "USD", "America/La_Paz", 1)]);
    }
    private sealed class Store(Client client) : IMetaConnectionRepository, IProviderConnectionRepository,
        IClientRepository, IAdAccountRepository, IUnitOfWork
    {
        public Client Client { get; } = client;
        public MetaConnection? Connection { get; private set; }
        public ProviderConnection? ProviderConnection { get; private set; }
        public AdAccount? Account { get; private set; }
        public Task<MetaConnection?> GetAsync(Guid agencyId, bool trackChanges, CancellationToken cancellationToken = default) => Task.FromResult(Connection?.AgencyId == agencyId ? Connection : null);
        public void Add(MetaConnection connection) => Connection = connection;
        public Task<ProviderConnection?> GetLatestAsync(Guid agencyId, DataProvider provider, bool trackChanges,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(ProviderConnection?.AgencyId == agencyId && ProviderConnection.Provider == provider
                ? ProviderConnection
                : null);
        public void Add(ProviderConnection connection) => ProviderConnection = connection;
        public Task<IReadOnlyList<Client>> ListAsync(CancellationToken cancellationToken = default) => Task.FromResult<IReadOnlyList<Client>>([Client]);
        Task<Client?> IClientRepository.GetByIdAsync(Guid id, bool trackChanges, CancellationToken cancellationToken) => Task.FromResult(id == Client.Id ? Client : null);
        public void Add(Client value) { }
        public void Remove(Client value) { }
        public Task<IReadOnlyList<AdAccount>> ListByClientAsync(Guid clientId, CancellationToken cancellationToken = default) => Task.FromResult<IReadOnlyList<AdAccount>>(Account is null ? [] : [Account]);
        Task<AdAccount?> IAdAccountRepository.GetByIdAsync(Guid id, bool trackChanges, CancellationToken cancellationToken) => Task.FromResult(Account?.Id == id ? Account : null);
        public Task<bool> MetaAccountIdExistsAsync(MetaAdAccountId id, CancellationToken cancellationToken = default) => Task.FromResult(Account?.MetaAccountId == id);
        public Task<AdAccount?> GetByMetaAccountIdAsync(MetaAdAccountId id, CancellationToken cancellationToken = default) => Task.FromResult(Account?.MetaAccountId == id ? Account : null);
        public Task<bool> AnyForClientAsync(Guid clientId, CancellationToken cancellationToken = default) => Task.FromResult(Account?.ClientId == clientId);
        public void Add(AdAccount value) => Account = value;
        public void Remove(AdAccount value) => Account = null;
        public Task SaveChangesAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;
    }
}
