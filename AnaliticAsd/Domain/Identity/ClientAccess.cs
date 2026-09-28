namespace AnaliticAsd.Domain.Identity;

// A grant is not an agency-wide role. Its composite foreign keys enforce the tenant boundary.
public sealed class ClientAccess
{
    private ClientAccess() { }
    public Guid AgencyId { get; private set; }
    public Guid ClientId { get; private set; }
    public Guid UserId { get; private set; }
    public DateTimeOffset GrantedAtUtc { get; private set; }

    public static ClientAccess Create(Guid agencyId, Guid clientId, Guid userId, DateTimeOffset now)
    {
        if (agencyId == Guid.Empty || clientId == Guid.Empty || userId == Guid.Empty)
            throw new ArgumentException("Agency, client and user are required.");
        return new() { AgencyId = agencyId, ClientId = clientId, UserId = userId, GrantedAtUtc = now.ToUniversalTime() };
    }
}

public sealed class ClientInvitation
{
    private ClientInvitation() { }
    public Guid Id { get; private set; }
    public Guid AgencyId { get; private set; }
    public Guid ClientId { get; private set; }
    public Guid CreatedByUserId { get; private set; }
    public string Email { get; private set; } = string.Empty;
    public string NormalizedEmail { get; private set; } = string.Empty;
    public string TokenHash { get; private set; } = string.Empty;
    public DateTimeOffset CreatedAtUtc { get; private set; }
    public DateTimeOffset ExpiresAtUtc { get; private set; }
    public DateTimeOffset? AcceptedAtUtc { get; private set; }
    public DateTimeOffset? RevokedAtUtc { get; private set; }
    public Guid Version { get; private set; }

    public static ClientInvitation Create(Guid agencyId, Guid clientId, Guid creatorId, string email, string tokenHash, DateTimeOffset now)
    {
        if (agencyId == Guid.Empty || clientId == Guid.Empty || creatorId == Guid.Empty)
            throw new ArgumentException("Agency, client and creator are required.");
        if (tokenHash.Length != 64) throw new ArgumentException("A SHA-256 token hash is required.");
        return new()
        {
            Id = Guid.NewGuid(),
            AgencyId = agencyId,
            ClientId = clientId,
            CreatedByUserId = creatorId,
            Email = email.Trim(),
            NormalizedEmail = User.NormalizeEmail(email),
            TokenHash = tokenHash,
            CreatedAtUtc = now.ToUniversalTime(),
            ExpiresAtUtc = now.ToUniversalTime().AddHours(48),
            Version = Guid.NewGuid()
        };
    }

    public bool IsAvailable(DateTimeOffset now) => AcceptedAtUtc is null && RevokedAtUtc is null && now < ExpiresAtUtc;
    public string Status(DateTimeOffset now) => AcceptedAtUtc is not null ? "Accepted" : RevokedAtUtc is not null ? "Revoked" : now >= ExpiresAtUtc ? "Expired" : "Pending";
    public void Accept(DateTimeOffset now)
    {
        if (!IsAvailable(now)) throw new InvalidOperationException("The invitation is no longer available.");
        AcceptedAtUtc = now.ToUniversalTime();
        Version = Guid.NewGuid();
    }
    public void Revoke(DateTimeOffset now)
    {
        if (AcceptedAtUtc is not null || RevokedAtUtc is not null) return;
        RevokedAtUtc = now.ToUniversalTime();
        Version = Guid.NewGuid();
    }
}
