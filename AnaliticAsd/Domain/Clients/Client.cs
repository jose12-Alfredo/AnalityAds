namespace AnaliticAsd.Domain.Clients;

public sealed class Client
{
    public const int MaxNameLength = 200;

    private Client()
    {
    }

    private Client(Guid id, Guid agencyId, string name, DateTimeOffset createdAtUtc)
    {
        Id = id;
        AgencyId = agencyId != Guid.Empty ? agencyId : throw new ArgumentException("Agency id is required.", nameof(agencyId));
        Name = NormalizeName(name);
        IsActive = true;
        CreatedAtUtc = createdAtUtc.ToUniversalTime();
        UpdatedAtUtc = CreatedAtUtc;
    }

    public Guid Id { get; private set; }
    public Guid AgencyId { get; private set; }
    public string Name { get; private set; } = string.Empty;
    public bool IsActive { get; private set; }
    public DateTimeOffset CreatedAtUtc { get; private set; }
    public DateTimeOffset UpdatedAtUtc { get; private set; }

    public static Client Create(Guid agencyId, string name, DateTimeOffset createdAtUtc) =>
        new(Guid.NewGuid(), agencyId, name, createdAtUtc);

    public void Update(string name, bool isActive, DateTimeOffset updatedAtUtc)
    {
        Name = NormalizeName(name);
        IsActive = isActive;
        UpdatedAtUtc = updatedAtUtc.ToUniversalTime();
    }

    private static string NormalizeName(string name)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        var normalized = name.Trim();
        if (normalized.Length > MaxNameLength)
        {
            throw new ArgumentException(
                $"Client name cannot exceed {MaxNameLength} characters.",
                nameof(name));
        }

        return normalized;
    }
}
