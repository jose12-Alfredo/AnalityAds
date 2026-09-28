namespace AnaliticAsd.Domain.Dashboards;

public sealed class DashboardShareLink
{
    private DashboardShareLink() { }
    public Guid Id { get; private set; }
    public Guid DashboardId { get; private set; }
    public Guid AgencyId { get; private set; }
    public Guid ClientId { get; private set; }
    public Guid CreatedByUserId { get; private set; }
    public string TokenHash { get; private set; } = string.Empty;
    public string? PasswordHash { get; private set; }
    public string? RecipientEmail { get; private set; }
    public DateTimeOffset? ExpiresAtUtc { get; private set; }
    public bool AllowFilters { get; private set; }
    public bool AllowExport { get; private set; }
    public bool AllowEmbed { get; private set; }
    public DateTimeOffset CreatedAtUtc { get; private set; }
    public DateTimeOffset? RevokedAtUtc { get; private set; }
    public DateTimeOffset? LastAccessedAtUtc { get; private set; }
    public long AccessCount { get; private set; }
    public Guid Version { get; private set; }

    public static DashboardShareLink Create(Guid dashboardId, Guid agencyId, Guid clientId, Guid userId,
        string tokenHash, string? passwordHash, string? recipientEmail, DateTimeOffset? expiresAt,
        bool allowFilters, bool allowExport, bool allowEmbed, DateTimeOffset now)
    {
        if (dashboardId == Guid.Empty || agencyId == Guid.Empty || clientId == Guid.Empty || userId == Guid.Empty) throw new ArgumentException("Dashboard, agency, client and creator are required.");
        if (tokenHash.Length != 64) throw new ArgumentException("A SHA-256 token hash is required.");
        if (expiresAt <= now) throw new ArgumentException("Expiration must be in the future.");
        return new() { Id=Guid.NewGuid(), DashboardId=dashboardId, AgencyId=agencyId, ClientId=clientId,
            CreatedByUserId=userId, TokenHash=tokenHash, PasswordHash=passwordHash,
            RecipientEmail=string.IsNullOrWhiteSpace(recipientEmail)?null:recipientEmail.Trim().ToLowerInvariant(),
            ExpiresAtUtc=expiresAt?.ToUniversalTime(), AllowFilters=allowFilters, AllowExport=allowExport,
            AllowEmbed=allowEmbed, CreatedAtUtc=now.ToUniversalTime(), Version=Guid.NewGuid() };
    }
    public bool IsAvailable(DateTimeOffset now) => RevokedAtUtc is null && (ExpiresAtUtc is null || now < ExpiresAtUtc);
    public string Status(DateTimeOffset now) => RevokedAtUtc is not null ? "Revoked" : ExpiresAtUtc <= now ? "Expired" : "Active";
    public void Revoke(DateTimeOffset now) { if (RevokedAtUtc is null) { RevokedAtUtc=now.ToUniversalTime(); Version=Guid.NewGuid(); } }
    public void SetPasswordHash(string hash) { ArgumentException.ThrowIfNullOrWhiteSpace(hash); PasswordHash=hash; Version=Guid.NewGuid(); }
    public void RecordAccess(DateTimeOffset now) { LastAccessedAtUtc=now.ToUniversalTime(); AccessCount++; Version=Guid.NewGuid(); }
}
