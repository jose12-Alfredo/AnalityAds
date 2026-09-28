using AnaliticAsd.Application.Identity;
using AnaliticAsd.Contracts.Identity;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AnaliticAsd.Controllers;

[ApiController]
[Authorize(Roles = "Owner,Admin")]
[Route("api/v1")]
public sealed class ClientEditorsController(IClientEditorService service) : ControllerBase
{
    [HttpGet("agency/editors")]
    public async Task<ActionResult<IReadOnlyList<AgencyEditorModel>>> Eligible(CancellationToken cancellationToken) =>
        Ok(await service.ListEligibleAsync(cancellationToken));

    [HttpGet("clients/{clientId:guid}/editors")]
    public async Task<ActionResult<IReadOnlyList<ClientEditorModel>>> Assigned(Guid clientId,
        CancellationToken cancellationToken) =>
        Ok(await service.ListAssignedAsync(clientId, cancellationToken));

    [HttpPost("clients/{clientId:guid}/editors")]
    public async Task<ActionResult<ClientEditorModel>> Assign(Guid clientId, AssignClientEditorRequest request,
        CancellationToken cancellationToken)
    {
        if (request.UserId == Guid.Empty) throw new ArgumentException("Editor user id is required.");
        var assignment = await service.AssignAsync(clientId, request.UserId, cancellationToken);
        return Created($"/api/v1/clients/{clientId}/editors", assignment);
    }

    [HttpDelete("clients/{clientId:guid}/editors/{userId:guid}")]
    public async Task<IActionResult> Remove(Guid clientId, Guid userId, [FromQuery] Guid expectedVersion,
        CancellationToken cancellationToken)
    {
        await service.RemoveAsync(clientId, userId, expectedVersion, cancellationToken);
        return NoContent();
    }
}
