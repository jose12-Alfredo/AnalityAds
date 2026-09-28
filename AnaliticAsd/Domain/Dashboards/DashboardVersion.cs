using System.Security.Cryptography;
using System.Text;

namespace AnaliticAsd.Domain.Dashboards;

public sealed class DashboardVersion
{
    private DashboardVersion() { }

    public Guid Id { get; private set; }
    public Guid DashboardId { get; private set; }
    public Guid AgencyId { get; private set; }
    public Guid ClientId { get; private set; }
    public int PublicationNumber { get; private set; }
    public int DraftRevision { get; private set; }
    public int SchemaVersion { get; private set; }
    public string DefinitionJson { get; private set; } = string.Empty;
    public string DefinitionHash { get; private set; } = string.Empty;
    public Guid PublishedByUserId { get; private set; }
    public DateTimeOffset PublishedAtUtc { get; private set; }

    public static DashboardVersion Create(Guid dashboardId, Guid agencyId, Guid clientId,
        int publicationNumber, int draftRevision, int schemaVersion, string definitionJson,
        Guid publishedByUserId, DateTimeOffset now)
    {
        if (dashboardId == Guid.Empty || agencyId == Guid.Empty || clientId == Guid.Empty
            || publishedByUserId == Guid.Empty)
            throw new ArgumentException("Dashboard, agency, client and publisher are required.");
        if (publicationNumber <= 0 || draftRevision <= 0 || schemaVersion <= 0)
            throw new ArgumentOutOfRangeException(nameof(publicationNumber));
        ArgumentException.ThrowIfNullOrWhiteSpace(definitionJson);
        return new DashboardVersion
        {
            Id = Guid.NewGuid(),
            DashboardId = dashboardId,
            AgencyId = agencyId,
            ClientId = clientId,
            PublicationNumber = publicationNumber,
            DraftRevision = draftRevision,
            SchemaVersion = schemaVersion,
            DefinitionJson = definitionJson,
            DefinitionHash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(definitionJson))),
            PublishedByUserId = publishedByUserId,
            PublishedAtUtc = now.ToUniversalTime()
        };
    }
}
