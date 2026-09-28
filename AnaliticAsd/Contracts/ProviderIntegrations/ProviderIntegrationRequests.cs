namespace AnaliticAsd.Contracts.ProviderIntegrations;

public sealed record ProviderAuthorizationResponse(string AuthorizationUrl);
public sealed record ProviderConnectionResponse(string Provider, string Status, DateTimeOffset? ExpiresAtUtc,
    DateTimeOffset? LastSucceededAtUtc, string? LastErrorCode);
public sealed record ProviderSourceResponse(string ExternalId, string Name, string? Currency, string TimeZone,
    string SourceType, bool IsAssigned, Guid? ClientId, bool IsManager, bool IsTest, Guid? DataSourceId);
public sealed record AssociateProviderSourceRequest(string ExternalId);
