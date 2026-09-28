using AnaliticAsd.Domain.Meta;

namespace AnaliticAsd.Application.Meta;

public sealed record MetaOAuthState(Guid AgencyId, Guid UserId);
public sealed record MetaToken(string AccessToken, DateTimeOffset? ExpiresAtUtc);
public sealed record MetaAccountModel(string MetaAccountId, string Name, string Currency, string TimeZone, int AccountStatus, bool IsLinked, Guid? ClientId);
public sealed record MetaConnectionStatusModel(bool IsConnected, DateTimeOffset? ExpiresAtUtc);

public interface IMetaOAuthStateProtector
{
    string Protect(MetaOAuthState state);
    MetaOAuthState Unprotect(string state);
}

public interface IMetaTokenProtector
{
    string Protect(string token);
    string Unprotect(string protectedToken);
}

public interface IMetaGraphClient
{
    Task<MetaToken> ExchangeCodeAsync(string code, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<MetaRemoteAccount>> ListAdAccountsAsync(string accessToken, CancellationToken cancellationToken = default);
}

public sealed record MetaRemoteAccount(string MetaAccountId, string Name, string Currency, string TimeZone, int AccountStatus);

public interface IMetaConnectionRepository
{
    Task<MetaConnection?> GetAsync(Guid agencyId, bool trackChanges, CancellationToken cancellationToken = default);
    void Add(MetaConnection connection);
}

public interface IMetaOAuthService
{
    string CreateAuthorizationUrl();
    Task<string> CompleteAsync(string code, string state, CancellationToken cancellationToken = default);
    string CreateFailureRedirect(string error);
    Task<MetaConnectionStatusModel> GetStatusAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<MetaAccountModel>> ListAccountsAsync(CancellationToken cancellationToken = default);
    Task<MetaAccountModel> AssociateAsync(Guid clientId, string metaAccountId, CancellationToken cancellationToken = default);
}
