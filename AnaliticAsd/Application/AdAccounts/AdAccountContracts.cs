using AnaliticAsd.Domain.Advertising;

namespace AnaliticAsd.Application.AdAccounts;

public sealed record CreateAdAccountCommand(
    string MetaAccountId,
    string Name,
    string Currency,
    string TimeZone);

public sealed record UpdateAdAccountCommand(
    string Name,
    string Currency,
    string TimeZone,
    bool IsActive);

public sealed record AdAccountModel(
    Guid Id,
    Guid ClientId,
    string MetaAccountId,
    string Name,
    string Currency,
    string TimeZone,
    AdAccountConnectionStatus ConnectionStatus,
    bool IsActive,
    DateTimeOffset CreatedAtUtc,
    DateTimeOffset UpdatedAtUtc);

public interface IAdAccountRepository
{
    Task<IReadOnlyList<AdAccount>> ListByClientAsync(
        Guid clientId,
        CancellationToken cancellationToken = default);
    Task<AdAccount?> GetByIdAsync(
        Guid id,
        bool trackChanges,
        CancellationToken cancellationToken = default);
    Task<bool> MetaAccountIdExistsAsync(
        MetaAdAccountId metaAccountId,
        CancellationToken cancellationToken = default);
    Task<AdAccount?> GetByMetaAccountIdAsync(MetaAdAccountId metaAccountId, CancellationToken cancellationToken = default);
    Task<bool> AnyForClientAsync(Guid clientId, CancellationToken cancellationToken = default);
    void Add(AdAccount adAccount);
    void Remove(AdAccount adAccount);
}

public interface IAdAccountService
{
    Task<IReadOnlyList<AdAccountModel>> ListByClientAsync(
        Guid clientId,
        CancellationToken cancellationToken = default);
    Task<AdAccountModel> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<AdAccountModel> CreateAsync(
        Guid clientId,
        CreateAdAccountCommand command,
        CancellationToken cancellationToken = default);
    Task<AdAccountModel> UpdateAsync(
        Guid id,
        UpdateAdAccountCommand command,
        CancellationToken cancellationToken = default);
    Task DeleteAsync(Guid id, CancellationToken cancellationToken = default);
}
