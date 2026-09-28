using AnaliticAsd.Application.Clients;
using AnaliticAsd.Application.Common;
using AnaliticAsd.Application.Identity;
using AnaliticAsd.Domain.Folders;
using AnaliticAsd.Domain.Identity;

namespace AnaliticAsd.Application.Folders;

public sealed class FolderService(IFolderRepository folderRepository, IClientRepository clientRepository,
    ICurrentTenant currentTenant, TimeProvider timeProvider) : IFolderService
{
    public async Task<IReadOnlyList<FolderModel>> ListAsync(Guid clientId, bool includeArchived, string? search,
        CancellationToken cancellationToken = default)
    {
        RequireReader();
        await RequireClientAsync(clientId, cancellationToken);
        var normalizedSearch = NormalizeSearch(search);
        return (await folderRepository.ListAsync(clientId, includeArchived, normalizedSearch, false,
                cancellationToken))
            .Select(Map)
            .ToArray();
    }

    public async Task<FolderModel> CreateAsync(Guid clientId, CreateFolderCommand command,
        CancellationToken cancellationToken = default)
    {
        RequireEditor();
        var client = await RequireClientAsync(clientId, cancellationToken);
        if (!client.IsActive) throw new ConflictException("Activate the client before creating folders.");
        if (command.ParentFolderId is { } parentId)
            await RequireActiveParentAsync(parentId, clientId, cancellationToken);

        var siblings = await folderRepository.ListAsync(clientId, false, null, false, cancellationToken);
        var nextSortOrder = siblings
            .Where(folder => folder.ParentFolderId == command.ParentFolderId)
            .Select(folder => folder.SortOrder)
            .DefaultIfEmpty(-1)
            .Max() + 1;
        var folder = Folder.Create(currentTenant.AgencyId, clientId, command.ParentFolderId, command.Name,
            nextSortOrder, timeProvider.GetUtcNow());
        folderRepository.Add(folder);
        await folderRepository.SaveAsync(cancellationToken);
        return Map(folder);
    }

    public async Task<FolderModel> UpdateAsync(Guid folderId, UpdateFolderCommand command,
        CancellationToken cancellationToken = default)
    {
        RequireEditor();
        var folder = await RequireFolderAsync(folderId, true, cancellationToken);
        EnsureVersion(folder, command.ExpectedVersion);
        if (folder.IsArchived) throw new ConflictException("Restore the folder before renaming it.");
        folder.Rename(command.Name, timeProvider.GetUtcNow());
        await folderRepository.SaveAsync(cancellationToken);
        return Map(folder);
    }

    public async Task<FolderModel> MoveAsync(Guid folderId, MoveFolderCommand command,
        CancellationToken cancellationToken = default)
    {
        RequireEditor();
        var folder = await RequireFolderAsync(folderId, true, cancellationToken);
        EnsureVersion(folder, command.ExpectedVersion);
        if (folder.IsArchived) throw new ConflictException("Restore the folder before moving it.");

        if (command.ParentFolderId is { } parentId)
        {
            var parent = await RequireActiveParentAsync(parentId, folder.ClientId, cancellationToken);
            var allFolders = await folderRepository.ListAsync(folder.ClientId, true, null, false,
                cancellationToken);
            if (DescendantIds(folder.Id, allFolders).Contains(parent.Id))
                throw new ConflictException("A folder cannot be moved inside one of its descendants.");
        }

        folder.Move(command.ParentFolderId, command.SortOrder, timeProvider.GetUtcNow());
        await folderRepository.SaveAsync(cancellationToken);
        return Map(folder);
    }

    public async Task ArchiveAsync(Guid folderId, Guid expectedVersion,
        CancellationToken cancellationToken = default)
    {
        RequireEditor();
        var folder = await RequireFolderAsync(folderId, true, cancellationToken);
        EnsureVersion(folder, expectedVersion);
        if (folder.IsArchived) throw new ConflictException("The folder is already archived.");

        var allFolders = await folderRepository.ListAsync(folder.ClientId, true, null, true, cancellationToken);
        var affected = DescendantIds(folder.Id, allFolders);
        var now = timeProvider.GetUtcNow();
        foreach (var item in allFolders.Where(item => affected.Contains(item.Id))) item.Archive(now);
        await folderRepository.SaveAsync(cancellationToken);
    }

    public async Task<FolderModel> RestoreAsync(Guid folderId, Guid expectedVersion,
        CancellationToken cancellationToken = default)
    {
        RequireEditor();
        var folder = await RequireFolderAsync(folderId, true, cancellationToken);
        EnsureVersion(folder, expectedVersion);
        if (!folder.IsArchived) throw new ConflictException("The folder is not archived.");
        if (folder.ParentFolderId is { } parentId)
        {
            var parent = await folderRepository.GetByIdAsync(parentId, false, cancellationToken)
                ?? throw new ConflictException("Restore the parent folder first.");
            if (parent.ClientId != folder.ClientId || parent.IsArchived)
                throw new ConflictException("Restore the parent folder first.");
        }

        var allFolders = await folderRepository.ListAsync(folder.ClientId, true, null, true, cancellationToken);
        var affected = DescendantIds(folder.Id, allFolders);
        var now = timeProvider.GetUtcNow();
        foreach (var item in allFolders.Where(item => affected.Contains(item.Id))) item.Restore(now);
        await folderRepository.SaveAsync(cancellationToken);
        return Map(folder);
    }

    private async Task<AnaliticAsd.Domain.Clients.Client> RequireClientAsync(Guid clientId, CancellationToken cancellationToken) =>
        await clientRepository.GetByIdAsync(clientId, false, cancellationToken)
        ?? throw new EntityNotFoundException($"Client '{clientId}' was not found.");

    private async Task<Folder> RequireFolderAsync(Guid folderId, bool trackChanges,
        CancellationToken cancellationToken) =>
        await folderRepository.GetByIdAsync(folderId, trackChanges, cancellationToken)
        ?? throw new EntityNotFoundException($"Folder '{folderId}' was not found.");

    private async Task<Folder> RequireActiveParentAsync(Guid parentId, Guid clientId,
        CancellationToken cancellationToken)
    {
        var parent = await RequireFolderAsync(parentId, false, cancellationToken);
        if (parent.ClientId != clientId)
            throw new ConflictException("Folders cannot be moved between clients.");
        if (parent.IsArchived) throw new ConflictException("Restore the parent folder first.");
        return parent;
    }

    private void RequireEditor()
    {
        if (currentTenant.Role is not (nameof(AgencyRole.Owner) or nameof(AgencyRole.Admin) or nameof(AgencyRole.Analyst)))
            throw new ForbiddenException("Only agency administrators and assigned editors can modify folders.");
    }

    private void RequireReader()
    {
        if (currentTenant.Role is not (nameof(AgencyRole.Owner) or nameof(AgencyRole.Admin)
            or nameof(AgencyRole.Analyst) or nameof(AgencyRole.Viewer)))
            throw new ForbiddenException("This role cannot browse the agency folder structure.");
    }

    private static void EnsureVersion(Folder folder, Guid expectedVersion)
    {
        if (expectedVersion == Guid.Empty) throw new ArgumentException("Expected version is required.");
        if (folder.Version != expectedVersion)
            throw new ConflictException("The folder changed since it was loaded. Refresh and try again.");
    }

    private static HashSet<Guid> DescendantIds(Guid rootId, IReadOnlyList<Folder> folders)
    {
        var result = new HashSet<Guid> { rootId };
        var pending = new Queue<Guid>();
        var childrenByParent = folders
            .Where(folder => folder.ParentFolderId is not null)
            .GroupBy(folder => folder.ParentFolderId!.Value)
            .ToDictionary(group => group.Key, group => group.ToArray());
        pending.Enqueue(rootId);
        while (pending.TryDequeue(out var parentId))
        {
            if (!childrenByParent.TryGetValue(parentId, out var children)) continue;
            foreach (var child in children)
                if (result.Add(child.Id)) pending.Enqueue(child.Id);
        }
        return result;
    }

    private static string? NormalizeSearch(string? search)
    {
        if (string.IsNullOrWhiteSpace(search)) return null;
        var normalized = search.Trim();
        if (normalized.Length > Folder.MaxNameLength)
            throw new ArgumentException($"Search cannot exceed {Folder.MaxNameLength} characters.", nameof(search));
        return normalized;
    }

    private static FolderModel Map(Folder folder) => new(folder.Id, folder.ClientId, folder.ParentFolderId,
        folder.Name, folder.SortOrder, folder.IsArchived, folder.CreatedAtUtc, folder.UpdatedAtUtc, folder.Version);
}
