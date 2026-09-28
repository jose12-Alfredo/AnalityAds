using AnaliticAsd.Application.ProviderIntegrations;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AnaliticAsd.Controllers;
[ApiController]
[Route("api/v1/data-sources/{sourceId:guid}/sync")]
public sealed class ProviderSyncController(IProviderSyncService service) : ControllerBase
{
    [Authorize(Roles = "Owner,Admin,Analyst")]
    [HttpPost]
    public Task<ProviderSyncModel> Sync(Guid sourceId, [FromBody] SyncProviderSourceRequest request, CancellationToken ct) => service.SyncAsync(sourceId, request.Since, request.Until, ct);
}
public sealed record SyncProviderSourceRequest(DateOnly Since, DateOnly Until);
