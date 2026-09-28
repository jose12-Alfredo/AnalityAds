using System.ComponentModel.DataAnnotations;
using AnaliticAsd.Domain.Folders;

namespace AnaliticAsd.Contracts.Folders;

public sealed class CreateFolderRequest
{
    [Required, StringLength(Folder.MaxNameLength)]
    public string Name { get; init; } = string.Empty;
    public Guid? ParentFolderId { get; init; }
}

public sealed class UpdateFolderRequest
{
    [Required, StringLength(Folder.MaxNameLength)]
    public string Name { get; init; } = string.Empty;
    public Guid ExpectedVersion { get; init; }
}

public sealed class MoveFolderRequest
{
    public Guid? ParentFolderId { get; init; }
    [Range(0, int.MaxValue)]
    public int SortOrder { get; init; }
    public Guid ExpectedVersion { get; init; }
}

public sealed class RestoreFolderRequest
{
    public Guid ExpectedVersion { get; init; }
}

public sealed record FolderResponse(Guid Id, Guid ClientId, Guid? ParentFolderId, string Name, int SortOrder,
    bool IsArchived, DateTimeOffset CreatedAtUtc, DateTimeOffset UpdatedAtUtc, Guid Version);
