namespace AnaliticAsd.Domain.Identity;

public sealed class ClientEditorAssignment
{
    private ClientEditorAssignment() { }

    public Guid AgencyId { get; private set; }
    public Guid ClientId { get; private set; }
    public Guid UserId { get; private set; }
    public Guid GrantedByUserId { get; private set; }
    public DateTimeOffset GrantedAtUtc { get; private set; }
    public Guid Version { get; private set; }

    public static ClientEditorAssignment Create(Guid agencyId, Guid clientId, Guid userId, Guid grantedByUserId,
        DateTimeOffset now)
    {
        if (agencyId == Guid.Empty || clientId == Guid.Empty || userId == Guid.Empty || grantedByUserId == Guid.Empty)
            throw new ArgumentException("Agency, client, editor and grantor are required.");
        return new ClientEditorAssignment
        {
            AgencyId = agencyId,
            ClientId = clientId,
            UserId = userId,
            GrantedByUserId = grantedByUserId,
            GrantedAtUtc = now.ToUniversalTime(),
            Version = Guid.NewGuid()
        };
    }
}
