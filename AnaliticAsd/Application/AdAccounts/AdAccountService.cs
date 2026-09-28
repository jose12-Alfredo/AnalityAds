using AnaliticAsd.Application.Abstractions;
using AnaliticAsd.Application.Clients;
using AnaliticAsd.Application.Common;
using AnaliticAsd.Domain.Advertising;

namespace AnaliticAsd.Application.AdAccounts;

public sealed class AdAccountService(
    IAdAccountRepository adAccountRepository,
    IClientRepository clientRepository,
    IUnitOfWork unitOfWork,
    TimeProvider timeProvider) : IAdAccountService
{
    public async Task<IReadOnlyList<AdAccountModel>> ListByClientAsync(
        Guid clientId,
        CancellationToken cancellationToken = default)
    {
        await EnsureClientExistsAsync(clientId, cancellationToken);
        return (await adAccountRepository.ListByClientAsync(clientId, cancellationToken))
            .Select(Map)
            .ToArray();
    }

    public async Task<AdAccountModel> GetByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default) =>
        Map(await GetRequiredAsync(id, false, cancellationToken));

    public async Task<AdAccountModel> CreateAsync(
        Guid clientId,
        CreateAdAccountCommand command,
        CancellationToken cancellationToken = default)
    {
        var client = await GetClientAsync(clientId, cancellationToken);
        var metaAccountId = MetaAdAccountId.Parse(command.MetaAccountId);
        if (await adAccountRepository.MetaAccountIdExistsAsync(metaAccountId, cancellationToken))
        {
            throw new ConflictException($"Meta ad account '{metaAccountId}' is already registered.");
        }

        var account = AdAccount.Create(
            client.AgencyId,
            clientId,
            metaAccountId,
            command.Name,
            CurrencyCode.Parse(command.Currency),
            MetaTimeZoneId.Parse(command.TimeZone),
            timeProvider.GetUtcNow());
        adAccountRepository.Add(account);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return Map(account);
    }

    public async Task<AdAccountModel> UpdateAsync(
        Guid id,
        UpdateAdAccountCommand command,
        CancellationToken cancellationToken = default)
    {
        var account = await GetRequiredAsync(id, true, cancellationToken);
        account.UpdateDetails(
            command.Name,
            CurrencyCode.Parse(command.Currency),
            MetaTimeZoneId.Parse(command.TimeZone),
            command.IsActive,
            timeProvider.GetUtcNow());
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return Map(account);
    }

    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var account = await GetRequiredAsync(id, true, cancellationToken);
        adAccountRepository.Remove(account);
        await unitOfWork.SaveChangesAsync(cancellationToken);
    }

    private async Task EnsureClientExistsAsync(Guid clientId, CancellationToken cancellationToken)
    {
        _ = await GetClientAsync(clientId, cancellationToken);
    }

    private async Task<AnaliticAsd.Domain.Clients.Client> GetClientAsync(Guid clientId, CancellationToken cancellationToken) =>
        await clientRepository.GetByIdAsync(clientId, false, cancellationToken)
        ?? throw new EntityNotFoundException($"Client '{clientId}' was not found.");

    private async Task<AdAccount> GetRequiredAsync(
        Guid id,
        bool trackChanges,
        CancellationToken cancellationToken) =>
        await adAccountRepository.GetByIdAsync(id, trackChanges, cancellationToken)
        ?? throw new EntityNotFoundException($"Ad account '{id}' was not found.");

    private static AdAccountModel Map(AdAccount account) =>
        new(
            account.Id,
            account.ClientId,
            account.MetaAccountId.Value,
            account.Name,
            account.Currency.Value,
            account.TimeZone.Value,
            account.ConnectionStatus,
            account.IsActive,
            account.CreatedAtUtc,
            account.UpdatedAtUtc);
}
