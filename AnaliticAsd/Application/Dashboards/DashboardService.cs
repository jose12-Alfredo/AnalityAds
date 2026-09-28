using System.Text.Json;
using AnaliticAsd.Application.Clients;
using AnaliticAsd.Application.Common;
using AnaliticAsd.Application.Folders;
using AnaliticAsd.Application.Identity;
using AnaliticAsd.Domain.Dashboards;
using AnaliticAsd.Domain.Folders;
using AnaliticAsd.Domain.Identity;

namespace AnaliticAsd.Application.Dashboards;

public sealed class DashboardService(IDashboardRepository repository, IDashboardDefinitionValidator validator,
    IClientRepository clientRepository, IFolderRepository folderRepository, ICurrentTenant currentTenant,
    TimeProvider timeProvider) : IDashboardService
{
    public async Task<IReadOnlyList<DashboardListItemModel>> ListAsync(Guid clientId, Guid? folderId,
        bool includeArchived, string? search, CancellationToken cancellationToken = default)
    {
        RequireReader();
        await RequireClientAsync(clientId, cancellationToken);
        if (folderId is not null) await RequireFolderAsync(folderId.Value, clientId, allowArchived: includeArchived,
            cancellationToken);
        return (await repository.ListAsync(clientId, folderId, includeArchived, NormalizeSearch(search),
                cancellationToken))
            .Select(MapList)
            .ToArray();
    }

    public async Task<DashboardModel> GetAsync(Guid dashboardId,
        CancellationToken cancellationToken = default)
    {
        RequireReader();
        var dashboard = await RequireDashboardAsync(dashboardId, false, cancellationToken);
        var draft = await RequireDraftAsync(dashboardId, false, cancellationToken);
        return Map(dashboard, draft.Revision, draft.UpdatedAtUtc);
    }

    public async Task<DashboardModel> CreateAsync(Guid clientId, CreateDashboardCommand command,
        CancellationToken cancellationToken = default)
    {
        RequireEditor();
        var client = await RequireClientAsync(clientId, cancellationToken);
        if (!client.IsActive) throw new ConflictException("Activate the client before creating dashboards.");
        if (command.FolderId is { } folderId)
            await RequireFolderAsync(folderId, clientId, allowArchived: false, cancellationToken);

        ValidatedDashboardDefinition definition;
        if (command.TemplateId is { } templateId)
        {
            var template = await repository.GetTemplateAsync(templateId, clientId, cancellationToken)
                ?? throw new EntityNotFoundException("Dashboard template was not found.");
            definition = await validator.BindTemplateAsync(template.DefinitionJson, command.SourceBindings,
                clientId, cancellationToken);
        }
        else
        {
            if (command.SourceBindings.Count > 0)
                throw new ArgumentException("Source bindings require a dashboard template.");
            definition = await validator.ValidateDashboardAsync(validator.CreateBlank(), clientId,
                cancellationToken);
        }

        var now = timeProvider.GetUtcNow();
        var dashboard = Dashboard.Create(currentTenant.AgencyId, clientId, command.FolderId, command.Title,
            command.Description, currentTenant.UserId, now);
        var draft = DashboardDraft.Create(dashboard.Id, dashboard.AgencyId, dashboard.ClientId,
            definition.SchemaVersion, definition.Json, currentTenant.UserId, now);
        repository.Add(dashboard);
        repository.Add(draft);
        await repository.SaveAsync(cancellationToken);
        return Map(dashboard, draft.Revision, draft.UpdatedAtUtc);
    }

    public async Task<DashboardModel> UpdateAsync(Guid dashboardId, UpdateDashboardCommand command,
        CancellationToken cancellationToken = default)
    {
        RequireEditor();
        var dashboard = await RequireDashboardAsync(dashboardId, true, cancellationToken);
        EnsureVersion(dashboard, command.ExpectedVersion);
        if (dashboard.IsArchived) throw new ConflictException("Restore the dashboard before modifying it.");
        dashboard.UpdateDetails(command.Title, command.Description, timeProvider.GetUtcNow());
        await repository.SaveAsync(cancellationToken);
        var draft = await RequireDraftAsync(dashboardId, false, cancellationToken);
        return Map(dashboard, draft.Revision, draft.UpdatedAtUtc);
    }

    public async Task<DashboardModel> MoveAsync(Guid dashboardId, MoveDashboardCommand command,
        CancellationToken cancellationToken = default)
    {
        RequireEditor();
        var dashboard = await RequireDashboardAsync(dashboardId, true, cancellationToken);
        EnsureVersion(dashboard, command.ExpectedVersion);
        if (dashboard.IsArchived) throw new ConflictException("Restore the dashboard before moving it.");
        if (command.FolderId is { } folderId)
            await RequireFolderAsync(folderId, dashboard.ClientId, allowArchived: false, cancellationToken);
        dashboard.Move(command.FolderId, timeProvider.GetUtcNow());
        await repository.SaveAsync(cancellationToken);
        var draft = await RequireDraftAsync(dashboardId, false, cancellationToken);
        return Map(dashboard, draft.Revision, draft.UpdatedAtUtc);
    }

    public async Task<DashboardModel> DuplicateAsync(Guid dashboardId, DuplicateDashboardCommand command,
        CancellationToken cancellationToken = default)
    {
        RequireEditor();
        var source = await RequireDashboardAsync(dashboardId, false, cancellationToken);
        if (source.IsArchived) throw new ConflictException("Restore the dashboard before duplicating it.");
        var sourceDraft = await RequireDraftAsync(dashboardId, false, cancellationToken);
        var destination = await RequireClientAsync(command.DestinationClientId, cancellationToken);
        if (!destination.IsActive) throw new ConflictException("Activate the destination client first.");
        if (command.FolderId is { } folderId)
            await RequireFolderAsync(folderId, destination.Id, allowArchived: false, cancellationToken);
        var definition = await validator.PrepareDuplicateAsync(sourceDraft.DefinitionJson, source.ClientId,
            destination.Id, cancellationToken);
        var now = timeProvider.GetUtcNow();
        var title = string.IsNullOrWhiteSpace(command.Title) ? $"{source.Title} (copia)" : command.Title;
        var copy = Dashboard.Create(currentTenant.AgencyId, destination.Id, command.FolderId, title,
            source.Description, currentTenant.UserId, now);
        var draft = DashboardDraft.Create(copy.Id, copy.AgencyId, copy.ClientId, definition.SchemaVersion,
            definition.Json, currentTenant.UserId, now);
        repository.Add(copy);
        repository.Add(draft);
        await repository.SaveAsync(cancellationToken);
        return Map(copy, draft.Revision, draft.UpdatedAtUtc);
    }

    public async Task ArchiveAsync(Guid dashboardId, Guid expectedVersion,
        CancellationToken cancellationToken = default)
    {
        RequireEditor();
        var dashboard = await RequireDashboardAsync(dashboardId, true, cancellationToken);
        EnsureVersion(dashboard, expectedVersion);
        if (dashboard.IsArchived) throw new ConflictException("The dashboard is already archived.");
        dashboard.Archive(timeProvider.GetUtcNow());
        await repository.SaveAsync(cancellationToken);
    }

    public async Task<DashboardModel> RestoreAsync(Guid dashboardId, Guid expectedVersion,
        CancellationToken cancellationToken = default)
    {
        RequireEditor();
        var dashboard = await RequireDashboardAsync(dashboardId, true, cancellationToken);
        EnsureVersion(dashboard, expectedVersion);
        if (!dashboard.IsArchived) throw new ConflictException("The dashboard is not archived.");
        if (dashboard.FolderId is { } folderId)
            await RequireFolderAsync(folderId, dashboard.ClientId, allowArchived: false, cancellationToken);
        dashboard.Restore(timeProvider.GetUtcNow());
        await repository.SaveAsync(cancellationToken);
        var draft = await RequireDraftAsync(dashboardId, false, cancellationToken);
        return Map(dashboard, draft.Revision, draft.UpdatedAtUtc);
    }

    public async Task<DashboardDraftModel> GetDraftAsync(Guid dashboardId,
        CancellationToken cancellationToken = default)
    {
        RequireEditor();
        _ = await RequireDashboardAsync(dashboardId, false, cancellationToken);
        return Map(await RequireDraftAsync(dashboardId, false, cancellationToken));
    }

    public async Task<DashboardDraftModel> SaveDraftAsync(Guid dashboardId, SaveDashboardDraftCommand command,
        CancellationToken cancellationToken = default)
    {
        RequireEditor();
        var dashboard = await RequireDashboardAsync(dashboardId, false, cancellationToken);
        if (dashboard.IsArchived) throw new ConflictException("Restore the dashboard before saving its draft.");
        var draft = await RequireDraftAsync(dashboardId, true, cancellationToken);
        if (draft.Revision != command.ExpectedRevision)
            throw new ConflictException("The dashboard draft changed since it was loaded. Refresh and try again.");
        var definition = await validator.ValidateDashboardAsync(command.DefinitionJson, dashboard.ClientId,
            cancellationToken);
        draft.Replace(command.ExpectedRevision, definition.SchemaVersion, definition.Json, currentTenant.UserId,
            timeProvider.GetUtcNow());
        await repository.SaveAsync(cancellationToken);
        return Map(draft);
    }

    public async Task<PublishedDashboardModel> PublishAsync(Guid dashboardId, PublishDashboardCommand command,
        CancellationToken cancellationToken = default)
    {
        RequireEditor();
        var dashboard = await RequireDashboardAsync(dashboardId, true, cancellationToken);
        if (dashboard.IsArchived) throw new ConflictException("Restore the dashboard before publishing it.");
        var draft = await RequireDraftAsync(dashboardId, false, cancellationToken);
        if (draft.Revision != command.ExpectedRevision)
            throw new ConflictException("The dashboard draft changed since it was loaded. Refresh and try again.");
        var definition = await validator.ValidateForPublicationAsync(draft.DefinitionJson, dashboard.ClientId,
            cancellationToken);
        var now = timeProvider.GetUtcNow();
        var publicationNumber = dashboard.RegisterPublication(now);
        var version = DashboardVersion.Create(dashboard.Id, dashboard.AgencyId, dashboard.ClientId,
            publicationNumber, draft.Revision, definition.SchemaVersion, definition.Json, currentTenant.UserId, now);
        repository.Add(version);
        await repository.SaveAsync(cancellationToken);
        return Map(version);
    }

    public async Task<PublishedDashboardModel> GetPublishedAsync(Guid dashboardId,
        CancellationToken cancellationToken = default)
    {
        RequireReader();
        var dashboard = await RequireDashboardAsync(dashboardId, false, cancellationToken);
        if (dashboard.CurrentPublicationNumber is not { } publicationNumber)
            throw new EntityNotFoundException("The dashboard has not been published.");
        return Map(await repository.GetPublishedAsync(dashboardId, publicationNumber, cancellationToken)
            ?? throw new EntityNotFoundException("The published dashboard version was not found."));
    }

    public async Task<IReadOnlyList<DashboardTemplateModel>> ListTemplatesAsync(Guid clientId,
        CancellationToken cancellationToken = default)
    {
        RequireReader();
        await RequireClientAsync(clientId, cancellationToken);
        return (await repository.ListTemplatesAsync(clientId, cancellationToken)).Select(Map).ToArray();
    }

    public async Task<DashboardTemplateModel> CreateClientTemplateAsync(Guid clientId,
        CreateDashboardTemplateCommand command, CancellationToken cancellationToken = default)
    {
        RequireEditor();
        await RequireClientAsync(clientId, cancellationToken);
        return await CreateTemplateAsync(clientId, command, cancellationToken);
    }

    public async Task<DashboardTemplateModel> CreateAgencyTemplateAsync(CreateDashboardTemplateCommand command,
        CancellationToken cancellationToken = default)
    {
        RequireAdministrator();
        return await CreateTemplateAsync(null, command, cancellationToken);
    }

    private async Task<DashboardTemplateModel> CreateTemplateAsync(Guid? clientId,
        CreateDashboardTemplateCommand command, CancellationToken cancellationToken)
    {
        var definition = validator.ValidateTemplate(command.DefinitionJson);
        var template = DashboardTemplate.Create(currentTenant.AgencyId, clientId, command.Name,
            command.Description, definition.SchemaVersion, definition.Json, currentTenant.UserId,
            timeProvider.GetUtcNow());
        repository.Add(template);
        await repository.SaveAsync(cancellationToken);
        return Map(template);
    }

    private async Task<AnaliticAsd.Domain.Clients.Client> RequireClientAsync(Guid clientId,
        CancellationToken cancellationToken) =>
        await clientRepository.GetByIdAsync(clientId, false, cancellationToken)
        ?? throw new EntityNotFoundException($"Client '{clientId}' was not found.");

    private async Task<Folder> RequireFolderAsync(Guid folderId, Guid clientId, bool allowArchived,
        CancellationToken cancellationToken)
    {
        var folder = await folderRepository.GetByIdAsync(folderId, false, cancellationToken)
            ?? throw new EntityNotFoundException($"Folder '{folderId}' was not found.");
        if (folder.ClientId != clientId) throw new ConflictException("The folder belongs to another client.");
        if (!allowArchived && folder.IsArchived) throw new ConflictException("Restore the folder first.");
        return folder;
    }

    private async Task<Dashboard> RequireDashboardAsync(Guid dashboardId, bool trackChanges,
        CancellationToken cancellationToken) =>
        await repository.GetAsync(dashboardId, trackChanges, cancellationToken)
        ?? throw new EntityNotFoundException($"Dashboard '{dashboardId}' was not found.");

    private async Task<DashboardDraft> RequireDraftAsync(Guid dashboardId, bool trackChanges,
        CancellationToken cancellationToken) =>
        await repository.GetDraftAsync(dashboardId, trackChanges, cancellationToken)
        ?? throw new EntityNotFoundException("Dashboard draft was not found.");

    private void RequireReader()
    {
        if (currentTenant.Role is not (nameof(AgencyRole.Owner) or nameof(AgencyRole.Admin)
            or nameof(AgencyRole.Analyst) or nameof(AgencyRole.Viewer)))
            throw new ForbiddenException("This role cannot browse agency dashboards.");
    }

    private void RequireEditor()
    {
        if (currentTenant.Role is not (nameof(AgencyRole.Owner) or nameof(AgencyRole.Admin) or nameof(AgencyRole.Analyst)))
            throw new ForbiddenException("Only agency administrators and assigned editors can modify dashboards.");
    }

    private void RequireAdministrator()
    {
        if (currentTenant.Role is not (nameof(AgencyRole.Owner) or nameof(AgencyRole.Admin)))
            throw new ForbiddenException("Only Owner and Admin can create agency templates.");
    }

    private static void EnsureVersion(Dashboard dashboard, Guid expectedVersion)
    {
        if (expectedVersion == Guid.Empty) throw new ArgumentException("Expected version is required.");
        if (dashboard.Version != expectedVersion)
            throw new ConflictException("The dashboard changed since it was loaded. Refresh and try again.");
    }

    private static string? NormalizeSearch(string? search)
    {
        if (string.IsNullOrWhiteSpace(search)) return null;
        var normalized = search.Trim();
        if (normalized.Length > Dashboard.MaxTitleLength)
            throw new ArgumentException($"Search cannot exceed {Dashboard.MaxTitleLength} characters.");
        return normalized;
    }

    private static DashboardListItemModel MapList(DashboardRecord value)
    {
        var dashboard = value.Dashboard;
        return new DashboardListItemModel(dashboard.Id, dashboard.ClientId, dashboard.FolderId, dashboard.Title,
            dashboard.Description, dashboard.IsArchived, value.DraftRevision, dashboard.CurrentPublicationNumber,
            dashboard.CreatedAtUtc, Later(dashboard.UpdatedAtUtc, value.DraftUpdatedAtUtc), dashboard.Version);
    }

    private static DashboardModel Map(Dashboard dashboard, int draftRevision, DateTimeOffset draftUpdatedAtUtc) =>
        new(dashboard.Id, dashboard.ClientId, dashboard.FolderId, dashboard.Title, dashboard.Description,
            dashboard.IsArchived, draftRevision, dashboard.CurrentPublicationNumber, dashboard.CreatedAtUtc,
            Later(dashboard.UpdatedAtUtc, draftUpdatedAtUtc), dashboard.Version);

    private static DashboardDraftModel Map(DashboardDraft draft) => new(draft.DashboardId, draft.SchemaVersion,
        draft.Revision, ParseJson(draft.DefinitionJson), draft.UpdatedByUserId, draft.UpdatedAtUtc);

    private static PublishedDashboardModel Map(DashboardVersion version) => new(version.Id, version.DashboardId,
        version.PublicationNumber, version.DraftRevision, version.SchemaVersion, ParseJson(version.DefinitionJson),
        version.DefinitionHash, version.PublishedByUserId, version.PublishedAtUtc);

    private static DashboardTemplateModel Map(DashboardTemplate template) => new(template.Id, template.ClientId,
        template.Name, template.Description, template.SchemaVersion, ParseJson(template.DefinitionJson),
        template.CreatedAtUtc, template.Version);

    private static JsonElement ParseJson(string json)
    {
        using var document = JsonDocument.Parse(json);
        return document.RootElement.Clone();
    }

    private static DateTimeOffset Later(DateTimeOffset first, DateTimeOffset second) => first >= second ? first : second;
}
