using AnaliticAsd.Application.Advertising;
using AnaliticAsd.Contracts.Advertising;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AnaliticAsd.Controllers;

[ApiController]
[Authorize]
[Route("api/v1")]
public sealed class AdvertisingController(IAdvertisingService service) : ControllerBase
{
    [HttpPost("ad-accounts/{adAccountId:guid}/sync")]
    [Authorize(Roles = "Owner,Admin,Analyst")]
    public async Task<ActionResult<SyncResponse>> Sync(Guid adAccountId, CancellationToken ct) => Ok(Map(await service.SyncAsync(adAccountId, ct)));
    [HttpGet("ad-accounts/{adAccountId:guid}/sync")]
    public async Task<ActionResult<SyncResponse>> SyncStatus(Guid adAccountId, CancellationToken ct) => Ok(Map(await service.GetSyncAsync(adAccountId, ct)));
    [HttpGet("ad-accounts/{adAccountId:guid}/campaigns")]
    public async Task<ActionResult<IReadOnlyList<CampaignResponse>>> Campaigns(Guid adAccountId, [FromQuery] bool includeMissing = false, CancellationToken ct = default) => Ok((await service.ListCampaignsAsync(adAccountId, includeMissing, ct)).Select(Map).ToArray());
    [HttpGet("campaigns/{campaignId:guid}")]
    public async Task<ActionResult<CampaignResponse>> Campaign(Guid campaignId, CancellationToken ct) => Ok(Map(await service.GetCampaignAsync(campaignId, ct)));
    [HttpGet("campaigns/{campaignId:guid}/ad-sets")]
    public async Task<ActionResult<IReadOnlyList<AdSetResponse>>> AdSets(Guid campaignId, [FromQuery] bool includeMissing = false, CancellationToken ct = default) => Ok((await service.ListAdSetsAsync(campaignId, includeMissing, ct)).Select(Map).ToArray());
    [HttpGet("ad-sets/{adSetId:guid}")]
    public async Task<ActionResult<AdSetResponse>> AdSet(Guid adSetId, CancellationToken ct) => Ok(Map(await service.GetAdSetAsync(adSetId, ct)));
    [HttpGet("ad-sets/{adSetId:guid}/ads")]
    public async Task<ActionResult<IReadOnlyList<AdResponse>>> Ads(Guid adSetId, [FromQuery] bool includeMissing = false, CancellationToken ct = default) => Ok((await service.ListAdsAsync(adSetId, includeMissing, ct)).Select(Map).ToArray());
    [HttpGet("ads/{adId:guid}")]
    public async Task<ActionResult<AdResponse>> Ad(Guid adId, CancellationToken ct) => Ok(Map(await service.GetAdAsync(adId, ct)));
    private static SyncResponse Map(SyncModel x) => new(x.AdAccountId, x.Status.ToString(), x.StartedAtUtc, x.CompletedAtUtc, x.CampaignsSynced, x.AdSetsSynced, x.AdsSynced, x.ErrorCode);
    private static CampaignResponse Map(CampaignModel x) => new(x.Id, x.AdAccountId, x.MetaCampaignId, x.Name, x.Objective, x.ConfiguredStatus, x.EffectiveStatus, x.StartsAtUtc, x.StopsAtUtc, x.MetaCreatedAtUtc, x.MetaUpdatedAtUtc, x.LastSyncedAtUtc, x.IsPresentOnMeta);
    private static AdSetResponse Map(AdSetModel x) => new(x.Id, x.CampaignId, x.MetaAdSetId, x.Name, x.OptimizationGoal, x.BillingEvent, x.ConfiguredStatus, x.EffectiveStatus, x.StartsAtUtc, x.EndsAtUtc, x.MetaCreatedAtUtc, x.MetaUpdatedAtUtc, x.LastSyncedAtUtc, x.IsPresentOnMeta);
    private static AdResponse Map(AdModel x) => new(x.Id, x.AdSetId, x.MetaAdId, x.Name, x.ConfiguredStatus, x.EffectiveStatus, x.MetaCreatedAtUtc, x.MetaUpdatedAtUtc, x.LastSyncedAtUtc, x.IsPresentOnMeta);
}
