using AnaliticAsd.Application.Abstractions;
using AnaliticAsd.Application.AdAccounts;
using AnaliticAsd.Application.Common;
using AnaliticAsd.Domain.Clients;
using AnaliticAsd.Domain.Identity;
using AnaliticAsd.Application.Identity;

namespace AnaliticAsd.Application.Clients;

public sealed class ClientService(
    IClientRepository clientRepository,
    IAdAccountRepository adAccountRepository,
    IUnitOfWork unitOfWork,
    TimeProvider timeProvider,
    ICurrentTenant currentTenant) : IClientService
{
    public async Task<IReadOnlyList<ClientModel>> ListAsync(
        CancellationToken cancellationToken = default) =>
        (await clientRepository.ListAsync(cancellationToken)).Select(Map).ToArray();

    public async Task<ClientModel> GetByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default) =>
        Map(await GetRequiredAsync(id, false, cancellationToken));

    public async Task<ClientModel> CreateAsync(
        CreateClientCommand command,
        CancellationToken cancellationToken = default)
    {
        RequireAdministrator();
        var client = Client.Create(currentTenant.AgencyId, command.Name, timeProvider.GetUtcNow());
        clientRepository.Add(client);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return Map(client);
    }

    public async Task<ClientModel> UpdateAsync(
        Guid id,
        UpdateClientCommand command,
        CancellationToken cancellationToken = default)
    {
        RequireAdministrator();
        var client = await GetRequiredAsync(id, true, cancellationToken);
        client.Update(command.Name, command.IsActive, timeProvider.GetUtcNow());
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return Map(client);
    }

    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        RequireAdministrator();
        var client = await GetRequiredAsync(id, true, cancellationToken);
        if (await adAccountRepository.AnyForClientAsync(id, cancellationToken))
        {
            throw new ConflictException(
                "The client cannot be deleted while it has associated ad accounts.");
        }

        clientRepository.Remove(client);
        await unitOfWork.SaveChangesAsync(cancellationToken);
    }

    private async Task<Client> GetRequiredAsync(
        Guid id,
        bool trackChanges,
        CancellationToken cancellationToken) =>
        await clientRepository.GetByIdAsync(id, trackChanges, cancellationToken)
        ?? throw new EntityNotFoundException($"Client '{id}' was not found.");

    private void RequireAdministrator()
    {
        if (currentTenant.Role is not (nameof(AgencyRole.Owner) or nameof(AgencyRole.Admin)))
            throw new ForbiddenException("Only Owner and Admin can manage clients.");
    }

    private static ClientModel Map(Client client) =>
        new(client.Id, client.Name, client.IsActive, client.CreatedAtUtc, client.UpdatedAtUtc);
}
