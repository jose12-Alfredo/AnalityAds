using AnaliticAsd.Application.Common;
using AnaliticAsd.Application.Folders;
using AnaliticAsd.Application.Identity;
using AnaliticAsd.Domain.Folders;
using Microsoft.EntityFrameworkCore;

namespace AnaliticAsd.Infrastructure.Persistence.Repositories;

public sealed class FolderRepository(AnalitiAdsDbContext dbContext, ICurrentTenant currentTenant)
    : IFolderRepository
{
    private IQueryable<Folder> Scoped => new AuthorizedData(dbContext, currentTenant).Folders;

    public async Task<IReadOnlyList<Folder>> ListAsync(Guid clientId, bool includeArchived, string? search,
        bool trackChanges, CancellationToken cancellationToken = default)
    {
        IQueryable<Folder> query = Scoped.Where(folder => folder.ClientId == clientId);
        if (!includeArchived) query = query.Where(folder => folder.ArchivedAtUtc == null);
        if (!string.IsNullOrWhiteSpace(search))
        {
            var normalized = search.Trim().ToLower();
            query = query.Where(folder => folder.Name.ToLower().Contains(normalized));
        }
        if (!trackChanges) query = query.AsNoTracking();
        return await query.OrderBy(folder => folder.ParentFolderId)
            .ThenBy(folder => folder.SortOrder)
            .ThenBy(folder => folder.Name)
            .ToArrayAsync(cancellationToken);
    }

    public Task<Folder?> GetByIdAsync(Guid folderId, bool trackChanges,
        CancellationToken cancellationToken = default)
    {
        IQueryable<Folder> query = Scoped;
        if (!trackChanges) query = query.AsNoTracking();
        return query.SingleOrDefaultAsync(folder => folder.Id == folderId, cancellationToken);
    }

    public void Add(Folder folder) => dbContext.Folders.Add(folder);

    public async Task SaveAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            await dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateConcurrencyException)
        {
            throw new ConflictException("The folder changed concurrently. Refresh and try again.");
        }
        catch (DbUpdateException)
        {
            throw new ConflictException("The folder hierarchy could not be saved. Refresh and try again.");
        }
    }
}
