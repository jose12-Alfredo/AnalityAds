using AnaliticAsd.Domain.DataSources;

namespace AnaliticAsd.Application.ProviderIntegrations;

public sealed record ProviderOAuthState(Guid AgencyId, Guid UserId, DataProvider Provider);
public sealed record ProviderCredential(string AccessToken, string RefreshToken, DateTimeOffset ExpiresAtUtc);
public sealed record RemoteProviderSource(string ExternalId, string Name, string? Currency, string TimeZone,
    DataSourceType SourceType, bool IsManager = false, bool IsTest = false);
public sealed record ProviderConnectionModel(DataProvider Provider, ProviderConnectionStatus Status,
    DateTimeOffset? ExpiresAtUtc, DateTimeOffset? LastSucceededAtUtc, string? LastErrorCode);
public sealed record ProviderSourceModel(string ExternalId, string Name, string? Currency, string TimeZone,
    DataSourceType SourceType, bool IsAssigned, Guid? ClientId, bool IsManager, bool IsTest, Guid? DataSourceId);
public sealed record ProviderDailyMetric(DateOnly Date, string DimensionKey, string DimensionName,
    decimal? Spend, long? Impressions, long? Clicks, decimal? Conversions, decimal? ConversionValue,
    long? ActiveUsers = null, long? Sessions = null, long? Views = null);
public sealed record ProviderSyncModel(Guid SourceId, DataProvider Provider, DateOnly Since, DateOnly Until,
    int RowsReceived, DateTimeOffset SynchronizedAtUtc);

public interface IProviderOAuthStateProtector
{
    string Protect(ProviderOAuthState state);
    ProviderOAuthState Unprotect(string state);
}

public interface IProviderCredentialProtector
{
    string Protect(ProviderCredential credential);
    ProviderCredential Unprotect(string payload);
}

public interface IGoogleProviderClient
{
    Task<ProviderCredential> ExchangeCodeAsync(string code, DataProvider provider,
        CancellationToken cancellationToken = default);
    Task<ProviderCredential> RefreshAsync(ProviderCredential credential,
        CancellationToken cancellationToken = default);
    Task<IReadOnlyList<RemoteProviderSource>> ListGoogleAdsCustomersAsync(string accessToken,
        CancellationToken cancellationToken = default);
    Task<IReadOnlyList<RemoteProviderSource>> ListAnalyticsPropertiesAsync(string accessToken,
        CancellationToken cancellationToken = default);
    Task<IReadOnlyList<ProviderDailyMetric>> ReadDailyMetricsAsync(string accessToken, DataProvider provider,
        string externalId, DateOnly since, DateOnly until, CancellationToken cancellationToken = default);
}

public interface ITikTokProviderClient
{
    Task<ProviderCredential> ExchangeCodeAsync(string code, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<RemoteProviderSource>> ListAdvertisersAsync(string accessToken,
        CancellationToken cancellationToken = default);
    Task<IReadOnlyList<ProviderDailyMetric>> ReadDailyMetricsAsync(string accessToken, string advertiserId,
        DateOnly since, DateOnly until, CancellationToken cancellationToken = default);
}

public interface IProviderSourceRepository
{
    Task<DataSource?> FindAsync(Guid agencyId, DataProvider provider, string externalId, bool trackChanges,
        CancellationToken cancellationToken = default);
    void Add(DataSource source);
    Task<DataSource?> GetByIdAsync(Guid agencyId, Guid sourceId, bool trackChanges,
        CancellationToken cancellationToken = default);
}

public interface IProviderMetricRepository
{
    Task UpsertAsync(Guid agencyId, Guid sourceId, IReadOnlyList<ProviderDailyMetric> values,
        DateTimeOffset observedAtUtc, CancellationToken cancellationToken = default);
}

public interface IProviderSyncService
{
    Task<ProviderSyncModel> SyncAsync(Guid sourceId, DateOnly since, DateOnly until,
        CancellationToken cancellationToken = default);
    Task<ProviderSyncModel> SyncForAgencyAsync(Guid agencyId, Guid sourceId, DateOnly since, DateOnly until,
        CancellationToken cancellationToken = default);
}

public interface IGoogleIntegrationService
{
    string CreateAuthorizationUrl(DataProvider provider);
    Task<string> CompleteAsync(DataProvider provider, string code, string state,
        CancellationToken cancellationToken = default);
    string CreateFailureRedirect(DataProvider provider, string error);
    Task<ProviderConnectionModel> StatusAsync(DataProvider provider,
        CancellationToken cancellationToken = default);
    Task<IReadOnlyList<ProviderSourceModel>> DiscoverAsync(DataProvider provider,
        CancellationToken cancellationToken = default);
    Task<ProviderSourceModel> AssociateAsync(DataProvider provider, Guid clientId, string externalId,
        CancellationToken cancellationToken = default);
}

public interface ITikTokIntegrationService
{
    string CreateAuthorizationUrl();
    Task<string> CompleteAsync(string code, string state, CancellationToken cancellationToken = default);
    string CreateFailureRedirect(string error);
    Task<ProviderConnectionModel> StatusAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<ProviderSourceModel>> DiscoverAsync(CancellationToken cancellationToken = default);
    Task<ProviderSourceModel> AssociateAsync(Guid clientId, string externalId,
        CancellationToken cancellationToken = default);
}
