using AnaliticAsd.Application.Folders;
using AnaliticAsd.Contracts.Folders;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AnaliticAsd.Controllers;

[ApiController]
[Authorize]
[Route("api/v1")]
public sealed class FoldersController(IFolderService service) : ControllerBase
{
    [HttpGet("clients/{clientId:guid}/folders")]
    [Authorize(Roles = "Owner,Admin,Analyst,Viewer")]
    public async Task<ActionResult<IReadOnlyList<FolderResponse>>> List(Guid clientId,
        [FromQuery] bool includeArchived = false, [FromQuery] string? search = null,
        CancellationToken cancellationToken = default) =>
        Ok((await service.ListAsync(clientId, includeArchived, search, cancellationToken)).Select(Map).ToArray());

    [HttpPost("clients/{clientId:guid}/folders")]
    [Authorize(Roles = "Owner,Admin,Analyst")]
    public async Task<ActionResult<FolderResponse>> Create(Guid clientId, CreateFolderRequest request,
        CancellationToken cancellationToken)
    {
        var response = Map(await service.CreateAsync(clientId,
            new CreateFolderCommand(request.Name, request.ParentFolderId), cancellationToken));
        return Created($"/api/v1/clients/{clientId}/folders", response);
    }

    [HttpPatch("folders/{folderId:guid}")]
    [Authorize(Roles = "Owner,Admin,Analyst")]
    public async Task<ActionResult<FolderResponse>> Update(Guid folderId, UpdateFolderRequest request,
        CancellationToken cancellationToken) =>
        Ok(Map(await service.UpdateAsync(folderId, new UpdateFolderCommand(request.Name, request.ExpectedVersion),
            cancellationToken)));

    [HttpPost("folders/{folderId:guid}/move")]
    [Authorize(Roles = "Owner,Admin,Analyst")]
    public async Task<ActionResult<FolderResponse>> Move(Guid folderId, MoveFolderRequest request,
        CancellationToken cancellationToken) =>
        Ok(Map(await service.MoveAsync(folderId,
            new MoveFolderCommand(request.ParentFolderId, request.SortOrder, request.ExpectedVersion),
            cancellationToken)));

    [HttpDelete("folders/{folderId:guid}")]
    [Authorize(Roles = "Owner,Admin,Analyst")]
    public async Task<IActionResult> Archive(Guid folderId, [FromQuery] Guid expectedVersion,
        CancellationToken cancellationToken)
    {
        await service.ArchiveAsync(folderId, expectedVersion, cancellationToken);
        return NoContent();
    }

    [HttpPost("folders/{folderId:guid}/restore")]
    [Authorize(Roles = "Owner,Admin,Analyst")]
    public async Task<ActionResult<FolderResponse>> Restore(Guid folderId, RestoreFolderRequest request,
        CancellationToken cancellationToken) =>
        Ok(Map(await service.RestoreAsync(folderId, request.ExpectedVersion, cancellationToken)));

    private static FolderResponse Map(FolderModel folder) => new(folder.Id, folder.ClientId,
        folder.ParentFolderId, folder.Name, folder.SortOrder, folder.IsArchived, folder.CreatedAtUtc,
        folder.UpdatedAtUtc, folder.Version);
}
