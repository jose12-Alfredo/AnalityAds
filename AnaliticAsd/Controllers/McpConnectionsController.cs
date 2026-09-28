using AnaliticAsd.Application.Identity;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AnaliticAsd.Controllers;

[ApiController, Authorize, Route("api/v1/mcp/connections")]
public sealed class McpConnectionsController(IMcpConnectionService service) : ControllerBase
{
    [HttpGet] public async Task<ActionResult<IReadOnlyList<McpConnectionModel>>> List(CancellationToken ct) => Ok(await service.ListAsync(ct));
    [HttpPost]
    public async Task<ActionResult<CreatedMcpConnectionModel>> Create(CreateMcpConnectionRequest request, CancellationToken ct)
    {
        if (!request.ConsentAccepted) return BadRequest(new ProblemDetails { Status = 400, Title = "Explicit consent is required", Detail = "The user must explicitly consent to the read-only MCP connection." });
        var result = await service.CreateAsync(request.Name, ct); return Created($"/api/v1/mcp/connections/{result.Connection.Id}", result);
    }
    [HttpDelete("{id:guid}")] public async Task<IActionResult> Revoke(Guid id, CancellationToken ct) { await service.RevokeAsync(id, ct); return NoContent(); }
}
public sealed record CreateMcpConnectionRequest(string Name, bool ConsentAccepted);
