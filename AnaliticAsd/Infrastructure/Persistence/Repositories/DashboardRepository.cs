using AnaliticAsd.Application.Common;
using AnaliticAsd.Application.Dashboards;
using AnaliticAsd.Application.Identity;
using AnaliticAsd.Domain.Dashboards;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace AnaliticAsd.Infrastructure.Persistence.Repositories;

public sealed class DashboardRepository(AnalitiAdsDbContext dbContext, ICurrentTenant currentTenant)
    : IDashboardRepository, IDashboardSourceAccess
{
    private IQueryable<Dashboard> Scoped => new AuthorizedData(dbContext, currentTenant).Dashboards;

    public async Task<IReadOnlyList<DashboardRecord>> ListAsync(Guid clientId, Guid? folderId,
        bool includeArchived, string? search, CancellationToken cancellationToken = default)
    {
        var query = from dashboard in Scoped.AsNoTracking()
                    join draft in dbContext.DashboardDrafts.AsNoTracking()
                        on dashboard.Id equals draft.DashboardId
                    where dashboard.ClientId == clientId
                    select new { Dashboard = dashboard, Draft = draft };
        if (folderId is not null) query = query.Where(row => row.Dashboard.FolderId == folderId);
        if (!includeArchived)
            query = query.Where(row => row.Dashboard.ArchivedAtUtc == null
                && (row.Dashboard.FolderId == null || !dbContext.Folders.Any(folder =>
                    folder.Id == row.Dashboard.FolderId && folder.ArchivedAtUtc != null)));
        if (!string.IsNullOrWhiteSpace(search))
        {
            var normalized = search.Trim().ToLower();
            query = query.Where(row => row.Dashboard.Title.ToLower().Contains(normalized));
        }
        var rows = await query.OrderBy(row => row.Dashboard.Title).ThenBy(row => row.Dashboard.Id)
            .ToArrayAsync(cancellationToken);
        return rows.Select(row => new DashboardRecord(row.Dashboard, row.Draft.Revision,
            row.Draft.UpdatedAtUtc)).ToArray();
    }

    public Task<Dashboard?> GetAsync(Guid dashboardId, bool trackChanges,
        CancellationToken cancellationToken = default)
    {
        IQueryable<Dashboard> query = Scoped;
        if (!trackChanges) query = query.AsNoTracking();
        return query.SingleOrDefaultAsync(dashboard => dashboard.Id == dashboardId, cancellationToken);
    }

    public Task<DashboardDraft?> GetDraftAsync(Guid dashboardId, bool trackChanges,
        CancellationToken cancellationToken = default)
    {
        IQueryable<DashboardDraft> query = dbContext.DashboardDrafts
            .Where(draft => Scoped.Any(dashboard => dashboard.Id == draft.DashboardId));
        if (!trackChanges) query = query.AsNoTracking();
        return query.SingleOrDefaultAsync(draft => draft.DashboardId == dashboardId, cancellationToken);
    }

    public Task<DashboardVersion?> GetPublishedAsync(Guid dashboardId, int publicationNumber,
        CancellationToken cancellationToken = default) =>
        dbContext.DashboardVersions.AsNoTracking()
            .Where(version => Scoped.Any(dashboard => dashboard.Id == version.DashboardId))
            .SingleOrDefaultAsync(version => version.DashboardId == dashboardId
                && version.PublicationNumber == publicationNumber, cancellationToken);

    public async Task<IReadOnlyList<DashboardTemplate>> ListTemplatesAsync(Guid clientId,
        CancellationToken cancellationToken = default)
    {
        var clients = new AuthorizedData(dbContext, currentTenant).Clients;
        return await dbContext.DashboardTemplates.AsNoTracking()
            .Where(template => clients.Any(client => client.Id == clientId)
                && template.AgencyId == currentTenant.AgencyId
                && (template.ClientId == null || template.ClientId == clientId))
            .OrderBy(template => template.ClientId == null ? 0 : 1)
            .ThenBy(template => template.Name)
            .ToArrayAsync(cancellationToken);
    }

    public Task<DashboardTemplate?> GetTemplateAsync(Guid templateId, Guid clientId,
        CancellationToken cancellationToken = default)
    {
        var clients = new AuthorizedData(dbContext, currentTenant).Clients;
        return dbContext.DashboardTemplates.AsNoTracking().SingleOrDefaultAsync(template =>
            clients.Any(client => client.Id == clientId) && template.Id == templateId
            && template.AgencyId == currentTenant.AgencyId
            && (template.ClientId == null || template.ClientId == clientId), cancellationToken);
    }

    public void Add(Dashboard dashboard) => dbContext.Dashboards.Add(dashboard);
    public void Add(DashboardDraft draft) => dbContext.DashboardDrafts.Add(draft);
    public void Add(DashboardVersion version) => dbContext.DashboardVersions.Add(version);
    public void Add(DashboardTemplate template) => dbContext.DashboardTemplates.Add(template);

    public async Task<bool> AllBelongToClientAsync(Guid clientId, IReadOnlyCollection<Guid> dataSourceIds,
        CancellationToken cancellationToken = default)
    {
        if (dataSourceIds.Count == 0) return true;
        var distinct = dataSourceIds.Distinct().ToArray();
        var count = await dbContext.DataSources.AsNoTracking().CountAsync(source =>
            source.AgencyId == currentTenant.AgencyId && source.ClientId == clientId
            && source.ArchivedAtUtc == null && distinct.Contains(source.Id), cancellationToken);
        return count == distinct.Length;
    }

    public async Task SaveAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            await dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateConcurrencyException)
        {
            throw new ConflictException("The dashboard changed concurrently. Refresh and try again.");
        }
        catch (DbUpdateException exception) when (exception.InnerException is PostgresException
            { SqlState: PostgresErrorCodes.UniqueViolation }
            or Microsoft.Data.Sqlite.SqliteException { SqliteExtendedErrorCode: 1555 or 2067 })
        {
            throw new ConflictException("The dashboard or publication already exists. Refresh and try again.");
        }
        catch (DbUpdateException)
        {
            throw new ConflictException("The dashboard could not be saved because one of its references is invalid.");
        }
    }
}
