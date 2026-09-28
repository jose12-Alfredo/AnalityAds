namespace AnaliticAsd.Contracts.Meta;

public sealed record MetaAuthorizationResponse(string AuthorizationUrl);
public sealed record MetaConnectionStatusResponse(bool IsConnected, DateTimeOffset? ExpiresAtUtc);
public sealed record MetaAccountResponse(string MetaAccountId, string Name, string Currency, string TimeZone, int AccountStatus, bool IsLinked, Guid? ClientId);
public sealed record AssociateMetaAccountRequest(string MetaAccountId);
