using AnaliticAsd.Domain.Identity;

namespace AnaliticAsd.Application.Identity;

public sealed record AgencyEditorModel(Guid UserId, string Email, bool IsActive);
public sealed record ClientEditorModel(Guid UserId, string Email, DateTimeOffset GrantedAtUtc, Guid Version);
public sealed record EditorMemberModel(Guid UserId, string Email, bool IsActive, AgencyRole Role);

public interface IClientEditorRepository
{
    Task<IReadOnlyList<AgencyEditorModel>> ListEligibleAsync(Guid agencyId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<ClientEditorModel>> ListAssignedAsync(Guid agencyId, Guid clientId,
        CancellationToken cancellationToken = default);
    Task<EditorMemberModel?> FindMemberAsync(Guid agencyId, Guid userId,
        CancellationToken cancellationToken = default);
    Task<ClientEditorAssignment?> FindAssignmentAsync(Guid agencyId, Guid clientId, Guid userId,
        bool trackChanges, CancellationToken cancellationToken = default);
    void Add(ClientEditorAssignment assignment);
    void Remove(ClientEditorAssignment assignment);
    Task SaveAsync(CancellationToken cancellationToken = default);
}

public interface IClientEditorService
{
    Task<IReadOnlyList<AgencyEditorModel>> ListEligibleAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<ClientEditorModel>> ListAssignedAsync(Guid clientId,
        CancellationToken cancellationToken = default);
    Task<ClientEditorModel> AssignAsync(Guid clientId, Guid userId,
        CancellationToken cancellationToken = default);
    Task RemoveAsync(Guid clientId, Guid userId, Guid expectedVersion,
        CancellationToken cancellationToken = default);
}
