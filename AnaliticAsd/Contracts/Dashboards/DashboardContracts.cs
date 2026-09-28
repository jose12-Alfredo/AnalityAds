using System.ComponentModel.DataAnnotations;
using System.Text.Json;
using AnaliticAsd.Domain.Dashboards;

namespace AnaliticAsd.Contracts.Dashboards;

public sealed class CreateDashboardRequest
{
    [Required, StringLength(Dashboard.MaxTitleLength)]
    public string Title { get; init; } = string.Empty;

    [StringLength(Dashboard.MaxDescriptionLength)]
    public string? Description { get; init; }

    public Guid? FolderId { get; init; }
    public Guid? TemplateId { get; init; }
    public Dictionary<string, Guid>? SourceBindings { get; init; }
}

public sealed class UpdateDashboardRequest
{
    [Required, StringLength(Dashboard.MaxTitleLength)]
    public string Title { get; init; } = string.Empty;

    [StringLength(Dashboard.MaxDescriptionLength)]
    public string? Description { get; init; }

    public Guid ExpectedVersion { get; init; }
}

public sealed class MoveDashboardRequest
{
    public Guid? FolderId { get; init; }
    public Guid ExpectedVersion { get; init; }
}

public sealed class DuplicateDashboardRequest
{
    public Guid DestinationClientId { get; init; }
    public Guid? FolderId { get; init; }

    [StringLength(Dashboard.MaxTitleLength)]
    public string? Title { get; init; }
}

public sealed class RestoreDashboardRequest
{
    public Guid ExpectedVersion { get; init; }
}

public sealed class SaveDashboardDraftRequest
{
    [Range(1, int.MaxValue)]
    public int ExpectedRevision { get; init; }

    public JsonElement Definition { get; init; }
}

public sealed class PublishDashboardRequest
{
    [Range(1, int.MaxValue)]
    public int ExpectedRevision { get; init; }
}

public sealed class CreateDashboardTemplateRequest
{
    [Required, StringLength(DashboardTemplate.MaxNameLength)]
    public string Name { get; init; } = string.Empty;

    [StringLength(DashboardTemplate.MaxDescriptionLength)]
    public string? Description { get; init; }

    public JsonElement Definition { get; init; }
}
