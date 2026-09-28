using AnaliticAsd.Application.Identity;
using AnaliticAsd.Application.Metrics;
using AnaliticAsd.Domain.Advertising;
using Microsoft.EntityFrameworkCore;

namespace AnaliticAsd.Infrastructure.Persistence.Repositories;

public sealed class MetricsRepository(AnalitiAdsDbContext db, ICurrentTenant tenant) : IMetricsRepository
{
    private AuthorizedData Data => new(db, tenant);
    private IQueryable<InsightSnapshot> Scoped { get { var accounts = Data.Accounts; return db.InsightSnapshots.Where(x => accounts.Any(a => a.Id == x.AdAccountId)); } }
    public Task<Dictionary<string, Campaign>> CampaignsAsync(Guid id, CancellationToken ct = default) => Data.Campaigns.Where(x => x.AdAccountId == id).ToDictionaryAsync(x => x.MetaCampaignId, ct);
    public Task<Dictionary<string, AdSet>> AdSetsAsync(Guid id, CancellationToken ct = default) { var campaigns = Data.Campaigns; return Data.AdSets.Where(x => campaigns.Any(c => c.Id == x.CampaignId && c.AdAccountId == id)).ToDictionaryAsync(x => x.MetaAdSetId, ct); }
    public Task<Dictionary<string, Ad>> AdsAsync(Guid id, CancellationToken ct = default) { var campaigns = Data.Campaigns; var sets = Data.AdSets; return Data.Ads.Where(x => sets.Any(s => s.Id == x.AdSetId && campaigns.Any(c => c.Id == s.CampaignId && c.AdAccountId == id))).ToDictionaryAsync(x => x.MetaAdId, ct); }
    public Task<InsightSnapshot?> FindAsync(Guid accountId, InsightLevel level, Guid? entityId, DateOnly date, CancellationToken ct = default) => Scoped.SingleOrDefaultAsync(x => x.AdAccountId == accountId && x.Level == level && x.SnapshotDate == date && (level == InsightLevel.Account || level == InsightLevel.Campaign && x.CampaignId == entityId || level == InsightLevel.AdSet && x.AdSetId == entityId || level == InsightLevel.Ad && x.AdId == entityId), ct);
    public async Task<IReadOnlyList<InsightSnapshot>> ListAsync(Guid accountId, InsightLevel level, Guid? entityId, DateOnly since, DateOnly until, CancellationToken ct = default) => await Scoped.AsNoTracking().Where(x => x.AdAccountId == accountId && x.Level == level && x.SnapshotDate >= since && x.SnapshotDate <= until && (level == InsightLevel.Account || level == InsightLevel.Campaign && x.CampaignId == entityId || level == InsightLevel.AdSet && x.AdSetId == entityId || level == InsightLevel.Ad && x.AdId == entityId)).OrderBy(x => x.SnapshotDate).ToArrayAsync(ct);
    public async Task<IReadOnlyList<InsightSnapshot>> ListForAccountAsync(Guid accountId, InsightLevel level, DateOnly since, DateOnly until, CancellationToken ct = default) => await Scoped.AsNoTracking().Where(x => x.AdAccountId == accountId && x.Level == level && x.SnapshotDate >= since && x.SnapshotDate <= until).OrderBy(x => x.SnapshotDate).ToArrayAsync(ct);
    public void Add(InsightSnapshot value) => db.InsightSnapshots.Add(value);
}
