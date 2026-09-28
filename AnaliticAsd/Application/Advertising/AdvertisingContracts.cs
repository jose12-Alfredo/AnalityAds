using AnaliticAsd.Domain.Advertising;

namespace AnaliticAsd.Application.Advertising;

public sealed record CampaignModel(Guid Id, Guid AdAccountId, string MetaCampaignId, string Name, string Objective, string ConfiguredStatus, string EffectiveStatus, DateTimeOffset? StartsAtUtc, DateTimeOffset? StopsAtUtc, DateTimeOffset? MetaCreatedAtUtc, DateTimeOffset? MetaUpdatedAtUtc, DateTimeOffset LastSyncedAtUtc, bool IsPresentOnMeta);
public sealed record AdSetModel(Guid Id, Guid CampaignId, string MetaAdSetId, string Name, string OptimizationGoal, string BillingEvent, string ConfiguredStatus, string EffectiveStatus, DateTimeOffset? StartsAtUtc, DateTimeOffset? EndsAtUtc, DateTimeOffset? MetaCreatedAtUtc, DateTimeOffset? MetaUpdatedAtUtc, DateTimeOffset LastSyncedAtUtc, bool IsPresentOnMeta);
public sealed record AdModel(Guid Id, Guid AdSetId, string MetaAdId, string Name, string ConfiguredStatus, string EffectiveStatus, DateTimeOffset? MetaCreatedAtUtc, DateTimeOffset? MetaUpdatedAtUtc, DateTimeOffset LastSyncedAtUtc, bool IsPresentOnMeta);
public sealed record SyncModel(Guid AdAccountId, SyncStatus Status, DateTimeOffset? StartedAtUtc, DateTimeOffset? CompletedAtUtc, int CampaignsSynced, int AdSetsSynced, int AdsSynced, string? ErrorCode);
public sealed record MetaHierarchy(IReadOnlyList<RemoteCampaign> Campaigns, IReadOnlyList<RemoteAdSet> AdSets, IReadOnlyList<RemoteAd> Ads);

public interface IAdvertisingRepository
{
    Task<IReadOnlyList<Campaign>> ListCampaignsAsync(Guid accountId, bool includeMissing, CancellationToken cancellationToken = default);
    Task<Campaign?> GetCampaignAsync(Guid id, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<AdSet>> ListAdSetsAsync(Guid campaignId, bool includeMissing, CancellationToken cancellationToken = default);
    Task<AdSet?> GetAdSetAsync(Guid id, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Ad>> ListAdsAsync(Guid adSetId, bool includeMissing, CancellationToken cancellationToken = default);
    Task<Ad?> GetAdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<AdAccountSync?> GetSyncAsync(Guid accountId, bool trackChanges, CancellationToken cancellationToken = default);
    Task<Dictionary<string, Campaign>> CampaignsForSyncAsync(Guid accountId, CancellationToken cancellationToken = default);
    Task<Dictionary<string, AdSet>> AdSetsForSyncAsync(Guid accountId, CancellationToken cancellationToken = default);
    Task<Dictionary<string, Ad>> AdsForSyncAsync(Guid accountId, CancellationToken cancellationToken = default);
    void Add(Campaign value); void Add(AdSet value); void Add(Ad value); void Add(AdAccountSync value);
}

public interface IMetaAdvertisingClient { Task<MetaHierarchy> GetHierarchyAsync(string metaAccountId, string accessToken, CancellationToken cancellationToken = default); }
public interface IAdvertisingService
{
    Task<SyncModel> SyncAsync(Guid accountId, CancellationToken cancellationToken = default);
    Task<SyncModel> GetSyncAsync(Guid accountId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<CampaignModel>> ListCampaignsAsync(Guid accountId, bool includeMissing, CancellationToken cancellationToken = default);
    Task<CampaignModel> GetCampaignAsync(Guid id, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<AdSetModel>> ListAdSetsAsync(Guid campaignId, bool includeMissing, CancellationToken cancellationToken = default);
    Task<AdSetModel> GetAdSetAsync(Guid id, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<AdModel>> ListAdsAsync(Guid adSetId, bool includeMissing, CancellationToken cancellationToken = default);
    Task<AdModel> GetAdAsync(Guid id, CancellationToken cancellationToken = default);
}
