namespace AnaliticAsd.Domain.Dashboards;

public sealed class DashboardDraft
{
    private DashboardDraft() { }

    public Guid DashboardId { get; private set; }
    public Guid AgencyId { get; private set; }
    public Guid ClientId { get; private set; }
    public int SchemaVersion { get; private set; }
    public int Revision { get; private set; }
    public string DefinitionJson { get; private set; } = string.Empty;
    public Guid UpdatedByUserId { get; private set; }
    public DateTimeOffset UpdatedAtUtc { get; private set; }
    public Guid Version { get; private set; }

    public static DashboardDraft Create(Guid dashboardId, Guid agencyId, Guid clientId, int schemaVersion,
        string definitionJson, Guid userId, DateTimeOffset now)
    {
        ValidateIds(dashboardId, agencyId, clientId, userId);
        if (schemaVersion <= 0) throw new ArgumentOutOfRangeException(nameof(schemaVersion));
        ArgumentException.ThrowIfNullOrWhiteSpace(definitionJson);
        return new DashboardDraft
        {
            DashboardId = dashboardId,
            AgencyId = agencyId,
            ClientId = clientId,
            SchemaVersion = schemaVersion,
            Revision = 1,
            DefinitionJson = definitionJson,
            UpdatedByUserId = userId,
            UpdatedAtUtc = now.ToUniversalTime(),
            Version = Guid.NewGuid()
        };
    }

    public void Replace(int expectedRevision, int schemaVersion, string definitionJson, Guid userId,
        DateTimeOffset now)
    {
        if (expectedRevision != Revision)
            throw new InvalidOperationException("The dashboard draft revision is stale.");
        if (schemaVersion <= 0) throw new ArgumentOutOfRangeException(nameof(schemaVersion));
        if (userId == Guid.Empty) throw new ArgumentException("User id is required.", nameof(userId));
        ArgumentException.ThrowIfNullOrWhiteSpace(definitionJson);
        SchemaVersion = schemaVersion;
        DefinitionJson = definitionJson;
        UpdatedByUserId = userId;
        UpdatedAtUtc = now.ToUniversalTime();
        Revision++;
        Version = Guid.NewGuid();
    }

    private static void ValidateIds(Guid dashboardId, Guid agencyId, Guid clientId, Guid userId)
    {
        if (dashboardId == Guid.Empty || agencyId == Guid.Empty || clientId == Guid.Empty || userId == Guid.Empty)
            throw new ArgumentException("Dashboard, agency, client and user are required.");
    }
}
