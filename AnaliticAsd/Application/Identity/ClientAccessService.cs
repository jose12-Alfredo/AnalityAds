using System.Net.Mail;
using System.Security.Cryptography;
using System.Text;
using AnaliticAsd.Application.Common;
using AnaliticAsd.Domain.Clients;
using AnaliticAsd.Domain.Identity;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.WebUtilities;

namespace AnaliticAsd.Application.Identity;

public sealed class ClientAccessService(IClientAccessRepository repository, IIdentityRepository identity,
    ICurrentTenant tenant, IPasswordHasher<User> passwordHasher, ITokenIssuer tokenIssuer, TimeProvider clock) : IClientAccessService
{
    public async Task<IReadOnlyList<ClientAccessModel>> ListAccessAsync(Guid clientId, CancellationToken ct = default)
    {
        await RequireManagedClient(clientId, ct);
        return await repository.ListAccessAsync(tenant.AgencyId, clientId, ct);
    }

    public async Task<IReadOnlyList<ClientInvitationModel>> ListInvitationsAsync(Guid clientId, CancellationToken ct = default)
    {
        await RequireManagedClient(clientId, ct);
        return (await repository.ListInvitationsAsync(tenant.AgencyId, clientId, ct)).Select(Map).OrderByDescending(x => x.CreatedAtUtc).ToArray();
    }

    public async Task<CreatedClientInvitationModel> InviteAsync(Guid clientId, string email, CancellationToken ct = default)
    {
        var client = await RequireManagedClient(clientId, ct);
        if (!client.IsActive) throw new ConflictException("Activate the client before inviting users.");
        var normalizedEmail = ValidateEmail(email);
        var user = await identity.FindUserByEmailAsync(normalizedEmail, ct);
        if (user is not null)
        {
            await RequireExternalMembership(user, tenant.AgencyId, ct);
            if (await repository.FindAccessAsync(tenant.AgencyId, clientId, user.Id, ct) is not null)
                throw new ConflictException("The user already has access to this client.");
        }
        var token = WebEncoders.Base64UrlEncode(RandomNumberGenerator.GetBytes(32));
        var invitation = ClientInvitation.Create(tenant.AgencyId, clientId, tenant.UserId, email, Hash(token), clock.GetUtcNow());
        repository.Add(invitation);
        await repository.SaveAsync(ct);
        return new(Map(invitation), token);
    }

    public async Task RevokeInvitationAsync(Guid clientId, Guid invitationId, CancellationToken ct = default)
    {
        await RequireManagedClient(clientId, ct);
        var invitation = await repository.FindInvitationAsync(tenant.AgencyId, clientId, invitationId, ct)
            ?? throw new EntityNotFoundException("Invitation was not found.");
        if (invitation.AcceptedAtUtc is not null) throw new ConflictException("Invitation was accepted. Revoke the user's client access instead.");
        invitation.Revoke(clock.GetUtcNow());
        await repository.SaveAsync(ct);
    }

    public async Task RevokeAccessAsync(Guid clientId, Guid userId, CancellationToken ct = default)
    {
        await RequireManagedClient(clientId, ct);
        var access = await repository.FindAccessAsync(tenant.AgencyId, clientId, userId, ct)
            ?? throw new EntityNotFoundException("Client access was not found.");
        var row = (await repository.ListAccessAsync(tenant.AgencyId, clientId, ct)).Single(x => x.UserId == userId);
        foreach (var invitation in await repository.ListInvitationsAsync(tenant.AgencyId, clientId, ct))
            if (invitation.NormalizedEmail == User.NormalizeEmail(row.Email)) invitation.Revoke(clock.GetUtcNow());
        repository.Remove(access);
        await repository.SaveAsync(ct);
    }

    // The agency comes from a stored, one-use invitation, never an anonymous request parameter.
    public async Task<AuthModel> AcceptAsync(AcceptClientInvitationCommand command, CancellationToken ct = default)
    {
        var normalizedEmail = ValidateEmail(command.Email);
        if (string.IsNullOrWhiteSpace(command.InvitationToken) || command.InvitationToken.Length != 43)
            throw InvalidInvitation();
        if (string.IsNullOrWhiteSpace(command.Password) || command.Password.Length > 128)
            throw new ArgumentException("A password of at most 128 characters is required.");
        var now = clock.GetUtcNow();
        var invitation = await repository.FindInvitationByHashAsync(Hash(command.InvitationToken), ct);
        if (invitation is null || !invitation.IsAvailable(now) || invitation.NormalizedEmail != normalizedEmail)
            throw InvalidInvitation();
        var agency = await repository.FindAgencyAsync(invitation.AgencyId, ct);
        var client = await repository.FindClientAsync(invitation.AgencyId, invitation.ClientId, ct);
        if (agency is not { IsActive: true } || client is not { IsActive: true }) throw InvalidInvitation();

        var user = await identity.FindUserByEmailAsync(normalizedEmail, ct);
        Membership? membership = null;
        if (user is null)
        {
            if (command.Password.Length < 12 || !command.Password.Any(char.IsUpper) || !command.Password.Any(char.IsLower) || !command.Password.Any(char.IsDigit))
                throw new ArgumentException("Password must contain 12 to 128 characters, uppercase, lowercase and a number.");
            user = User.Create(invitation.Email, now);
            user.SetPasswordHash(passwordHasher.HashPassword(user, command.Password));
            identity.Add(user);
        }
        else
        {
            if (!user.IsActive || passwordHasher.VerifyHashedPassword(user, user.PasswordHash, command.Password) == PasswordVerificationResult.Failed)
                throw new UnauthorizedAccessException("Invalid email or password.");
            membership = await RequireExternalMembership(user, invitation.AgencyId, ct);
        }
        if (membership is null)
        {
            membership = Membership.Create(invitation.AgencyId, user.Id, AgencyRole.ClientViewer, now);
            identity.Add(membership);
        }
        if (await repository.FindAccessAsync(invitation.AgencyId, invitation.ClientId, user.Id, ct) is null)
            repository.Add(ClientAccess.Create(invitation.AgencyId, invitation.ClientId, user.Id, now));
        invitation.Accept(now);
        // A single SaveChanges transaction includes identity, grant and optimistic one-use consumption.
        await repository.SaveAsync(ct);
        var token = tokenIssuer.Issue(user, agency, membership);
        return new(token.Token, token.ExpiresAtUtc, user.Id, user.Email, agency.Id, agency.Name, membership.Role);
    }

    private async Task<Client> RequireManagedClient(Guid clientId, CancellationToken ct)
    {
        if (tenant.Role is not (nameof(AgencyRole.Owner) or nameof(AgencyRole.Admin)))
            throw new ForbiddenException("Only Owner and Admin can manage client access.");
        return await repository.FindClientAsync(tenant.AgencyId, clientId, ct) ?? throw new EntityNotFoundException("Client was not found.");
    }
    private async Task<Membership?> RequireExternalMembership(User user, Guid agencyId, CancellationToken ct)
    {
        var membership = await repository.FindMembershipAsync(agencyId, user.Id, ct);
        if (!user.IsActive || membership is not null && membership.Role != AgencyRole.ClientViewer)
            throw new ConflictException("This invitation cannot be used for an internal or inactive user.");
        return membership;
    }
    private ClientInvitationModel Map(ClientInvitation x) => new(x.Id, x.ClientId, x.Email, x.Status(clock.GetUtcNow()), x.CreatedAtUtc, x.ExpiresAtUtc);
    private static string ValidateEmail(string email)
    {
        var normalized = User.NormalizeEmail(email);
        if (!MailAddress.TryCreate(email.Trim(), out var address) || address.Address != email.Trim()) throw new ArgumentException("A valid email address is required.");
        return normalized;
    }
    private static string Hash(string token) => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(token)));
    private static ArgumentException InvalidInvitation() => new("The invitation is invalid, expired, revoked or already accepted.");
}
