namespace AnaliticAsd.Domain.Reports;

public sealed class ReportShareLink
{
    private ReportShareLink() { }
    public Guid Id { get; private set; }
    public Guid ReportId { get; private set; }
    public Guid AgencyId { get; private set; }
    public Guid ClientId { get; private set; }
    public Guid CreatedByUserId { get; private set; }
    public string TokenHash { get; private set; } = string.Empty;
    public DateTimeOffset CreatedAtUtc { get; private set; }
    public DateTimeOffset ExpiresAtUtc { get; private set; }
    public DateTimeOffset? RevokedAtUtc { get; private set; }
    public Guid Version { get; private set; }

    public static ReportShareLink Create(Guid reportId, Guid agencyId, Guid clientId, Guid creatorId,
        string tokenHash, DateTimeOffset now, DateTimeOffset expiresAt)
    {
        if (reportId == Guid.Empty || agencyId == Guid.Empty || clientId == Guid.Empty || creatorId == Guid.Empty)
            throw new ArgumentException("Report, agency, client and creator are required.");
        if (tokenHash.Length != 64) throw new ArgumentException("A SHA-256 token hash is required.");
        if (expiresAt <= now) throw new ArgumentException("Share link expiration must be in the future.");
        return new()
        {
            Id = Guid.NewGuid(), ReportId = reportId, AgencyId = agencyId, ClientId = clientId,
            CreatedByUserId = creatorId, TokenHash = tokenHash, CreatedAtUtc = now.ToUniversalTime(),
            ExpiresAtUtc = expiresAt.ToUniversalTime(), Version = Guid.NewGuid()
        };
    }

    public bool IsAvailable(DateTimeOffset now) => RevokedAtUtc is null && now < ExpiresAtUtc;
    public string Status(DateTimeOffset now) => RevokedAtUtc is not null ? "Revoked" : now >= ExpiresAtUtc ? "Expired" : "Active";
    public void Revoke(DateTimeOffset now)
    {
        if (RevokedAtUtc is not null) return;
        RevokedAtUtc = now.ToUniversalTime();
        Version = Guid.NewGuid();
    }
}

public sealed class ReportShareAuditEvent
{
    private ReportShareAuditEvent() { }
    public Guid Id { get; private set; }
    public Guid ShareLinkId { get; private set; }
    public string Operation { get; private set; } = string.Empty;
    public DateTimeOffset OccurredAtUtc { get; private set; }

    public static ReportShareAuditEvent Create(Guid shareLinkId, string operation, DateTimeOffset now)
    {
        if (shareLinkId == Guid.Empty || string.IsNullOrWhiteSpace(operation)) throw new ArgumentException("Share link and operation are required.");
        return new() { Id = Guid.NewGuid(), ShareLinkId = shareLinkId, Operation = operation.Trim(), OccurredAtUtc = now.ToUniversalTime() };
    }
}
