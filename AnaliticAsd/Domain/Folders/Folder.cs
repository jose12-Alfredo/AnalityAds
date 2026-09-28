namespace AnaliticAsd.Domain.Folders;

public sealed class Folder
{
    public const int MaxNameLength = 200;

    private Folder() { }

    public Guid Id { get; private set; }
    public Guid AgencyId { get; private set; }
    public Guid ClientId { get; private set; }
    public Guid? ParentFolderId { get; private set; }
    public string Name { get; private set; } = string.Empty;
    public int SortOrder { get; private set; }
    public DateTimeOffset? ArchivedAtUtc { get; private set; }
    public DateTimeOffset CreatedAtUtc { get; private set; }
    public DateTimeOffset UpdatedAtUtc { get; private set; }
    public Guid Version { get; private set; }
    public bool IsArchived => ArchivedAtUtc is not null;

    public static Folder Create(Guid agencyId, Guid clientId, Guid? parentFolderId, string name, int sortOrder,
        DateTimeOffset now)
    {
        if (agencyId == Guid.Empty) throw new ArgumentException("Agency id is required.", nameof(agencyId));
        if (clientId == Guid.Empty) throw new ArgumentException("Client id is required.", nameof(clientId));
        if (parentFolderId == Guid.Empty) throw new ArgumentException("Parent folder id cannot be empty.", nameof(parentFolderId));
        if (sortOrder < 0) throw new ArgumentOutOfRangeException(nameof(sortOrder), "Sort order cannot be negative.");
        var utcNow = now.ToUniversalTime();
        return new Folder
        {
            Id = Guid.NewGuid(),
            AgencyId = agencyId,
            ClientId = clientId,
            ParentFolderId = parentFolderId,
            Name = NormalizeName(name),
            SortOrder = sortOrder,
            CreatedAtUtc = utcNow,
            UpdatedAtUtc = utcNow,
            Version = Guid.NewGuid()
        };
    }

    public void Rename(string name, DateTimeOffset now)
    {
        if (IsArchived) throw new InvalidOperationException("Restore the folder before renaming it.");
        Name = NormalizeName(name);
        Touch(now);
    }

    public void Move(Guid? parentFolderId, int sortOrder, DateTimeOffset now)
    {
        if (IsArchived) throw new InvalidOperationException("Restore the folder before moving it.");
        if (parentFolderId == Guid.Empty) throw new ArgumentException("Parent folder id cannot be empty.", nameof(parentFolderId));
        if (parentFolderId == Id) throw new ArgumentException("A folder cannot be its own parent.", nameof(parentFolderId));
        if (sortOrder < 0) throw new ArgumentOutOfRangeException(nameof(sortOrder), "Sort order cannot be negative.");
        ParentFolderId = parentFolderId;
        SortOrder = sortOrder;
        Touch(now);
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

    private void Touch(DateTimeOffset now)
    {
        UpdatedAtUtc = now.ToUniversalTime();
        Version = Guid.NewGuid();
    }

    private static string NormalizeName(string name)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        var normalized = name.Trim();
        if (normalized.Length > MaxNameLength)
            throw new ArgumentException($"Folder name cannot exceed {MaxNameLength} characters.", nameof(name));
        return normalized;
    }
}
