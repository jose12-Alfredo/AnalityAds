using AnaliticAsd.Application.Advertising;
using AnaliticAsd.Application.Identity;
using AnaliticAsd.Domain.Advertising;
using Microsoft.EntityFrameworkCore;

namespace AnaliticAsd.Infrastructure.Persistence.Repositories;

public sealed class AdvertisingRepository(AnalitiAdsDbContext db, ICurrentTenant tenant) : IAdvertisingRepository
{
    private AuthorizedData Scoped => new(db, tenant);
    private IQueryable<Campaign> Campaigns => Scoped.Campaigns;
    private IQueryable<AdSet> AdSets => Scoped.AdSets;
    private IQueryable<Ad> Ads => Scoped.Ads;
    public async Task<IReadOnlyList<Campaign>> ListCampaignsAsync(Guid accountId, bool includeMissing, CancellationToken ct = default) => await Campaigns.AsNoTracking().Where(x => x.AdAccountId == accountId && (includeMissing || x.IsPresentOnMeta)).OrderBy(x => x.Name).ToArrayAsync(ct);
    public Task<Campaign?> GetCampaignAsync(Guid id, CancellationToken ct = default) => Campaigns.AsNoTracking().SingleOrDefaultAsync(x => x.Id == id, ct);
    public async Task<IReadOnlyList<AdSet>> ListAdSetsAsync(Guid campaignId, bool includeMissing, CancellationToken ct = default) => await AdSets.AsNoTracking().Where(x => x.CampaignId == campaignId && (includeMissing || x.IsPresentOnMeta)).OrderBy(x => x.Name).ToArrayAsync(ct);
    public Task<AdSet?> GetAdSetAsync(Guid id, CancellationToken ct = default) => AdSets.AsNoTracking().SingleOrDefaultAsync(x => x.Id == id, ct);
    public async Task<IReadOnlyList<Ad>> ListAdsAsync(Guid adSetId, bool includeMissing, CancellationToken ct = default) => await Ads.AsNoTracking().Where(x => x.AdSetId == adSetId && (includeMissing || x.IsPresentOnMeta)).OrderBy(x => x.Name).ToArrayAsync(ct);
    public Task<Ad?> GetAdAsync(Guid id, CancellationToken ct = default) => Ads.AsNoTracking().SingleOrDefaultAsync(x => x.Id == id, ct);
    public Task<AdAccountSync?> GetSyncAsync(Guid accountId, bool trackChanges, CancellationToken ct = default) { var accounts = Scoped.Accounts; IQueryable<AdAccountSync> query = db.AdAccountSyncs.Where(x => accounts.Any(a => a.Id == x.AdAccountId)); if (!trackChanges) query = query.AsNoTracking(); return query.SingleOrDefaultAsync(x => x.AdAccountId == accountId, ct); }
    public Task<Dictionary<string, Campaign>> CampaignsForSyncAsync(Guid accountId, CancellationToken ct = default) => Campaigns.Where(x => x.AdAccountId == accountId).ToDictionaryAsync(x => x.MetaCampaignId, ct);
    public Task<Dictionary<string, AdSet>> AdSetsForSyncAsync(Guid accountId, CancellationToken ct = default) => AdSets.Where(x => Campaigns.Any(c => c.Id == x.CampaignId && c.AdAccountId == accountId)).ToDictionaryAsync(x => x.MetaAdSetId, ct);
    public Task<Dictionary<string, Ad>> AdsForSyncAsync(Guid accountId, CancellationToken ct = default) => Ads.Where(x => AdSets.Any(s => s.Id == x.AdSetId && Campaigns.Any(c => c.Id == s.CampaignId && c.AdAccountId == accountId))).ToDictionaryAsync(x => x.MetaAdId, ct);
    public void Add(Campaign value) => db.Campaigns.Add(value); public void Add(AdSet value) => db.AdSets.Add(value); public void Add(Ad value) => db.Ads.Add(value); public void Add(AdAccountSync value) => db.AdAccountSyncs.Add(value);
}
