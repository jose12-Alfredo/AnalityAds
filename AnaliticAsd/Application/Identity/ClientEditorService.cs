using AnaliticAsd.Application.Clients;
using AnaliticAsd.Application.Common;
using AnaliticAsd.Domain.Identity;

namespace AnaliticAsd.Application.Identity;

public sealed class ClientEditorService(IClientEditorRepository repository, IClientRepository clientRepository,
    ICurrentTenant currentTenant, TimeProvider timeProvider) : IClientEditorService
{
    public async Task<IReadOnlyList<AgencyEditorModel>> ListEligibleAsync(
        CancellationToken cancellationToken = default)
    {
        RequireAdministrator();
        return await repository.ListEligibleAsync(currentTenant.AgencyId, cancellationToken);
    }

    public async Task<IReadOnlyList<ClientEditorModel>> ListAssignedAsync(Guid clientId,
        CancellationToken cancellationToken = default)
    {
        RequireAdministrator();
        await RequireClientAsync(clientId, cancellationToken);
        return await repository.ListAssignedAsync(currentTenant.AgencyId, clientId, cancellationToken);
    }

    public async Task<ClientEditorModel> AssignAsync(Guid clientId, Guid userId,
        CancellationToken cancellationToken = default)
    {
        RequireAdministrator();
        await RequireClientAsync(clientId, cancellationToken);
        var member = await repository.FindMemberAsync(currentTenant.AgencyId, userId, cancellationToken)
            ?? throw new EntityNotFoundException("The agency member was not found.");
        if (!member.IsActive) throw new ConflictException("The editor account is inactive.");
        if (member.Role != AgencyRole.Analyst)
            throw new ConflictException("Only agency members with the Analyst role can be assigned as editors.");
        if (await repository.FindAssignmentAsync(currentTenant.AgencyId, clientId, userId, false,
                cancellationToken) is not null)
            throw new ConflictException("The editor is already assigned to this client.");

        var assignment = ClientEditorAssignment.Create(currentTenant.AgencyId, clientId, userId,
            currentTenant.UserId, timeProvider.GetUtcNow());
        repository.Add(assignment);
        await repository.SaveAsync(cancellationToken);
        return new ClientEditorModel(userId, member.Email, assignment.GrantedAtUtc, assignment.Version);
    }

    public async Task RemoveAsync(Guid clientId, Guid userId, Guid expectedVersion,
        CancellationToken cancellationToken = default)
    {
        RequireAdministrator();
        await RequireClientAsync(clientId, cancellationToken);
        if (expectedVersion == Guid.Empty) throw new ArgumentException("Expected version is required.");
        var assignment = await repository.FindAssignmentAsync(currentTenant.AgencyId, clientId, userId, true,
                cancellationToken)
            ?? throw new EntityNotFoundException("The editor assignment was not found.");
        if (assignment.Version != expectedVersion)
            throw new ConflictException("The editor assignment changed since it was loaded. Refresh and try again.");
        repository.Remove(assignment);
        await repository.SaveAsync(cancellationToken);
    }

    private async Task RequireClientAsync(Guid clientId, CancellationToken cancellationToken)
    {
        _ = await clientRepository.GetByIdAsync(clientId, false, cancellationToken)
            ?? throw new EntityNotFoundException("Client was not found.");
    }

    private void RequireAdministrator()
    {
        if (currentTenant.Role is not (nameof(AgencyRole.Owner) or nameof(AgencyRole.Admin)))
            throw new ForbiddenException("Only Owner and Admin can manage editor assignments.");
    }
}
