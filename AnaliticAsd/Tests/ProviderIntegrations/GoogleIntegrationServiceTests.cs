using AnaliticAsd.Application.Abstractions;
using AnaliticAsd.Application.Clients;
using AnaliticAsd.Application.DataSources;
using AnaliticAsd.Application.Identity;
using AnaliticAsd.Application.ProviderIntegrations;
using AnaliticAsd.Domain.Clients;
using AnaliticAsd.Domain.DataSources;
using Microsoft.Extensions.Configuration;

namespace AnaliticAsd.Tests.ProviderIntegrations;

public sealed class GoogleIntegrationServiceTests
{
    [Fact]
    public void Authorization_urls_use_offline_access_and_provider_specific_scopes()
    {
        var fixture = new Fixture();

        var ads = fixture.Service.CreateAuthorizationUrl(DataProvider.GoogleAds);
        var analytics = fixture.Service.CreateAuthorizationUrl(DataProvider.GoogleAnalytics4);

        Assert.Contains("access_type=offline", ads);
        Assert.Contains(Uri.EscapeDataString("https://www.googleapis.com/auth/adwords"), ads);
        Assert.Contains(Uri.EscapeDataString("https://www.googleapis.com/auth/analytics.readonly"), analytics);
        Assert.DoesNotContain("analytics.edit", analytics);
    }

    [Theory]
    [InlineData(DataProvider.GoogleAds, DataSourceType.AdvertisingAccount, "customers/123")]
    [InlineData(DataProvider.GoogleAnalytics4, DataSourceType.AnalyticsProperty, "properties/456")]
    public async Task Callback_discovery_and_assignment_preserve_provider_ownership(DataProvider provider,
        DataSourceType sourceType, string externalId)
    {
        var fixture = new Fixture();
        fixture.Google.Source = new(externalId, "Source", provider == DataProvider.GoogleAds ? "USD" : null,
            "America/La_Paz", sourceType);

        fixture.Service.CreateAuthorizationUrl(provider);
        var redirect = await fixture.Service.CompleteAsync(provider, "code", "state");
        var discovered = await fixture.Service.DiscoverAsync(provider);
        var assigned = await fixture.Service.AssociateAsync(provider, fixture.Store.Client.Id, externalId);

        Assert.EndsWith("result=success", redirect);
        Assert.StartsWith("protected:", fixture.Store.Connection!.ProtectedCredentialPayload,
            StringComparison.Ordinal);
        Assert.Single(discovered);
        Assert.True(assigned.IsAssigned);
        Assert.Equal(fixture.Store.Client.Id, assigned.ClientId);
        Assert.Equal(provider, fixture.Store.Source!.Provider);
        Assert.Equal(fixture.Store.Connection.Id, fixture.Store.Source.ProviderConnectionId);
    }

    [Fact]
    public async Task Google_ads_manager_account_cannot_be_assigned_as_a_source()
    {
        var fixture = new Fixture();
        fixture.Google.Source = new("123", "Manager", "USD", "UTC", DataSourceType.AdvertisingAccount, true);
        fixture.Service.CreateAuthorizationUrl(DataProvider.GoogleAds);
        await fixture.Service.CompleteAsync(DataProvider.GoogleAds, "code", "state");

        await Assert.ThrowsAsync<AnaliticAsd.Application.Common.ConflictException>(() =>
            fixture.Service.AssociateAsync(DataProvider.GoogleAds, fixture.Store.Client.Id, "123"));
    }

    private sealed class Fixture
    {
        private readonly FixedTenant tenant = new(Guid.NewGuid(), Guid.NewGuid());
        public Store Store { get; }
        public GoogleClient Google { get; } = new();
        public GoogleIntegrationService Service { get; }

        public Fixture()
        {
            Store = new(Client.Create(tenant.AgencyId, "Client", DateTimeOffset.UtcNow));
            var configuration = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Google:ClientId"] = "client-id",
                ["GoogleAds:RedirectUri"] = "https://api.example.com/google-ads",
                ["GoogleAds:FrontendCallbackUrl"] = "https://app.example.com/google-ads",
                ["GoogleAnalytics4:RedirectUri"] = "https://api.example.com/ga4",
                ["GoogleAnalytics4:FrontendCallbackUrl"] = "https://app.example.com/ga4"
            }).Build();
            Service = new(configuration, tenant, new StateProtector(tenant), new CredentialProtector(), Google,
                Store, Store, Store, Store, TimeProvider.System);
        }
    }

    private sealed record FixedTenant(Guid AgencyId, Guid UserId) : ICurrentTenant { public string Role => "Owner"; }
    private sealed class StateProtector(FixedTenant tenant) : IProviderOAuthStateProtector
    {
        private DataProvider currentProvider;
        public string Protect(ProviderOAuthState state) { currentProvider = state.Provider; return "state"; }
        public ProviderOAuthState Unprotect(string state) => new(tenant.AgencyId, tenant.UserId,
            currentProvider);
    }
    private sealed class CredentialProtector : IProviderCredentialProtector
    {
        public string Protect(ProviderCredential credential) => $"protected:{credential.AccessToken}|{credential.RefreshToken}";
        public ProviderCredential Unprotect(string payload)
        {
            var values = payload[10..].Split('|');
            return new(values[0], values[1], DateTimeOffset.UtcNow.AddHours(1));
        }
    }
    private sealed class GoogleClient : IGoogleProviderClient
    {
        public RemoteProviderSource Source { get; set; } = new("123", "Google", "USD", "UTC",
            DataSourceType.AdvertisingAccount);
        public Task<ProviderCredential> ExchangeCodeAsync(string code, DataProvider provider,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult(new ProviderCredential("access", "refresh", DateTimeOffset.UtcNow.AddHours(1)));
        }
        public Task<ProviderCredential> RefreshAsync(ProviderCredential credential,
            CancellationToken cancellationToken = default) => Task.FromResult(credential);
        public Task<IReadOnlyList<RemoteProviderSource>> ListGoogleAdsCustomersAsync(string accessToken,
            CancellationToken cancellationToken = default) => Task.FromResult<IReadOnlyList<RemoteProviderSource>>([Source]);
        public Task<IReadOnlyList<RemoteProviderSource>> ListAnalyticsPropertiesAsync(string accessToken,
            CancellationToken cancellationToken = default) => Task.FromResult<IReadOnlyList<RemoteProviderSource>>([Source]);
        public Task<IReadOnlyList<ProviderDailyMetric>> ReadDailyMetricsAsync(string accessToken,
            DataProvider provider, string externalId, DateOnly since, DateOnly until,
            CancellationToken cancellationToken = default) => Task.FromResult<IReadOnlyList<ProviderDailyMetric>>([]);
    }
    private sealed class Store(Client client) : IProviderConnectionRepository, IProviderSourceRepository,
        IClientRepository, IUnitOfWork
    {
        public Client Client { get; } = client;
        public ProviderConnection? Connection { get; private set; }
        public DataSource? Source { get; private set; }
        public Task<ProviderConnection?> GetLatestAsync(Guid agencyId, DataProvider provider, bool trackChanges,
            CancellationToken cancellationToken = default) => Task.FromResult(Connection?.AgencyId == agencyId
            && Connection.Provider == provider ? Connection : null);
        public void Add(ProviderConnection connection) => Connection = connection;
        public Task<DataSource?> FindAsync(Guid agencyId, DataProvider provider, string externalId, bool trackChanges,
            CancellationToken cancellationToken = default) => Task.FromResult(Source?.AgencyId == agencyId
            && Source.Provider == provider && Source.ExternalId == externalId ? Source : null);
        public void Add(DataSource source) => Source = source;
        public Task<DataSource?> GetByIdAsync(Guid agencyId, Guid sourceId, bool trackChanges,
            CancellationToken cancellationToken = default) => Task.FromResult(Source?.AgencyId == agencyId && Source.Id == sourceId ? Source : null);
        public Task<IReadOnlyList<Client>> ListAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<Client>>([Client]);
        Task<Client?> IClientRepository.GetByIdAsync(Guid id, bool trackChanges,
            CancellationToken cancellationToken) => Task.FromResult(id == Client.Id ? Client : null);
        public void Add(Client value) { }
        public void Remove(Client value) { }
        public Task SaveChangesAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;
    }
}
