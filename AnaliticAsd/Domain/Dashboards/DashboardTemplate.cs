namespace AnaliticAsd.Domain.Dashboards;

public sealed class DashboardTemplate
{
    public const int MaxNameLength = 200;
    public const int MaxDescriptionLength = 1000;

    private DashboardTemplate() { }

    public Guid Id { get; private set; }
    public Guid AgencyId { get; private set; }
    public Guid? ClientId { get; private set; }
    public string Name { get; private set; } = string.Empty;
    public string Description { get; private set; } = string.Empty;
    public int SchemaVersion { get; private set; }
    public string DefinitionJson { get; private set; } = string.Empty;
    public Guid CreatedByUserId { get; private set; }
    public DateTimeOffset CreatedAtUtc { get; private set; }
    public Guid Version { get; private set; }

    public static DashboardTemplate Create(Guid agencyId, Guid? clientId, string name, string? description,
        int schemaVersion, string definitionJson, Guid createdByUserId, DateTimeOffset now)
    {
        if (agencyId == Guid.Empty || createdByUserId == Guid.Empty)
            throw new ArgumentException("Agency and creator are required.");
        if (clientId == Guid.Empty) throw new ArgumentException("Client id cannot be empty.", nameof(clientId));
        if (schemaVersion <= 0) throw new ArgumentOutOfRangeException(nameof(schemaVersion));
        ArgumentException.ThrowIfNullOrWhiteSpace(definitionJson);
        return new DashboardTemplate
        {
            Id = Guid.NewGuid(),
            AgencyId = agencyId,
            ClientId = clientId,
            Name = Dashboard.NormalizeRequired(name, MaxNameLength, nameof(name)),
            Description = Dashboard.NormalizeOptional(description, MaxDescriptionLength, nameof(description)),
            SchemaVersion = schemaVersion,
            DefinitionJson = definitionJson,
            CreatedByUserId = createdByUserId,
            CreatedAtUtc = now.ToUniversalTime(),
            Version = Guid.NewGuid()
        };
    }
}
