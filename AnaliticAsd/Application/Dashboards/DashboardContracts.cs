using System.Text.Json;
using AnaliticAsd.Domain.Dashboards;

namespace AnaliticAsd.Application.Dashboards;

public sealed record CreateDashboardCommand(string Title, string? Description, Guid? FolderId,
    Guid? TemplateId, IReadOnlyDictionary<string, Guid> SourceBindings);
public sealed record UpdateDashboardCommand(string Title, string? Description, Guid ExpectedVersion);
public sealed record MoveDashboardCommand(Guid? FolderId, Guid ExpectedVersion);
public sealed record DuplicateDashboardCommand(Guid DestinationClientId, Guid? FolderId, string? Title);
public sealed record SaveDashboardDraftCommand(int ExpectedRevision, string DefinitionJson);
public sealed record PublishDashboardCommand(int ExpectedRevision);
public sealed record CreateDashboardTemplateCommand(string Name, string? Description, string DefinitionJson);

public sealed record DashboardListItemModel(Guid Id, Guid ClientId, Guid? FolderId, string Title,
    string Description, bool IsArchived, int DraftRevision, int? CurrentPublicationNumber,
    DateTimeOffset CreatedAtUtc, DateTimeOffset UpdatedAtUtc, Guid Version);
public sealed record DashboardModel(Guid Id, Guid ClientId, Guid? FolderId, string Title, string Description,
    bool IsArchived, int DraftRevision, int? CurrentPublicationNumber, DateTimeOffset CreatedAtUtc,
    DateTimeOffset UpdatedAtUtc, Guid Version);
public sealed record DashboardDraftModel(Guid DashboardId, int SchemaVersion, int Revision,
    JsonElement Definition, Guid UpdatedByUserId, DateTimeOffset UpdatedAtUtc);
public sealed record PublishedDashboardModel(Guid VersionId, Guid DashboardId, int PublicationNumber,
    int DraftRevision, int SchemaVersion, JsonElement Definition, string DefinitionHash,
    Guid PublishedByUserId, DateTimeOffset PublishedAtUtc);
public sealed record DashboardTemplateModel(Guid Id, Guid? ClientId, string Name, string Description,
    int SchemaVersion, JsonElement Definition, DateTimeOffset CreatedAtUtc, Guid Version);
public sealed record DashboardRecord(Dashboard Dashboard, int DraftRevision, DateTimeOffset DraftUpdatedAtUtc);

public interface IDashboardRepository
{
    Task<IReadOnlyList<DashboardRecord>> ListAsync(Guid clientId, Guid? folderId, bool includeArchived,
        string? search, CancellationToken cancellationToken = default);
    Task<Dashboard?> GetAsync(Guid dashboardId, bool trackChanges,
        CancellationToken cancellationToken = default);
    Task<DashboardDraft?> GetDraftAsync(Guid dashboardId, bool trackChanges,
        CancellationToken cancellationToken = default);
    Task<DashboardVersion?> GetPublishedAsync(Guid dashboardId, int publicationNumber,
        CancellationToken cancellationToken = default);
    Task<IReadOnlyList<DashboardTemplate>> ListTemplatesAsync(Guid clientId,
        CancellationToken cancellationToken = default);
    Task<DashboardTemplate?> GetTemplateAsync(Guid templateId, Guid clientId,
        CancellationToken cancellationToken = default);
    void Add(Dashboard dashboard);
    void Add(DashboardDraft draft);
    void Add(DashboardVersion version);
    void Add(DashboardTemplate template);
    Task SaveAsync(CancellationToken cancellationToken = default);
}

public interface IDashboardService
{
    Task<IReadOnlyList<DashboardListItemModel>> ListAsync(Guid clientId, Guid? folderId,
        bool includeArchived, string? search, CancellationToken cancellationToken = default);
    Task<DashboardModel> GetAsync(Guid dashboardId, CancellationToken cancellationToken = default);
    Task<DashboardModel> CreateAsync(Guid clientId, CreateDashboardCommand command,
        CancellationToken cancellationToken = default);
    Task<DashboardModel> UpdateAsync(Guid dashboardId, UpdateDashboardCommand command,
        CancellationToken cancellationToken = default);
    Task<DashboardModel> MoveAsync(Guid dashboardId, MoveDashboardCommand command,
        CancellationToken cancellationToken = default);
    Task<DashboardModel> DuplicateAsync(Guid dashboardId, DuplicateDashboardCommand command,
        CancellationToken cancellationToken = default);
    Task ArchiveAsync(Guid dashboardId, Guid expectedVersion, CancellationToken cancellationToken = default);
    Task<DashboardModel> RestoreAsync(Guid dashboardId, Guid expectedVersion,
        CancellationToken cancellationToken = default);
    Task<DashboardDraftModel> GetDraftAsync(Guid dashboardId, CancellationToken cancellationToken = default);
    Task<DashboardDraftModel> SaveDraftAsync(Guid dashboardId, SaveDashboardDraftCommand command,
        CancellationToken cancellationToken = default);
    Task<PublishedDashboardModel> PublishAsync(Guid dashboardId, PublishDashboardCommand command,
        CancellationToken cancellationToken = default);
    Task<PublishedDashboardModel> GetPublishedAsync(Guid dashboardId,
        CancellationToken cancellationToken = default);
    Task<IReadOnlyList<DashboardTemplateModel>> ListTemplatesAsync(Guid clientId,
        CancellationToken cancellationToken = default);
    Task<DashboardTemplateModel> CreateClientTemplateAsync(Guid clientId,
        CreateDashboardTemplateCommand command, CancellationToken cancellationToken = default);
    Task<DashboardTemplateModel> CreateAgencyTemplateAsync(CreateDashboardTemplateCommand command,
        CancellationToken cancellationToken = default);
}
