namespace AnaliticAsd.Domain.DataSources;

public sealed class ProviderConnection
{
    public const int MaxDisplayNameLength = 200;
    public const int MaxExternalSubjectIdLength = 300;
    public const int MaxProtectedCredentialLength = 8192;
    public const int MaxErrorCodeLength = 100;

    private ProviderConnection() { }

    public Guid Id { get; private set; }
    public Guid AgencyId { get; private set; }
    public DataProvider Provider { get; private set; }
    public string DisplayName { get; private set; } = string.Empty;
    public string? ExternalSubjectId { get; private set; }
    public string ProtectedCredentialPayload { get; private set; } = string.Empty;
    public DateTimeOffset? ExpiresAtUtc { get; private set; }
    public ProviderConnectionStatus Status { get; private set; }
    public DateTimeOffset AuthorizedAtUtc { get; private set; }
    public DateTimeOffset? LastSucceededAtUtc { get; private set; }
    public string? LastErrorCode { get; private set; }
    public DateTimeOffset CreatedAtUtc { get; private set; }
    public DateTimeOffset UpdatedAtUtc { get; private set; }
    public Guid Version { get; private set; }

    public static ProviderConnection Create(Guid agencyId, DataProvider provider, string displayName,
        string protectedCredentialPayload, string? externalSubjectId, DateTimeOffset? expiresAtUtc,
        DateTimeOffset now)
    {
        if (agencyId == Guid.Empty) throw new ArgumentException("Agency id is required.", nameof(agencyId));
        var utcNow = now.ToUniversalTime();
        return new ProviderConnection
        {
            Id = Guid.NewGuid(),
            AgencyId = agencyId,
            Provider = provider,
            DisplayName = NormalizeRequired(displayName, MaxDisplayNameLength, nameof(displayName)),
            ExternalSubjectId = NormalizeOptional(externalSubjectId, MaxExternalSubjectIdLength, nameof(externalSubjectId)),
            ProtectedCredentialPayload = NormalizeRequired(protectedCredentialPayload, MaxProtectedCredentialLength, nameof(protectedCredentialPayload)),
            ExpiresAtUtc = expiresAtUtc?.ToUniversalTime(),
            Status = ProviderConnectionStatus.Connected,
            AuthorizedAtUtc = utcNow,
            CreatedAtUtc = utcNow,
            UpdatedAtUtc = utcNow,
            Version = Guid.NewGuid()
        };
    }

    public void UpdateAuthorization(string displayName, string protectedCredentialPayload,
        string? externalSubjectId, DateTimeOffset? expiresAtUtc, DateTimeOffset now)
    {
        DisplayName = NormalizeRequired(displayName, MaxDisplayNameLength, nameof(displayName));
        ProtectedCredentialPayload = NormalizeRequired(protectedCredentialPayload, MaxProtectedCredentialLength,
            nameof(protectedCredentialPayload));
        ExternalSubjectId = NormalizeOptional(externalSubjectId, MaxExternalSubjectIdLength,
            nameof(externalSubjectId));
        ExpiresAtUtc = expiresAtUtc?.ToUniversalTime();
        Status = ProviderConnectionStatus.Connected;
        AuthorizedAtUtc = now.ToUniversalTime();
        LastErrorCode = null;
        Touch(now);
    }

    public void RecordSuccess(DateTimeOffset now)
    {
        Status = ProviderConnectionStatus.Connected;
        LastSucceededAtUtc = now.ToUniversalTime();
        LastErrorCode = null;
        Touch(now);
    }

    public void RecordError(string errorCode, DateTimeOffset now)
    {
        Status = ProviderConnectionStatus.Error;
        LastErrorCode = NormalizeRequired(errorCode, MaxErrorCodeLength, nameof(errorCode));
        Touch(now);
    }

    public void Revoke(DateTimeOffset now)
    {
        Status = ProviderConnectionStatus.Revoked;
        ProtectedCredentialPayload = string.Empty;
        Touch(now);
    }

    private void Touch(DateTimeOffset now)
    {
        UpdatedAtUtc = now.ToUniversalTime();
        Version = Guid.NewGuid();
    }

    private static string NormalizeRequired(string value, int maxLength, string parameter)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(value, parameter);
        var normalized = value.Trim();
        if (normalized.Length > maxLength) throw new ArgumentException($"{parameter} cannot exceed {maxLength} characters.", parameter);
        return normalized;
    }

    private static string? NormalizeOptional(string? value, int maxLength, string parameter)
    {
        if (string.IsNullOrWhiteSpace(value)) return null;
        var normalized = value.Trim();
        if (normalized.Length > maxLength) throw new ArgumentException($"{parameter} cannot exceed {maxLength} characters.", parameter);
        return normalized;
    }
}
