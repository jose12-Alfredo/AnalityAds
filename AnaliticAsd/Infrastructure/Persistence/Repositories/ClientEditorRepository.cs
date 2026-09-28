using AnaliticAsd.Application.Common;
using AnaliticAsd.Application.Identity;
using AnaliticAsd.Domain.Identity;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace AnaliticAsd.Infrastructure.Persistence.Repositories;

public sealed class ClientEditorRepository(AnalitiAdsDbContext dbContext) : IClientEditorRepository
{
    public async Task<IReadOnlyList<AgencyEditorModel>> ListEligibleAsync(Guid agencyId,
        CancellationToken cancellationToken = default) =>
        await (from membership in dbContext.Memberships.AsNoTracking()
               join user in dbContext.Users.AsNoTracking() on membership.UserId equals user.Id
               where membership.AgencyId == agencyId && membership.Role == AgencyRole.Analyst
               orderby user.Email
               select new AgencyEditorModel(user.Id, user.Email, user.IsActive))
            .ToArrayAsync(cancellationToken);

    public async Task<IReadOnlyList<ClientEditorModel>> ListAssignedAsync(Guid agencyId, Guid clientId,
        CancellationToken cancellationToken = default) =>
        await (from assignment in dbContext.ClientEditorAssignments.AsNoTracking()
               join user in dbContext.Users.AsNoTracking() on assignment.UserId equals user.Id
               where assignment.AgencyId == agencyId && assignment.ClientId == clientId
               orderby user.Email
               select new ClientEditorModel(user.Id, user.Email, assignment.GrantedAtUtc, assignment.Version))
            .ToArrayAsync(cancellationToken);

    public Task<EditorMemberModel?> FindMemberAsync(Guid agencyId, Guid userId,
        CancellationToken cancellationToken = default) =>
        (from membership in dbContext.Memberships.AsNoTracking()
         join user in dbContext.Users.AsNoTracking() on membership.UserId equals user.Id
         where membership.AgencyId == agencyId && membership.UserId == userId
         select new EditorMemberModel(user.Id, user.Email, user.IsActive, membership.Role))
        .SingleOrDefaultAsync(cancellationToken);

    public Task<ClientEditorAssignment?> FindAssignmentAsync(Guid agencyId, Guid clientId, Guid userId,
        bool trackChanges, CancellationToken cancellationToken = default)
    {
        IQueryable<ClientEditorAssignment> query = dbContext.ClientEditorAssignments;
        if (!trackChanges) query = query.AsNoTracking();
        return query.SingleOrDefaultAsync(assignment => assignment.AgencyId == agencyId
            && assignment.ClientId == clientId && assignment.UserId == userId, cancellationToken);
    }

    public void Add(ClientEditorAssignment assignment) => dbContext.ClientEditorAssignments.Add(assignment);
    public void Remove(ClientEditorAssignment assignment) => dbContext.ClientEditorAssignments.Remove(assignment);

    public async Task SaveAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            await dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateConcurrencyException)
        {
            throw new ConflictException("The editor assignment changed concurrently. Refresh and try again.");
        }
        catch (DbUpdateException exception) when (exception.InnerException is PostgresException
            { SqlState: PostgresErrorCodes.UniqueViolation }
            or Microsoft.Data.Sqlite.SqliteException { SqliteExtendedErrorCode: 1555 or 2067 })
        {
            throw new ConflictException("The editor is already assigned to this client.");
        }
    }
}
