using AnaliticAsd.Application.Common;
using AnaliticAsd.Application.Identity;
using AnaliticAsd.Domain.Clients;
using AnaliticAsd.Domain.Identity;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace AnaliticAsd.Infrastructure.Persistence.Repositories;

public sealed class ClientAccessRepository(AnalitiAdsDbContext db) : IClientAccessRepository
{
    public Task<Client?> FindClientAsync(Guid agencyId, Guid clientId, CancellationToken ct) => db.Clients.AsNoTracking().SingleOrDefaultAsync(x => x.Id == clientId && x.AgencyId == agencyId, ct);
    public Task<Agency?> FindAgencyAsync(Guid agencyId, CancellationToken ct) => db.Agencies.AsNoTracking().SingleOrDefaultAsync(x => x.Id == agencyId, ct);
    public Task<Membership?> FindMembershipAsync(Guid agencyId, Guid userId, CancellationToken ct) => db.Memberships.SingleOrDefaultAsync(x => x.AgencyId == agencyId && x.UserId == userId, ct);
    public Task<ClientAccess?> FindAccessAsync(Guid agencyId, Guid clientId, Guid userId, CancellationToken ct) => db.ClientAccesses.SingleOrDefaultAsync(x => x.AgencyId == agencyId && x.ClientId == clientId && x.UserId == userId, ct);
    public async Task<IReadOnlyList<ClientAccessModel>> ListAccessAsync(Guid agencyId, Guid clientId, CancellationToken ct) =>
        await (from access in db.ClientAccesses.AsNoTracking()
               join user in db.Users on access.UserId equals user.Id
               where access.AgencyId == agencyId && access.ClientId == clientId
               orderby user.Email
               select new ClientAccessModel(user.Id, user.Email, "ClientViewer", access.GrantedAtUtc)).ToArrayAsync(ct);
    public async Task<IReadOnlyList<ClientInvitation>> ListInvitationsAsync(Guid agencyId, Guid clientId, CancellationToken ct) => await db.ClientInvitations.Where(x => x.AgencyId == agencyId && x.ClientId == clientId).ToArrayAsync(ct);
    public Task<ClientInvitation?> FindInvitationAsync(Guid agencyId, Guid clientId, Guid invitationId, CancellationToken ct) => db.ClientInvitations.SingleOrDefaultAsync(x => x.AgencyId == agencyId && x.ClientId == clientId && x.Id == invitationId, ct);
    public Task<ClientInvitation?> FindInvitationByHashAsync(string tokenHash, CancellationToken ct) => db.ClientInvitations.SingleOrDefaultAsync(x => x.TokenHash == tokenHash, ct);
    public void Add(ClientInvitation invitation) => db.ClientInvitations.Add(invitation);
    public void Add(ClientAccess access) => db.ClientAccesses.Add(access);
    public void Remove(ClientAccess access) => db.ClientAccesses.Remove(access);
    public async Task SaveAsync(CancellationToken ct)
    {
        try { await db.SaveChangesAsync(ct); }
        catch (DbUpdateConcurrencyException) { throw new ConflictException("Client access changed concurrently. Refresh and try again."); }
        catch (DbUpdateException ex) when (ex.InnerException is PostgresException { SqlState: PostgresErrorCodes.UniqueViolation }
            or Microsoft.Data.Sqlite.SqliteException { SqliteExtendedErrorCode: 1555 or 2067 })
        { throw new ConflictException("The user or client access already exists. Refresh and try again."); }
    }
}
