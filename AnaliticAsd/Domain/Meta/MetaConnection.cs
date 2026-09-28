namespace AnaliticAsd.Domain.Meta;

public sealed class MetaConnection
{
    private MetaConnection() { }
    private MetaConnection(Guid agencyId, string protectedAccessToken, DateTimeOffset? expiresAtUtc, DateTimeOffset now)
    {
        AgencyId = agencyId;
        Update(protectedAccessToken, expiresAtUtc, now);
        CreatedAtUtc = UpdatedAtUtc;
    }

    public Guid AgencyId { get; private set; }
    public string ProtectedAccessToken { get; private set; } = string.Empty;
    public DateTimeOffset? ExpiresAtUtc { get; private set; }
    public DateTimeOffset CreatedAtUtc { get; private set; }
    public DateTimeOffset UpdatedAtUtc { get; private set; }
    public static MetaConnection Create(Guid agencyId, string token, DateTimeOffset? expiresAtUtc, DateTimeOffset now) => new(agencyId, token, expiresAtUtc, now);
    public void Update(string token, DateTimeOffset? expiresAtUtc, DateTimeOffset now)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(token);
        ProtectedAccessToken = token;
        ExpiresAtUtc = expiresAtUtc?.ToUniversalTime();
        UpdatedAtUtc = now.ToUniversalTime();
    }
}
