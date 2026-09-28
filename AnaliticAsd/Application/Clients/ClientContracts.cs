using AnaliticAsd.Domain.Clients;

namespace AnaliticAsd.Application.Clients;

public sealed record CreateClientCommand(string Name);
public sealed record UpdateClientCommand(string Name, bool IsActive);
public sealed record ClientModel(
    Guid Id,
    string Name,
    bool IsActive,
    DateTimeOffset CreatedAtUtc,
    DateTimeOffset UpdatedAtUtc);

public interface IClientRepository
{
    Task<IReadOnlyList<Client>> ListAsync(CancellationToken cancellationToken = default);
    Task<Client?> GetByIdAsync(Guid id, bool trackChanges, CancellationToken cancellationToken = default);
    void Add(Client client);
    void Remove(Client client);
}

public interface IClientService
{
    Task<IReadOnlyList<ClientModel>> ListAsync(CancellationToken cancellationToken = default);
    Task<ClientModel> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<ClientModel> CreateAsync(CreateClientCommand command, CancellationToken cancellationToken = default);
    Task<ClientModel> UpdateAsync(Guid id, UpdateClientCommand command, CancellationToken cancellationToken = default);
    Task DeleteAsync(Guid id, CancellationToken cancellationToken = default);
}
