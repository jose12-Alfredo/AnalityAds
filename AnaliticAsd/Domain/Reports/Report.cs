namespace AnaliticAsd.Domain.Reports;

public sealed class Report
{
    public const int MaxTitleLength = 200;
    public const int CurrentSchemaVersion = 1;
    private Report() { }

    public Guid Id { get; private set; }
    public Guid AgencyId { get; private set; }
    public Guid ClientId { get; private set; }
    public Guid AdAccountId { get; private set; }
    public Guid CreatedByUserId { get; private set; }
    public string Title { get; private set; } = string.Empty;
    public DateOnly Since { get; private set; }
    public DateOnly Until { get; private set; }
    public DateTimeOffset CreatedAtUtc { get; private set; }
    public int SchemaVersion { get; private set; }
    public string SnapshotJson { get; private set; } = string.Empty;
    public string SnapshotHash { get; private set; } = string.Empty;

    public static Report Create(Guid id, Guid agencyId, Guid clientId, Guid adAccountId, Guid creatorId,
        string title, DateOnly since, DateOnly until, string snapshotJson, string snapshotHash, DateTimeOffset now)
    {
        if (id == Guid.Empty || agencyId == Guid.Empty || clientId == Guid.Empty || adAccountId == Guid.Empty || creatorId == Guid.Empty)
            throw new ArgumentException("Agency, client, ad account and creator are required.");
        ArgumentException.ThrowIfNullOrWhiteSpace(title);
        var normalizedTitle = title.Trim();
        if (normalizedTitle.Length > MaxTitleLength) throw new ArgumentException($"Report title cannot exceed {MaxTitleLength} characters.");
        if (since == DateOnly.MinValue || until == DateOnly.MinValue || since > until) throw new ArgumentException("A valid report period is required.");
        if (string.IsNullOrWhiteSpace(snapshotJson) || snapshotHash.Length != 64) throw new ArgumentException("A valid immutable snapshot is required.");
        return new()
        {
            Id = id, AgencyId = agencyId, ClientId = clientId, AdAccountId = adAccountId,
            CreatedByUserId = creatorId, Title = normalizedTitle, Since = since, Until = until,
            CreatedAtUtc = now.ToUniversalTime(), SchemaVersion = CurrentSchemaVersion,
            SnapshotJson = snapshotJson, SnapshotHash = snapshotHash
        };
    }
}
