using AnaliticAsd.Domain.Folders;

namespace AnaliticAsd.Application.Folders;

public sealed record CreateFolderCommand(string Name, Guid? ParentFolderId);
public sealed record UpdateFolderCommand(string Name, Guid ExpectedVersion);
public sealed record MoveFolderCommand(Guid? ParentFolderId, int SortOrder, Guid ExpectedVersion);
public sealed record FolderModel(Guid Id, Guid ClientId, Guid? ParentFolderId, string Name, int SortOrder,
    bool IsArchived, DateTimeOffset CreatedAtUtc, DateTimeOffset UpdatedAtUtc, Guid Version);

public interface IFolderRepository
{
    Task<IReadOnlyList<Folder>> ListAsync(Guid clientId, bool includeArchived, string? search, bool trackChanges,
        CancellationToken cancellationToken = default);
    Task<Folder?> GetByIdAsync(Guid folderId, bool trackChanges,
        CancellationToken cancellationToken = default);
    void Add(Folder folder);
    Task SaveAsync(CancellationToken cancellationToken = default);
}

public interface IFolderService
{
    Task<IReadOnlyList<FolderModel>> ListAsync(Guid clientId, bool includeArchived, string? search,
        CancellationToken cancellationToken = default);
    Task<FolderModel> CreateAsync(Guid clientId, CreateFolderCommand command,
        CancellationToken cancellationToken = default);
    Task<FolderModel> UpdateAsync(Guid folderId, UpdateFolderCommand command,
        CancellationToken cancellationToken = default);
    Task<FolderModel> MoveAsync(Guid folderId, MoveFolderCommand command,
        CancellationToken cancellationToken = default);
    Task ArchiveAsync(Guid folderId, Guid expectedVersion, CancellationToken cancellationToken = default);
    Task<FolderModel> RestoreAsync(Guid folderId, Guid expectedVersion,
        CancellationToken cancellationToken = default);
}
