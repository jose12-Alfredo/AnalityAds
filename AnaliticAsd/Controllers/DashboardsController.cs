using System.Text.Json;
using AnaliticAsd.Application.Dashboards;
using AnaliticAsd.Contracts.Dashboards;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AnaliticAsd.Controllers;

[ApiController]
[Authorize]
[Route("api/v1")]
public sealed class DashboardsController(IDashboardService service) : ControllerBase
{
    [HttpGet("clients/{clientId:guid}/dashboards")]
    [Authorize(Roles = "Owner,Admin,Analyst,Viewer")]
    public async Task<ActionResult<IReadOnlyList<DashboardListItemModel>>> List(Guid clientId,
        [FromQuery] Guid? folderId = null, [FromQuery] bool includeArchived = false,
        [FromQuery] string? search = null, CancellationToken cancellationToken = default) =>
        Ok(await service.ListAsync(clientId, folderId, includeArchived, search, cancellationToken));

    [HttpPost("clients/{clientId:guid}/dashboards")]
    [Authorize(Roles = "Owner,Admin,Analyst")]
    public async Task<ActionResult<DashboardModel>> Create(Guid clientId, CreateDashboardRequest request,
        CancellationToken cancellationToken)
    {
        var dashboard = await service.CreateAsync(clientId,
            new CreateDashboardCommand(request.Title, request.Description, request.FolderId, request.TemplateId,
                request.SourceBindings ?? new Dictionary<string, Guid>()), cancellationToken);
        return CreatedAtAction(nameof(Get), new { dashboardId = dashboard.Id }, dashboard);
    }

    [HttpGet("dashboards/{dashboardId:guid}")]
    [Authorize(Roles = "Owner,Admin,Analyst,Viewer")]
    public async Task<ActionResult<DashboardModel>> Get(Guid dashboardId,
        CancellationToken cancellationToken) => Ok(await service.GetAsync(dashboardId, cancellationToken));

    [HttpPatch("dashboards/{dashboardId:guid}")]
    [Authorize(Roles = "Owner,Admin,Analyst")]
    public async Task<ActionResult<DashboardModel>> Update(Guid dashboardId, UpdateDashboardRequest request,
        CancellationToken cancellationToken) =>
        Ok(await service.UpdateAsync(dashboardId,
            new UpdateDashboardCommand(request.Title, request.Description, request.ExpectedVersion),
            cancellationToken));

    [HttpPost("dashboards/{dashboardId:guid}/move")]
    [Authorize(Roles = "Owner,Admin,Analyst")]
    public async Task<ActionResult<DashboardModel>> Move(Guid dashboardId, MoveDashboardRequest request,
        CancellationToken cancellationToken) =>
        Ok(await service.MoveAsync(dashboardId,
            new MoveDashboardCommand(request.FolderId, request.ExpectedVersion), cancellationToken));

    [HttpPost("dashboards/{dashboardId:guid}/duplicate")]
    [Authorize(Roles = "Owner,Admin,Analyst")]
    public async Task<ActionResult<DashboardModel>> Duplicate(Guid dashboardId,
        DuplicateDashboardRequest request, CancellationToken cancellationToken)
    {
        var dashboard = await service.DuplicateAsync(dashboardId,
            new DuplicateDashboardCommand(request.DestinationClientId, request.FolderId, request.Title),
            cancellationToken);
        return CreatedAtAction(nameof(Get), new { dashboardId = dashboard.Id }, dashboard);
    }

    [HttpDelete("dashboards/{dashboardId:guid}")]
    [Authorize(Roles = "Owner,Admin,Analyst")]
    public async Task<IActionResult> Archive(Guid dashboardId, [FromQuery] Guid expectedVersion,
        CancellationToken cancellationToken)
    {
        await service.ArchiveAsync(dashboardId, expectedVersion, cancellationToken);
        return NoContent();
    }

    [HttpPost("dashboards/{dashboardId:guid}/restore")]
    [Authorize(Roles = "Owner,Admin,Analyst")]
    public async Task<ActionResult<DashboardModel>> Restore(Guid dashboardId,
        RestoreDashboardRequest request, CancellationToken cancellationToken) =>
        Ok(await service.RestoreAsync(dashboardId, request.ExpectedVersion, cancellationToken));

    [HttpGet("dashboards/{dashboardId:guid}/draft")]
    [Authorize(Roles = "Owner,Admin,Analyst")]
    public async Task<ActionResult<DashboardDraftModel>> GetDraft(Guid dashboardId,
        CancellationToken cancellationToken) => Ok(await service.GetDraftAsync(dashboardId, cancellationToken));

    [HttpPut("dashboards/{dashboardId:guid}/draft")]
    [Authorize(Roles = "Owner,Admin,Analyst")]
    public async Task<ActionResult<DashboardDraftModel>> SaveDraft(Guid dashboardId,
        SaveDashboardDraftRequest request, CancellationToken cancellationToken) =>
        Ok(await service.SaveDraftAsync(dashboardId,
            new SaveDashboardDraftCommand(request.ExpectedRevision, DefinitionJson(request.Definition)),
            cancellationToken));

    [HttpPost("dashboards/{dashboardId:guid}/publish")]
    [Authorize(Roles = "Owner,Admin,Analyst")]
    public async Task<ActionResult<PublishedDashboardModel>> Publish(Guid dashboardId,
        PublishDashboardRequest request, CancellationToken cancellationToken) =>
        Ok(await service.PublishAsync(dashboardId, new PublishDashboardCommand(request.ExpectedRevision),
            cancellationToken));

    [HttpGet("dashboards/{dashboardId:guid}/published")]
    [Authorize(Roles = "Owner,Admin,Analyst,Viewer")]
    public async Task<ActionResult<PublishedDashboardModel>> GetPublished(Guid dashboardId,
        CancellationToken cancellationToken) =>
        Ok(await service.GetPublishedAsync(dashboardId, cancellationToken));

    [HttpGet("clients/{clientId:guid}/dashboard-templates")]
    [Authorize(Roles = "Owner,Admin,Analyst,Viewer")]
    public async Task<ActionResult<IReadOnlyList<DashboardTemplateModel>>> ListTemplates(Guid clientId,
        CancellationToken cancellationToken) =>
        Ok(await service.ListTemplatesAsync(clientId, cancellationToken));

    [HttpPost("clients/{clientId:guid}/dashboard-templates")]
    [Authorize(Roles = "Owner,Admin,Analyst")]
    public async Task<ActionResult<DashboardTemplateModel>> CreateClientTemplate(Guid clientId,
        CreateDashboardTemplateRequest request, CancellationToken cancellationToken)
    {
        var template = await service.CreateClientTemplateAsync(clientId,
            new CreateDashboardTemplateCommand(request.Name, request.Description,
                DefinitionJson(request.Definition)), cancellationToken);
        return Created($"/api/v1/clients/{clientId}/dashboard-templates", template);
    }

    [HttpPost("dashboard-templates")]
    [Authorize(Roles = "Owner,Admin")]
    public async Task<ActionResult<DashboardTemplateModel>> CreateAgencyTemplate(
        CreateDashboardTemplateRequest request, CancellationToken cancellationToken)
    {
        var template = await service.CreateAgencyTemplateAsync(
            new CreateDashboardTemplateCommand(request.Name, request.Description,
                DefinitionJson(request.Definition)), cancellationToken);
        return Created("/api/v1/dashboard-templates", template);
    }

    private static string DefinitionJson(JsonElement definition)
    {
        if (definition.ValueKind != JsonValueKind.Object)
            throw new ArgumentException("Dashboard definition must be a JSON object.", nameof(definition));
        return definition.GetRawText();
    }
}
