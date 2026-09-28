using AnaliticAsd.Domain.Advertising;
using AnaliticAsd.Domain.Clients;
using AnaliticAsd.Domain.Identity;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace AnaliticAsd.Infrastructure.Persistence;

public sealed class DevelopmentDataSeeder(AnalitiAdsDbContext db, IPasswordHasher<User> passwordHasher,
    TimeProvider clock, IConfiguration configuration)
{
    private const string OwnerEmail = "owner@analitiads.local";

    public async Task SeedAsync(CancellationToken cancellationToken = default)
    {
        if (!configuration.GetValue<bool>("DevelopmentSeed:Enabled")) return;

        if (await db.Users.AnyAsync(x => x.NormalizedEmail == User.NormalizeEmail(OwnerEmail), cancellationToken))
        {
            await EnsureBenchmarkScenariosAsync(cancellationToken);
            return;
        }

        var password = configuration["DevelopmentSeed:Password"];
        if (string.IsNullOrWhiteSpace(password))
            throw new InvalidOperationException("DevelopmentSeed:Password is required when development seeding is enabled.");

        var now = clock.GetUtcNow();
        await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);
        var agency = Agency.Create("AnalitiAds Demo Agency", now);
        var otherAgency = Agency.Create("Isolation Test Agency", now);
        db.Agencies.AddRange(agency, otherAgency);
        var owner = AddUser(agency, OwnerEmail, AgencyRole.Owner, password, now);
        AddUser(agency, "admin@analitiads.local", AgencyRole.Admin, password, now);
        AddUser(agency, "analyst@analitiads.local", AgencyRole.Analyst, password, now);
        AddUser(agency, "viewer@analitiads.local", AgencyRole.Viewer, password, now);
        var clientViewer = AddUser(agency, "clientviewer@analitiads.local", AgencyRole.ClientViewer, password, now);
        AddUser(otherAgency, "owner.isolation@analitiads.local", AgencyRole.Owner, password, now);

        var activeClient = Client.Create(agency.Id, "Cliente activo de demostración", now);
        var inactiveClient = Client.Create(agency.Id, "Cliente inactivo de demostración", now);
        inactiveClient.Update(inactiveClient.Name, false, now);
        var revokedClient = Client.Create(agency.Id, "Cliente con acceso revocado", now);
        var foreignClient = Client.Create(otherAgency.Id, "Cliente de otra agencia", now);
        db.Clients.AddRange(activeClient, inactiveClient, revokedClient, foreignClient);
        db.ClientAccesses.Add(ClientAccess.Create(agency.Id, activeClient.Id, clientViewer.Id, now));
        var invitation = ClientInvitation.Create(agency.Id, revokedClient.Id, owner.Id,
            "revoked.clientviewer@analitiads.local", new string('a', 64), now);
        invitation.Revoke(now);
        db.ClientInvitations.Add(invitation);

        var account = AdAccount.Create(agency.Id, activeClient.Id, MetaAdAccountId.Parse("990000000001"), "Cuenta demo USD",
            CurrencyCode.Parse("USD"), MetaTimeZoneId.Parse("America/La_Paz"), now);
        db.AdAccounts.AddRange(account,
            AdAccount.Create(agency.Id, inactiveClient.Id, MetaAdAccountId.Parse("990000000002"), "Cuenta de cliente inactivo", CurrencyCode.Parse("BOB"), MetaTimeZoneId.Parse("America/La_Paz"), now),
            AdAccount.Create(otherAgency.Id, foreignClient.Id, MetaAdAccountId.Parse("990000000003"), "Cuenta aislada", CurrencyCode.Parse("USD"), MetaTimeZoneId.Parse("America/La_Paz"), now));

        var hierarchies = new[]
        {
            AddHierarchy(account, "991", "Ventas principal", now),
            AddHierarchy(account, "992", "Ventas remarketing", now),
            AddHierarchy(account, "993", "Ventas catálogo", now)
        };
        var baseline = new DateOnly(2026, 8, 31);
        var days = new[] { new DateOnly(2026, 9, 1), new DateOnly(2026, 9, 2), new DateOnly(2026, 9, 3) };
        db.InsightSnapshots.Add(Snapshot(account.Id, null, null, null, InsightLevel.Account, baseline, "USD", 0m, 0, 0, 0, 0m, 0m, 0m, now));
        db.InsightSnapshots.Add(Snapshot(account.Id, null, null, null, InsightLevel.Account, days[0], "USD", 100m, 10000, 7000, 250, 15m, 5m, 600m, now));
        db.InsightSnapshots.Add(Snapshot(account.Id, null, null, null, InsightLevel.Account, days[1], "USD", null, 8000, null, 180, null, 2m, 220m, now));
        db.InsightSnapshots.Add(Snapshot(account.Id, null, null, null, InsightLevel.Account, days[2], "BOB", 700m, 9000, 6200, 210, 12m, 3m, 500m, now));

        for (var index = 0; index < hierarchies.Length; index++)
        {
            var (campaign, adSet, ad) = hierarchies[index];
            var spend = 40m + index * 20m;
            foreach (var day in days)
                db.InsightSnapshots.Add(Snapshot(account.Id, campaign.Id, null, null, InsightLevel.Campaign, day, "USD", spend, 4000 + index * 1000, 2500 + index * 500, 100 + index * 20, 6 + index, 1 + index, 160 + index * 80, now));
            db.InsightSnapshots.Add(Snapshot(account.Id, campaign.Id, adSet.Id, null, InsightLevel.AdSet, days[0], "USD", spend, 4000, 2500, 100, 6m, 2m, 200m, now));
            db.InsightSnapshots.Add(Snapshot(account.Id, campaign.Id, adSet.Id, ad.Id, InsightLevel.Ad, days[0], "USD", spend, 4000, 2500, 100, 6m, 2m, 200m, now));
        }
        var legacy = Snapshot(account.Id, hierarchies[0].Campaign.Id, null, null, InsightLevel.Campaign,
            new DateOnly(2026, 8, 30), "USD", 0m, 0, 0, 0, 0m, 0m, 0m, now);
        legacy.MarkLegacyZeroNormalized();
        db.InsightSnapshots.Add(legacy);
        await db.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
    }

    private async Task EnsureBenchmarkScenariosAsync(CancellationToken cancellationToken)
    {
        var client = await db.Clients.SingleAsync(x => x.Name == "Cliente activo de demostración", cancellationToken);
        var account = await db.AdAccounts.SingleAsync(x => x.ClientId == client.Id, cancellationToken);
        var now = clock.GetUtcNow();
        var days = Enumerable.Range(0, 5).Select(offset => new DateOnly(2026, 8, 30).AddDays(offset)).ToArray();

        if (!await db.Campaigns.AnyAsync(x => x.AdAccountId == account.Id && x.MetaCampaignId == "99401", cancellationToken))
        {
            var complete = new[]
            {
                AddHierarchy(account, "994", "Benchmark completo A", now, "LEADS"),
                AddHierarchy(account, "995", "Benchmark completo B", now, "LEADS"),
                AddHierarchy(account, "996", "Benchmark completo C", now, "LEADS")
            };
            for (var index = 0; index < complete.Length; index++)
                foreach (var day in days)
                    db.InsightSnapshots.Add(Snapshot(account.Id, complete[index].Campaign.Id, null, null, InsightLevel.Campaign,
                        day, "USD", 30m + index * 10m, 3000 + index * 500, 2000 + index * 300,
                        90 + index * 15, 8m + index, 2m + index, 180m + index * 60m, now));
        }

        if (!await db.Campaigns.AnyAsync(x => x.AdAccountId == account.Id && x.MetaCampaignId == "99701", cancellationToken))
        {
            var zeroBenchmark = new[]
            {
                AddHierarchy(account, "997", "Benchmark cero evaluado", now, "AWARENESS"),
                AddHierarchy(account, "998", "Benchmark cero par A", now, "AWARENESS"),
                AddHierarchy(account, "999", "Benchmark cero par B", now, "AWARENESS")
            };
            foreach (var day in days)
            {
                db.InsightSnapshots.Add(Snapshot(account.Id, zeroBenchmark[0].Campaign.Id, null, null, InsightLevel.Campaign,
                    day, "USD", 25m, 2500, 1800, 75, 5m, 1m, 100m, now));
                for (var index = 1; index < zeroBenchmark.Length; index++)
                    db.InsightSnapshots.Add(Snapshot(account.Id, zeroBenchmark[index].Campaign.Id, null, null, InsightLevel.Campaign,
                        day, "USD", 0m, 2500 + index * 100, 1800 + index * 100, 50, 5m, 1m, 100m, now));
            }
        }

        await db.SaveChangesAsync(cancellationToken);
    }

    private User AddUser(Agency agency, string email, AgencyRole role, string password, DateTimeOffset now)
    {
        var user = User.Create(email, now);
        user.SetPasswordHash(passwordHasher.HashPassword(user, password));
        db.Users.Add(user);
        db.Memberships.Add(Membership.Create(agency.Id, user.Id, role, now));
        return user;
    }

    private (Campaign Campaign, AdSet AdSet, Ad Ad) AddHierarchy(AdAccount account, string prefix, string name, DateTimeOffset now, string objective = "SALES")
    {
        var campaign = Campaign.Create(account.Id, new(prefix + "01", name, objective, "ACTIVE", "ACTIVE", null, null, now.AddDays(-30), now), now);
        var adSet = AdSet.Create(campaign.Id, new(prefix + "02", campaign.MetaCampaignId, name + " / conjunto", "OFFSITE_CONVERSIONS", "IMPRESSIONS", "ACTIVE", "ACTIVE", null, null, now.AddDays(-30), now), now);
        var ad = Ad.Create(adSet.Id, new(prefix + "03", adSet.MetaAdSetId, name + " / anuncio", "ACTIVE", "ACTIVE", now.AddDays(-30), now), now);
        db.AddRange(campaign, adSet, ad);
        return (campaign, adSet, ad);
    }

    private static InsightSnapshot Snapshot(Guid accountId, Guid? campaignId, Guid? adSetId, Guid? adId,
        InsightLevel level, DateOnly day, string currency, decimal? spend, long? impressions, long? reach,
        long? clicks, decimal? leads, decimal? purchases, decimal? purchaseValue, DateTimeOffset now) =>
        InsightSnapshot.Create(accountId, campaignId, adSetId, adId, level,
            new RemoteInsight(level, day, null, null, null, spend, impressions, reach, clicks, leads, purchases, purchaseValue), currency, now);
}
