namespace AnaliticAsd.Domain.Dashboards;

public sealed class Dashboard
{
    public const int MaxTitleLength = 200;
    public const int MaxDescriptionLength = 2000;

    private Dashboard() { }

    public Guid Id { get; private set; }
    public Guid AgencyId { get; private set; }
    public Guid ClientId { get; private set; }
    public Guid? FolderId { get; private set; }
    public string Title { get; private set; } = string.Empty;
    public string Description { get; private set; } = string.Empty;
    public Guid CreatedByUserId { get; private set; }
    public int? CurrentPublicationNumber { get; private set; }
    public DateTimeOffset? ArchivedAtUtc { get; private set; }
    public DateTimeOffset CreatedAtUtc { get; private set; }
    public DateTimeOffset UpdatedAtUtc { get; private set; }
    public Guid Version { get; private set; }
    public bool IsArchived => ArchivedAtUtc is not null;

    public static Dashboard Create(Guid agencyId, Guid clientId, Guid? folderId, string title,
        string? description, Guid createdByUserId, DateTimeOffset now)
    {
        if (agencyId == Guid.Empty || clientId == Guid.Empty || createdByUserId == Guid.Empty)
            throw new ArgumentException("Agency, client and creator are required.");
        if (folderId == Guid.Empty) throw new ArgumentException("Folder id cannot be empty.", nameof(folderId));
        var utcNow = now.ToUniversalTime();
        return new Dashboard
        {
            Id = Guid.NewGuid(),
            AgencyId = agencyId,
            ClientId = clientId,
            FolderId = folderId,
            Title = NormalizeRequired(title, MaxTitleLength, nameof(title)),
            Description = NormalizeOptional(description, MaxDescriptionLength, nameof(description)),
            CreatedByUserId = createdByUserId,
            CreatedAtUtc = utcNow,
            UpdatedAtUtc = utcNow,
            Version = Guid.NewGuid()
        };
    }

    public void UpdateDetails(string title, string? description, DateTimeOffset now)
    {
        RequireActive();
        Title = NormalizeRequired(title, MaxTitleLength, nameof(title));
        Description = NormalizeOptional(description, MaxDescriptionLength, nameof(description));
        Touch(now);
    }

    public void Move(Guid? folderId, DateTimeOffset now)
    {
        RequireActive();
        if (folderId == Guid.Empty) throw new ArgumentException("Folder id cannot be empty.", nameof(folderId));
        FolderId = folderId;
        Touch(now);
    }

    public int RegisterPublication(DateTimeOffset now)
    {
        RequireActive();
        CurrentPublicationNumber = (CurrentPublicationNumber ?? 0) + 1;
        Touch(now);
        return CurrentPublicationNumber.Value;
    }

    public void Archive(DateTimeOffset now)
    {
        if (IsArchived) return;
        ArchivedAtUtc = now.ToUniversalTime();
        Touch(now);
    }

    public void Restore(DateTimeOffset now)
    {
        if (!IsArchived) return;
        ArchivedAtUtc = null;
        Touch(now);
    }

    private void RequireActive()
    {
        if (IsArchived) throw new InvalidOperationException("Restore the dashboard before modifying it.");
    }

    private void Touch(DateTimeOffset now)
    {
        UpdatedAtUtc = now.ToUniversalTime();
        Version = Guid.NewGuid();
    }

    internal static string NormalizeRequired(string value, int maxLength, string parameter)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(value, parameter);
        var normalized = value.Trim();
        if (normalized.Length > maxLength)
            throw new ArgumentException($"{parameter} cannot exceed {maxLength} characters.", parameter);
        return normalized;
    }

    internal static string NormalizeOptional(string? value, int maxLength, string parameter)
    {
        if (string.IsNullOrWhiteSpace(value)) return string.Empty;
        var normalized = value.Trim();
        if (normalized.Length > maxLength)
            throw new ArgumentException($"{parameter} cannot exceed {maxLength} characters.", parameter);
        return normalized;
    }
}
