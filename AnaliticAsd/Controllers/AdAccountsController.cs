using AnaliticAsd.Application.AdAccounts;
using AnaliticAsd.Contracts.AdAccounts;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;

namespace AnaliticAsd.Controllers;

[ApiController]
[Authorize]
[Route("api/v1/ad-accounts")]
public sealed class AdAccountsController(IAdAccountService adAccountService) : ControllerBase
{
    [HttpGet("/api/v1/clients/{clientId:guid}/ad-accounts")]
    public async Task<ActionResult<IReadOnlyList<AdAccountResponse>>> ListByClient(
        Guid clientId,
        CancellationToken cancellationToken) =>
        Ok((await adAccountService.ListByClientAsync(clientId, cancellationToken)).Select(Map).ToArray());

    [HttpGet("{adAccountId:guid}")]
    public async Task<ActionResult<AdAccountResponse>> GetById(
        Guid adAccountId,
        CancellationToken cancellationToken) =>
        Ok(Map(await adAccountService.GetByIdAsync(adAccountId, cancellationToken)));

    [HttpPost("/api/v1/clients/{clientId:guid}/ad-accounts")]
    [Authorize(Roles = "Owner,Admin,Analyst")]
    public async Task<ActionResult<AdAccountResponse>> Create(
        Guid clientId,
        CreateAdAccountRequest request,
        CancellationToken cancellationToken)
    {
        var response = Map(await adAccountService.CreateAsync(
            clientId,
            new CreateAdAccountCommand(
                request.MetaAccountId,
                request.Name,
                request.Currency,
                request.TimeZone),
            cancellationToken));
        return CreatedAtAction(nameof(GetById), new { adAccountId = response.Id }, response);
    }

    [HttpPut("{adAccountId:guid}")]
    [Authorize(Roles = "Owner,Admin,Analyst")]
    public async Task<ActionResult<AdAccountResponse>> Update(
        Guid adAccountId,
        UpdateAdAccountRequest request,
        CancellationToken cancellationToken) =>
        Ok(Map(await adAccountService.UpdateAsync(
            adAccountId,
            new UpdateAdAccountCommand(
                request.Name,
                request.Currency,
                request.TimeZone,
                request.IsActive),
            cancellationToken)));

    [HttpDelete("{adAccountId:guid}")]
    [Authorize(Roles = "Owner,Admin")]
    public async Task<IActionResult> Delete(Guid adAccountId, CancellationToken cancellationToken)
    {
        await adAccountService.DeleteAsync(adAccountId, cancellationToken);
        return NoContent();
    }

    private static AdAccountResponse Map(AdAccountModel account) =>
        new(
            account.Id,
            account.ClientId,
            account.MetaAccountId,
            account.Name,
            account.Currency,
            account.TimeZone,
            account.ConnectionStatus.ToString(),
            account.IsActive,
            account.CreatedAtUtc,
            account.UpdatedAtUtc);
}
