using AnaliticAsd.Domain.Clients;
using AnaliticAsd.Domain.Identity;

namespace AnaliticAsd.Application.Identity;

public sealed record ClientAccessModel(Guid UserId, string Email, string Role, DateTimeOffset GrantedAtUtc);
public sealed record ClientInvitationModel(Guid Id, Guid ClientId, string Email, string Status, DateTimeOffset CreatedAtUtc, DateTimeOffset ExpiresAtUtc);
public sealed record CreatedClientInvitationModel(ClientInvitationModel Invitation, string InvitationToken);
public sealed record AcceptClientInvitationCommand(string InvitationToken, string Email, string Password);

public interface IClientAccessService
{
    Task<IReadOnlyList<ClientAccessModel>> ListAccessAsync(Guid clientId, CancellationToken ct = default);
    Task<IReadOnlyList<ClientInvitationModel>> ListInvitationsAsync(Guid clientId, CancellationToken ct = default);
    Task<CreatedClientInvitationModel> InviteAsync(Guid clientId, string email, CancellationToken ct = default);
    Task RevokeInvitationAsync(Guid clientId, Guid invitationId, CancellationToken ct = default);
    Task RevokeAccessAsync(Guid clientId, Guid userId, CancellationToken ct = default);
    Task<AuthModel> AcceptAsync(AcceptClientInvitationCommand command, CancellationToken ct = default);
}

public interface IClientAccessRepository
{
    Task<Client?> FindClientAsync(Guid agencyId, Guid clientId, CancellationToken ct);
    Task<Agency?> FindAgencyAsync(Guid agencyId, CancellationToken ct);
    Task<Membership?> FindMembershipAsync(Guid agencyId, Guid userId, CancellationToken ct);
    Task<ClientAccess?> FindAccessAsync(Guid agencyId, Guid clientId, Guid userId, CancellationToken ct);
    Task<IReadOnlyList<ClientAccessModel>> ListAccessAsync(Guid agencyId, Guid clientId, CancellationToken ct);
    Task<IReadOnlyList<ClientInvitation>> ListInvitationsAsync(Guid agencyId, Guid clientId, CancellationToken ct);
    Task<ClientInvitation?> FindInvitationAsync(Guid agencyId, Guid clientId, Guid invitationId, CancellationToken ct);
    Task<ClientInvitation?> FindInvitationByHashAsync(string tokenHash, CancellationToken ct);
    void Add(ClientInvitation invitation);
    void Add(ClientAccess access);
    void Remove(ClientAccess access);
    Task SaveAsync(CancellationToken ct);
}
