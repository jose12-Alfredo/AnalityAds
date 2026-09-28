namespace AnaliticAsd.Domain.Identity;

public enum AgencyRole { Owner, Admin, Analyst, Viewer, ClientViewer }

public sealed class Membership
{
    private Membership() { }
    private Membership(Guid agencyId, Guid userId, AgencyRole role, DateTimeOffset now)
    {
        AgencyId = agencyId;
        UserId = userId;
        Role = role;
        CreatedAtUtc = now.ToUniversalTime();
    }

    public Guid AgencyId { get; private set; }
    public Guid UserId { get; private set; }
    public AgencyRole Role { get; private set; }
    public DateTimeOffset CreatedAtUtc { get; private set; }
    public static Membership Create(Guid agencyId, Guid userId, AgencyRole role, DateTimeOffset now) => new(agencyId, userId, role, now);
}
