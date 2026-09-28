using AnaliticAsd.Application.Clients;
using AnaliticAsd.Contracts.Clients;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;

namespace AnaliticAsd.Controllers;

[ApiController]
[Authorize]
[Route("api/v1/clients")]
public sealed class ClientsController(IClientService clientService) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<ClientResponse>>> List(CancellationToken cancellationToken) =>
        Ok((await clientService.ListAsync(cancellationToken)).Select(Map).ToArray());

    [HttpGet("{clientId:guid}")]
    public async Task<ActionResult<ClientResponse>> GetById(
        Guid clientId,
        CancellationToken cancellationToken) =>
        Ok(Map(await clientService.GetByIdAsync(clientId, cancellationToken)));

    [HttpPost]
    [Authorize(Roles = "Owner,Admin")]
    public async Task<ActionResult<ClientResponse>> Create(
        CreateClientRequest request,
        CancellationToken cancellationToken)
    {
        var response = Map(await clientService.CreateAsync(
            new CreateClientCommand(request.Name),
            cancellationToken));
        return CreatedAtAction(nameof(GetById), new { clientId = response.Id }, response);
    }

    [HttpPut("{clientId:guid}")]
    [Authorize(Roles = "Owner,Admin")]
    public async Task<ActionResult<ClientResponse>> Update(
        Guid clientId,
        UpdateClientRequest request,
        CancellationToken cancellationToken) =>
        Ok(Map(await clientService.UpdateAsync(
            clientId,
            new UpdateClientCommand(request.Name, request.IsActive),
            cancellationToken)));

    [HttpDelete("{clientId:guid}")]
    [Authorize(Roles = "Owner,Admin")]
    public async Task<IActionResult> Delete(Guid clientId, CancellationToken cancellationToken)
    {
        await clientService.DeleteAsync(clientId, cancellationToken);
        return NoContent();
    }

    private static ClientResponse Map(ClientModel client) =>
        new(client.Id, client.Name, client.IsActive, client.CreatedAtUtc, client.UpdatedAtUtc);
}
