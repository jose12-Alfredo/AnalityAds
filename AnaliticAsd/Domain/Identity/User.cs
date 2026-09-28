namespace AnaliticAsd.Domain.Identity;

public sealed class User
{
    public const int MaxEmailLength = 320;
    private User() { }
    private User(Guid id, string email, DateTimeOffset now)
    {
        Id = id;
        SetEmail(email);
        IsActive = true;
        CreatedAtUtc = now.ToUniversalTime();
        UpdatedAtUtc = CreatedAtUtc;
    }

    public Guid Id { get; private set; }
    public string Email { get; private set; } = string.Empty;
    public string NormalizedEmail { get; private set; } = string.Empty;
    public string PasswordHash { get; private set; } = string.Empty;
    public bool IsActive { get; private set; }
    public DateTimeOffset CreatedAtUtc { get; private set; }
    public DateTimeOffset UpdatedAtUtc { get; private set; }
    public static User Create(string email, DateTimeOffset now) => new(Guid.NewGuid(), email, now);
    public void SetPasswordHash(string hash) => PasswordHash = !string.IsNullOrWhiteSpace(hash) ? hash : throw new ArgumentException("Password hash is required.", nameof(hash));

    public static string NormalizeEmail(string email)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(email);
        var result = email.Trim().ToUpperInvariant();
        if (result.Length > MaxEmailLength || !result.Contains('@')) throw new ArgumentException("A valid email is required.", nameof(email));
        return result;
    }

    private void SetEmail(string email)
    {
        NormalizedEmail = NormalizeEmail(email);
        Email = email.Trim();
    }
}
