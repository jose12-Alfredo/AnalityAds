namespace AnaliticAsd.Domain.Identity;

public sealed class Agency
{
    public const int MaxNameLength = 200;
    private Agency() { }
    private Agency(Guid id, string name, DateTimeOffset now)
    {
        Id = id;
        Name = NormalizeName(name);
        IsActive = true;
        CreatedAtUtc = now.ToUniversalTime();
        UpdatedAtUtc = CreatedAtUtc;
    }

    public Guid Id { get; private set; }
    public string Name { get; private set; } = string.Empty;
    public bool IsActive { get; private set; }
    public DateTimeOffset CreatedAtUtc { get; private set; }
    public DateTimeOffset UpdatedAtUtc { get; private set; }
    public static Agency Create(string name, DateTimeOffset now) => new(Guid.NewGuid(), name, now);

    private static string NormalizeName(string value)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(value);
        var result = value.Trim();
        if (result.Length > MaxNameLength) throw new ArgumentException($"Agency name cannot exceed {MaxNameLength} characters.", nameof(value));
        return result;
    }
}
