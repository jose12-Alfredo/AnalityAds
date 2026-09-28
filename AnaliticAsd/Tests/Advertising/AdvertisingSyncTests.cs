using AnaliticAsd.Application.Abstractions;
using AnaliticAsd.Application.Advertising;
using AnaliticAsd.Application.Identity;
using AnaliticAsd.Application.Meta;
using AnaliticAsd.Domain.Advertising;
using AnaliticAsd.Domain.Clients;
using AnaliticAsd.Domain.Identity;
using AnaliticAsd.Domain.Meta;
using AnaliticAsd.Infrastructure.Persistence;
using AnaliticAsd.Infrastructure.Persistence.Repositories;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace AnaliticAsd.Tests.Advertising;

public sealed class AdvertisingSyncTests
{
    [Fact]
    public async Task Synchronizes_and_persists_campaign_adset_ad_hierarchy_for_current_tenant()
    {
        await using var connection = new SqliteConnection("Data Source=:memory:"); await connection.OpenAsync();
        var options = new DbContextOptionsBuilder<AnalitiAdsDbContext>().UseSqlite(connection).Options;
        await using var db = new AnalitiAdsDbContext(options); await db.Database.EnsureCreatedAsync();
        var agency = Agency.Create("Agency", DateTimeOffset.UtcNow); var client = Client.Create(agency.Id, "Client", DateTimeOffset.UtcNow);
        var account = AdAccount.Create(agency.Id, client.Id, MetaAdAccountId.Parse("123"), "Account", CurrencyCode.Parse("USD"), MetaTimeZoneId.Parse("America/La_Paz"), DateTimeOffset.UtcNow); account.MarkConnected(DateTimeOffset.UtcNow);
        db.AddRange(agency, client, account, MetaConnection.Create(agency.Id, "protected-token", DateTimeOffset.UtcNow.AddDays(10), DateTimeOffset.UtcNow)); await db.SaveChangesAsync();
        var tenant = new FixedTenant(agency.Id); var accountRepo = new AdAccountRepository(db, tenant); var advertisingRepo = new AdvertisingRepository(db, tenant);
        var service = new AdvertisingService(advertisingRepo, accountRepo, new MetaConnectionRepository(db), new TokenProtector(), new MetaClient(), tenant, (IUnitOfWork)db, TimeProvider.System);

        var result = await service.SyncAsync(account.Id);
        var campaigns = await service.ListCampaignsAsync(account.Id, false); var adSets = await service.ListAdSetsAsync(campaigns[0].Id, false); var ads = await service.ListAdsAsync(adSets[0].Id, false);

        Assert.Equal(SyncStatus.Succeeded, result.Status); Assert.Equal(1, result.CampaignsSynced); Assert.Single(campaigns); Assert.Single(adSets); Assert.Single(ads);
        Assert.Equal("cmp-1", campaigns[0].MetaCampaignId); Assert.Equal("set-1", adSets[0].MetaAdSetId); Assert.Equal("ad-1", ads[0].MetaAdId);
    }

    private sealed record FixedTenant(Guid AgencyId) : ICurrentTenant { public Guid UserId => Guid.NewGuid(); public string Role => "Owner"; }
    private sealed class TokenProtector : IMetaTokenProtector { public string Protect(string token) => token; public string Unprotect(string protectedToken) => "raw-token"; }
    private sealed class MetaClient : IMetaAdvertisingClient
    {
        public Task<MetaHierarchy> GetHierarchyAsync(string accountId, string token, CancellationToken ct = default) => Task.FromResult(new MetaHierarchy(
            [new RemoteCampaign("cmp-1", "Campaign", "OUTCOME_TRAFFIC", "ACTIVE", "ACTIVE", null, null, DateTimeOffset.UtcNow, DateTimeOffset.UtcNow)],
            [new RemoteAdSet("set-1", "cmp-1", "Ad set", "LINK_CLICKS", "IMPRESSIONS", "ACTIVE", "ACTIVE", null, null, DateTimeOffset.UtcNow, DateTimeOffset.UtcNow)],
            [new RemoteAd("ad-1", "set-1", "Ad", "ACTIVE", "ACTIVE", DateTimeOffset.UtcNow, DateTimeOffset.UtcNow)]));
    }
}
