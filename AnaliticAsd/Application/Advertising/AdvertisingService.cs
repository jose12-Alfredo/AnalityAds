using AnaliticAsd.Application.Abstractions;
using AnaliticAsd.Application.AdAccounts;
using AnaliticAsd.Application.Common;
using AnaliticAsd.Application.Identity;
using AnaliticAsd.Application.Meta;
using AnaliticAsd.Domain.Advertising;

namespace AnaliticAsd.Application.Advertising;

public sealed class AdvertisingService(IAdvertisingRepository repository, IAdAccountRepository accounts, IMetaConnectionRepository connections, IMetaTokenProtector tokenProtector, IMetaAdvertisingClient meta, ICurrentTenant tenant, IUnitOfWork unitOfWork, TimeProvider timeProvider) : IAdvertisingService
{
    public async Task<SyncModel> SyncAsync(Guid accountId, CancellationToken cancellationToken = default)
    {
        var account = await RequiredAccount(accountId, cancellationToken);
        if (account.ConnectionStatus != AdAccountConnectionStatus.Connected) throw new ConflictException("The ad account is not connected through Meta OAuth.");
        var connection = await connections.GetAsync(tenant.AgencyId, false, cancellationToken) ?? throw new ConflictException("The agency is not connected to Meta.");
        if (connection.ExpiresAtUtc is not null && connection.ExpiresAtUtc <= timeProvider.GetUtcNow()) throw new ConflictException("The Meta connection has expired. Connect again.");
        var sync = await repository.GetSyncAsync(accountId, true, cancellationToken);
        if (sync?.Status == SyncStatus.Running) throw new ConflictException("A synchronization is already running for this account.");
        sync ??= AdAccountSync.Create(accountId);
        if (sync.Status == SyncStatus.NeverSynced) repository.Add(sync);
        sync.Start(timeProvider.GetUtcNow());
        await unitOfWork.SaveChangesAsync(cancellationToken);

        try
        {
            var hierarchy = await meta.GetHierarchyAsync(account.MetaAccountId.Value, tokenProtector.Unprotect(connection.ProtectedAccessToken), cancellationToken);
            await Upsert(accountId, hierarchy, cancellationToken);
            sync.Succeed(hierarchy.Campaigns.Count, hierarchy.AdSets.Count, hierarchy.Ads.Count, timeProvider.GetUtcNow());
            await unitOfWork.SaveChangesAsync(cancellationToken);
            return Map(sync);
        }
        catch (OperationCanceledException)
        {
            sync.Fail("cancelled", timeProvider.GetUtcNow());
            await unitOfWork.SaveChangesAsync(CancellationToken.None);
            throw;
        }
        catch (Exception exception)
        {
            sync.Fail("meta_sync_failed", timeProvider.GetUtcNow());
            await unitOfWork.SaveChangesAsync(CancellationToken.None);
            if (exception is ExternalServiceException) throw;
            throw new ExternalServiceException("Meta returned an inconsistent advertising hierarchy.");
        }
    }

    private async Task Upsert(Guid accountId, MetaHierarchy hierarchy, CancellationToken ct)
    {
        var now = timeProvider.GetUtcNow();
        var campaigns = await repository.CampaignsForSyncAsync(accountId, ct); foreach (var value in campaigns.Values) value.MarkMissing();
        foreach (var value in hierarchy.Campaigns) { if (campaigns.TryGetValue(value.MetaId, out var entity)) entity.Update(value, now); else { entity = Campaign.Create(accountId, value, now); repository.Add(entity); campaigns.Add(value.MetaId, entity); } }
        var adSets = await repository.AdSetsForSyncAsync(accountId, ct); foreach (var value in adSets.Values) value.MarkMissing();
        foreach (var value in hierarchy.AdSets) { if (!campaigns.TryGetValue(value.MetaCampaignId, out var campaign)) throw new InvalidOperationException("Ad set references an unknown campaign."); if (adSets.TryGetValue(value.MetaId, out var entity)) entity.Update(value, now); else { entity = AdSet.Create(campaign.Id, value, now); repository.Add(entity); adSets.Add(value.MetaId, entity); } }
        var ads = await repository.AdsForSyncAsync(accountId, ct); foreach (var value in ads.Values) value.MarkMissing();
        foreach (var value in hierarchy.Ads) { if (!adSets.TryGetValue(value.MetaAdSetId, out var adSet)) throw new InvalidOperationException("Ad references an unknown ad set."); if (ads.TryGetValue(value.MetaId, out var entity)) entity.Update(value, now); else { entity = Ad.Create(adSet.Id, value, now); repository.Add(entity); ads.Add(value.MetaId, entity); } }
    }

    public async Task<SyncModel> GetSyncAsync(Guid accountId, CancellationToken ct = default)
    {
        await RequiredAccount(accountId, ct); var sync = await repository.GetSyncAsync(accountId, false, ct); return sync is null ? new(accountId, SyncStatus.NeverSynced, null, null, 0, 0, 0, null) : Map(sync);
    }
    public async Task<IReadOnlyList<CampaignModel>> ListCampaignsAsync(Guid accountId, bool includeMissing, CancellationToken ct = default) { await RequiredAccount(accountId, ct); return (await repository.ListCampaignsAsync(accountId, includeMissing, ct)).Select(Map).ToArray(); }
    public async Task<CampaignModel> GetCampaignAsync(Guid id, CancellationToken ct = default) => Map(await repository.GetCampaignAsync(id, ct) ?? throw new EntityNotFoundException($"Campaign '{id}' was not found."));
    public async Task<IReadOnlyList<AdSetModel>> ListAdSetsAsync(Guid campaignId, bool includeMissing, CancellationToken ct = default) { await GetCampaignAsync(campaignId, ct); return (await repository.ListAdSetsAsync(campaignId, includeMissing, ct)).Select(Map).ToArray(); }
    public async Task<AdSetModel> GetAdSetAsync(Guid id, CancellationToken ct = default) => Map(await repository.GetAdSetAsync(id, ct) ?? throw new EntityNotFoundException($"Ad set '{id}' was not found."));
    public async Task<IReadOnlyList<AdModel>> ListAdsAsync(Guid adSetId, bool includeMissing, CancellationToken ct = default) { await GetAdSetAsync(adSetId, ct); return (await repository.ListAdsAsync(adSetId, includeMissing, ct)).Select(Map).ToArray(); }
    public async Task<AdModel> GetAdAsync(Guid id, CancellationToken ct = default) => Map(await repository.GetAdAsync(id, ct) ?? throw new EntityNotFoundException($"Ad '{id}' was not found."));
    private async Task<AdAccount> RequiredAccount(Guid id, CancellationToken ct) => await accounts.GetByIdAsync(id, false, ct) ?? throw new EntityNotFoundException($"Ad account '{id}' was not found.");
    private static SyncModel Map(AdAccountSync x) => new(x.AdAccountId, x.Status, x.StartedAtUtc, x.CompletedAtUtc, x.CampaignsSynced, x.AdSetsSynced, x.AdsSynced, x.ErrorCode);
    private static CampaignModel Map(Campaign x) => new(x.Id, x.AdAccountId, x.MetaCampaignId, x.Name, x.Objective, x.ConfiguredStatus, x.EffectiveStatus, x.StartsAtUtc, x.StopsAtUtc, x.MetaCreatedAtUtc, x.MetaUpdatedAtUtc, x.LastSyncedAtUtc, x.IsPresentOnMeta);
    private static AdSetModel Map(AdSet x) => new(x.Id, x.CampaignId, x.MetaAdSetId, x.Name, x.OptimizationGoal, x.BillingEvent, x.ConfiguredStatus, x.EffectiveStatus, x.StartsAtUtc, x.EndsAtUtc, x.MetaCreatedAtUtc, x.MetaUpdatedAtUtc, x.LastSyncedAtUtc, x.IsPresentOnMeta);
    private static AdModel Map(Ad x) => new(x.Id, x.AdSetId, x.MetaAdId, x.Name, x.ConfiguredStatus, x.EffectiveStatus, x.MetaCreatedAtUtc, x.MetaUpdatedAtUtc, x.LastSyncedAtUtc, x.IsPresentOnMeta);
}
