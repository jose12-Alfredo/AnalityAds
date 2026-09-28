using AnaliticAsd.Application.Identity;
using AnaliticAsd.Domain.Identity;
using Microsoft.EntityFrameworkCore;

namespace AnaliticAsd.Infrastructure.Persistence.Repositories;

public sealed class IdentityRepository(AnalitiAdsDbContext dbContext) : IIdentityRepository
{
    public Task<User?> FindUserByEmailAsync(string normalizedEmail, CancellationToken cancellationToken = default) =>
        dbContext.Users.SingleOrDefaultAsync(x => x.NormalizedEmail == normalizedEmail, cancellationToken);

    public async Task<IReadOnlyList<(Membership Membership, Agency Agency)>> ListMembershipsAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var rows = await (from membership in dbContext.Memberships
                          join agency in dbContext.Agencies on membership.AgencyId equals agency.Id
                          where membership.UserId == userId
                          select new { Membership = membership, Agency = agency }).ToListAsync(cancellationToken);
        return rows.Select(x => (x.Membership, x.Agency)).ToArray();
    }

    public void Add(Agency agency) => dbContext.Agencies.Add(agency);
    public void Add(User user) => dbContext.Users.Add(user);
    public void Add(Membership membership) => dbContext.Memberships.Add(membership);
}
