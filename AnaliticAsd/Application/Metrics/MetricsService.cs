using AnaliticAsd.Application.Abstractions;
using AnaliticAsd.Application.AdAccounts;
using AnaliticAsd.Application.Advertising;
using AnaliticAsd.Application.Common;
using AnaliticAsd.Application.Identity;
using AnaliticAsd.Application.Meta;
using AnaliticAsd.Domain.Advertising;

namespace AnaliticAsd.Application.Metrics;

public sealed class MetricsService(IMetricsRepository repository, IAdAccountRepository accounts, IAdvertisingRepository advertising, IMetaConnectionRepository connections, IMetaTokenProtector protector, IMetaInsightsClient meta, ICurrentTenant tenant, IUnitOfWork unitOfWork, TimeProvider clock) : IMetricsService
{
    public async Task<MetricsSyncModel> SyncAsync(Guid accountId, SyncMetricsCommand command, CancellationToken ct = default)
    {
        ValidateRange(command.Since, command.Until);
        var account = await accounts.GetByIdAsync(accountId, true, ct)
            ?? throw new EntityNotFoundException($"Ad account '{accountId}' was not found.");
        if (account.ConnectionStatus != AdAccountConnectionStatus.Connected) throw new ConflictException("The ad account is not connected through Meta OAuth.");
        var connection = await connections.GetAsync(tenant.AgencyId, false, ct) ?? throw new ConflictException("The agency is not connected to Meta.");
        if (connection.ExpiresAtUtc is not null && connection.ExpiresAtUtc <= clock.GetUtcNow()) throw new ConflictException("The Meta connection has expired. Connect again.");
        var campaigns = await repository.CampaignsAsync(accountId, ct); var adSets = await repository.AdSetsAsync(accountId, ct); var ads = await repository.AdsAsync(accountId, ct);
        var remote = await meta.GetDailyAsync(account.MetaAccountId.Value, protector.Unprotect(connection.ProtectedAccessToken), command.Since, command.Until, ct);
        var counts = new int[4]; var now = clock.GetUtcNow();
        var seen = new HashSet<(InsightLevel Level, Guid? EntityId, DateOnly Date)>();
        foreach (var value in remote.Values)
        {
            if (value.Date < command.Since || value.Date > command.Until)
                throw new ExternalServiceException("Meta returned an insight outside the requested date range.");
            var ids = Resolve(value, campaigns, adSets, ads); var entityId = value.Level switch { InsightLevel.Account => null, InsightLevel.Campaign => ids.campaign, InsightLevel.AdSet => ids.adSet, InsightLevel.Ad => ids.ad, _ => null };
            if (!seen.Add((value.Level, entityId, value.Date)))
                throw new ExternalServiceException("Meta returned duplicate daily insights for the same entity.");
            var snapshot = await repository.FindAsync(accountId, value.Level, entityId, value.Date, ct);
            if (snapshot is null) { snapshot = InsightSnapshot.Create(accountId, ids.campaign, ids.adSet, ids.ad, value.Level, value, account.Currency.Value, now); repository.Add(snapshot); }
            else snapshot.Update(value, account.Currency.Value, now);
            counts[(int)value.Level]++;
        }
        account.DataSource.RecordSynchronization(command.Since, command.Until, now);
        await unitOfWork.SaveChangesAsync(ct);
        return new(accountId, command.Since, command.Until, counts[0], counts[1], counts[2], counts[3], now);
    }

    public async Task<IReadOnlyList<InsightSnapshotModel>> ListAccountAsync(Guid id, DateOnly since, DateOnly until, CancellationToken ct = default) { ValidateRange(since, until); await RequiredAccount(id, ct); return Map(await repository.ListAsync(id, InsightLevel.Account, null, since, until, ct)); }
    public async Task<IReadOnlyList<InsightSnapshotModel>> ListCampaignAsync(Guid id, DateOnly since, DateOnly until, CancellationToken ct = default)
    {
        ValidateRange(since, until);
        var campaign = await RequiredCampaign(id, ct);
        return Map(await repository.ListAsync(campaign.AdAccountId, InsightLevel.Campaign, id, since, until, ct));
    }
    public async Task<IReadOnlyList<InsightSnapshotModel>> ListAdSetAsync(Guid id, DateOnly since, DateOnly until, CancellationToken ct = default)
    {
        ValidateRange(since, until);
        var adSet = await RequiredAdSet(id, ct);
        var campaign = await RequiredCampaign(adSet.CampaignId, ct);
        return Map(await repository.ListAsync(campaign.AdAccountId, InsightLevel.AdSet, id, since, until, ct));
    }
    public async Task<IReadOnlyList<InsightSnapshotModel>> ListAdAsync(Guid id, DateOnly since, DateOnly until, CancellationToken ct = default)
    {
        ValidateRange(since, until);
        var ad = await advertising.GetAdAsync(id, ct) ?? throw new EntityNotFoundException("Ad was not found.");
        var adSet = await RequiredAdSet(ad.AdSetId, ct);
        var campaign = await RequiredCampaign(adSet.CampaignId, ct);
        return Map(await repository.ListAsync(campaign.AdAccountId, InsightLevel.Ad, id, since, until, ct));
    }

    public async Task<RangeMetricsSummaryModel> GetAccountSummaryAsync(Guid id, DateOnly since, DateOnly until, CancellationToken ct = default)
    {
        ValidateRange(since, until);
        await RequiredAccount(id, ct);
        return RangeMetricsCalculator.Summarize(id, null, null, null, InsightLevel.Account, since, until,
            await repository.ListAsync(id, InsightLevel.Account, null, since, until, ct));
    }

    public async Task<RangeMetricsSummaryModel> GetCampaignSummaryAsync(Guid id, DateOnly since, DateOnly until, CancellationToken ct = default)
    {
        ValidateRange(since, until);
        var campaign = await RequiredCampaign(id, ct);
        return RangeMetricsCalculator.Summarize(campaign.AdAccountId, campaign.Id, null, null, InsightLevel.Campaign, since, until,
            await repository.ListAsync(campaign.AdAccountId, InsightLevel.Campaign, id, since, until, ct));
    }

    public async Task<RangeMetricsSummaryModel> GetAdSetSummaryAsync(Guid id, DateOnly since, DateOnly until, CancellationToken ct = default)
    {
        ValidateRange(since, until);
        var adSet = await RequiredAdSet(id, ct);
        var campaign = await RequiredCampaign(adSet.CampaignId, ct);
        return RangeMetricsCalculator.Summarize(campaign.AdAccountId, campaign.Id, adSet.Id, null, InsightLevel.AdSet, since, until,
            await repository.ListAsync(campaign.AdAccountId, InsightLevel.AdSet, id, since, until, ct));
    }

    public async Task<RangeMetricsSummaryModel> GetAdSummaryAsync(Guid id, DateOnly since, DateOnly until, CancellationToken ct = default)
    {
        ValidateRange(since, until);
        var ad = await advertising.GetAdAsync(id, ct) ?? throw new EntityNotFoundException("Ad was not found.");
        var adSet = await RequiredAdSet(ad.AdSetId, ct);
        var campaign = await RequiredCampaign(adSet.CampaignId, ct);
        return RangeMetricsCalculator.Summarize(campaign.AdAccountId, campaign.Id, adSet.Id, ad.Id, InsightLevel.Ad, since, until,
            await repository.ListAsync(campaign.AdAccountId, InsightLevel.Ad, id, since, until, ct));
    }

    public async Task<CampaignSelectorModel> ListCampaignSelectorAsync(Guid accountId, DateOnly since, DateOnly until, CampaignActivityFilter activityFilter, CancellationToken ct = default)
    {
        ValidateRange(since, until);
        await RequiredAccount(accountId, ct);
        var campaigns = await advertising.ListCampaignsAsync(accountId, false, ct);
        var snapshots = await repository.ListForAccountAsync(accountId, InsightLevel.Campaign, since, until, ct);
        var snapshotsByCampaign = snapshots
            .Where(x => x.CampaignId.HasValue)
            .GroupBy(x => x.CampaignId!.Value)
            .ToDictionary(x => x.Key, x => (IReadOnlyList<InsightSnapshot>)x.OrderBy(y => y.SnapshotDate).ToArray());
        var items = new List<CampaignSelectorItemModel>();
        foreach (var campaign in campaigns)
        {
            var campaignSnapshots = snapshotsByCampaign.GetValueOrDefault(campaign.Id) ?? Array.Empty<InsightSnapshot>();
            var summary = RangeMetricsCalculator.Summarize(accountId, campaign.Id, null, null, InsightLevel.Campaign, since, until, campaignSnapshots);
            var hasActivity = RangeMetricsCalculator.HasActivity(campaignSnapshots);
            var hasObservedSpend = RangeMetricsCalculator.HasObservedSpend(campaignSnapshots);
            if (activityFilter == CampaignActivityFilter.WithActivity && !hasActivity) continue;
            if (activityFilter == CampaignActivityFilter.WithSpend && !hasObservedSpend) continue;
            items.Add(new(campaign.Id, campaign.Name, campaign.Objective, campaign.ConfiguredStatus, campaign.EffectiveStatus, campaign.IsPresentOnMeta,
                hasActivity, hasObservedSpend, summary.Currency, summary.CurrencyStatus, summary.Coverage, summary.Observed.Spend));
        }
        return new(accountId, since, until, activityFilter, items
            .OrderByDescending(x => x.HasObservedSpend)
            .ThenByDescending(x => x.HasActivity)
            .ThenBy(x => x.Name, StringComparer.OrdinalIgnoreCase)
            .ToArray());
    }

    public Task<RangeMetricsComparisonModel> CompareAccountAsync(Guid id, DateOnly since, DateOnly until, ComparisonPeriodType type, DateOnly? comparisonSince, DateOnly? comparisonUntil, CancellationToken ct = default) =>
        Compare(since, until, type, comparisonSince, comparisonUntil, (from, to) => GetAccountSummaryAsync(id, from, to, ct));
    public Task<RangeMetricsComparisonModel> CompareCampaignAsync(Guid id, DateOnly since, DateOnly until, ComparisonPeriodType type, DateOnly? comparisonSince, DateOnly? comparisonUntil, CancellationToken ct = default) =>
        Compare(since, until, type, comparisonSince, comparisonUntil, (from, to) => GetCampaignSummaryAsync(id, from, to, ct));
    public Task<RangeMetricsComparisonModel> CompareAdSetAsync(Guid id, DateOnly since, DateOnly until, ComparisonPeriodType type, DateOnly? comparisonSince, DateOnly? comparisonUntil, CancellationToken ct = default) =>
        Compare(since, until, type, comparisonSince, comparisonUntil, (from, to) => GetAdSetSummaryAsync(id, from, to, ct));
    public Task<RangeMetricsComparisonModel> CompareAdAsync(Guid id, DateOnly since, DateOnly until, ComparisonPeriodType type, DateOnly? comparisonSince, DateOnly? comparisonUntil, CancellationToken ct = default) =>
        Compare(since, until, type, comparisonSince, comparisonUntil, (from, to) => GetAdSummaryAsync(id, from, to, ct));

    public async Task<CampaignBenchmarksModel> GetCampaignBenchmarksAsync(Guid accountId, DateOnly since, DateOnly until, CancellationToken ct = default)
    {
        ValidateRange(since, until); await RequiredAccount(accountId, ct);
        var campaigns = await advertising.ListCampaignsAsync(accountId, false, ct);
        var snapshots = await repository.ListForAccountAsync(accountId, InsightLevel.Campaign, since, until, ct);
        var summaries = campaigns.ToDictionary(x => x.Id, x => RangeMetricsCalculator.Summarize(accountId, x.Id, null, null, InsightLevel.Campaign, since, until, snapshots.Where(y => y.CampaignId == x.Id).ToArray()));
        var items = campaigns.Select(c =>
        {
            var own = summaries[c.Id]; var peers = campaigns.Where(x => x.Id != c.Id && string.Equals(x.Objective, c.Objective, StringComparison.OrdinalIgnoreCase)).Select(x => summaries[x.Id]).ToArray();
            IReadOnlyList<(DecimalRangeMetricModel Metric, string? Currency)> Values(Func<RangeMetricsSummaryModel, DecimalRangeMetricModel> select) => peers.Select(x => (select(x), x.Currency)).ToArray();
            return new CampaignBenchmarkItemModel(c.Id, c.Name, c.Objective, own.Currency,
                CampaignBenchmarkCalculator.Calculate(own.Derived.Cpm, Values(x => x.Derived.Cpm), own.Currency, true),
                CampaignBenchmarkCalculator.Calculate(own.Derived.Ctr, Values(x => x.Derived.Ctr), own.Currency, false),
                CampaignBenchmarkCalculator.Calculate(own.Derived.Cpc, Values(x => x.Derived.Cpc), own.Currency, true),
                CampaignBenchmarkCalculator.Calculate(own.Derived.Cpl, Values(x => x.Derived.Cpl), own.Currency, true),
                CampaignBenchmarkCalculator.Calculate(own.Derived.Cpa, Values(x => x.Derived.Cpa), own.Currency, true),
                CampaignBenchmarkCalculator.Calculate(own.Derived.Roas, Values(x => x.Derived.Roas), own.Currency, false));
        }).ToArray();
        return new(accountId, since, until, "MedianOfOtherCampaignsWithSameObjective", items);
    }

    private async Task<RangeMetricsComparisonModel> Compare(DateOnly since, DateOnly until, ComparisonPeriodType type, DateOnly? customSince, DateOnly? customUntil, Func<DateOnly, DateOnly, Task<RangeMetricsSummaryModel>> summary)
    {
        ValidateRange(since, until); var (baselineSince, baselineUntil) = ResolveComparisonRange(since, until, type, customSince, customUntil); ValidateRange(baselineSince, baselineUntil);
        if (baselineUntil >= since) throw new ArgumentException("The comparison range must end before the current range starts.");
        var current = await summary(since, until); var baseline = await summary(baselineSince, baselineUntil);
        return RangeComparisonCalculator.Compare(type, current, baseline);
    }

    private static (DateOnly Since, DateOnly Until) ResolveComparisonRange(DateOnly since, DateOnly until, ComparisonPeriodType type, DateOnly? customSince, DateOnly? customUntil)
    {
        var days = until.DayNumber - since.DayNumber + 1;
        return type switch
        {
            ComparisonPeriodType.PreviousPeriod => (since.AddDays(-days), since.AddDays(-1)),
            ComparisonPeriodType.PreviousMonth => (new DateOnly(since.Year, since.Month, 1).AddMonths(-1), new DateOnly(since.Year, since.Month, 1).AddDays(-1)),
            ComparisonPeriodType.PreviousYear => (since.AddYears(-1), until.AddYears(-1)),
            ComparisonPeriodType.Custom when customSince.HasValue && customUntil.HasValue => (customSince.Value, customUntil.Value),
            ComparisonPeriodType.Custom => throw new ArgumentException("'comparisonSince' and 'comparisonUntil' are required for Custom comparison."),
            _ => throw new ArgumentException("Unsupported comparison type.")
        };
    }

    private async Task<Campaign> RequiredCampaign(Guid id, CancellationToken ct) => await advertising.GetCampaignAsync(id, ct) ?? throw new EntityNotFoundException("Campaign was not found.");
    private async Task<AdSet> RequiredAdSet(Guid id, CancellationToken ct) => await advertising.GetAdSetAsync(id, ct) ?? throw new EntityNotFoundException("Ad set was not found.");
    private async Task<AdAccount> RequiredAccount(Guid id, CancellationToken ct) => await accounts.GetByIdAsync(id, false, ct) ?? throw new EntityNotFoundException($"Ad account '{id}' was not found.");
    private static (Guid? campaign, Guid? adSet, Guid? ad) Resolve(RemoteInsight value, IReadOnlyDictionary<string, Campaign> campaigns, IReadOnlyDictionary<string, AdSet> adSets, IReadOnlyDictionary<string, Ad> ads)
    {
        if (value.Level == InsightLevel.Account) return (null, null, null);
        if (value.Level == InsightLevel.Campaign && value.MetaCampaignId is not null && campaigns.TryGetValue(value.MetaCampaignId, out var campaign))
            return (campaign.Id, null, null);
        if (value.Level == InsightLevel.AdSet && value.MetaCampaignId is not null && value.MetaAdSetId is not null &&
            campaigns.TryGetValue(value.MetaCampaignId, out campaign) && adSets.TryGetValue(value.MetaAdSetId, out var adSet) && adSet.CampaignId == campaign.Id)
            return (campaign.Id, adSet.Id, null);
        if (value.Level == InsightLevel.Ad && value.MetaCampaignId is not null && value.MetaAdSetId is not null && value.MetaAdId is not null &&
            campaigns.TryGetValue(value.MetaCampaignId, out campaign) && adSets.TryGetValue(value.MetaAdSetId, out adSet) && ads.TryGetValue(value.MetaAdId, out var ad) &&
            adSet.CampaignId == campaign.Id && ad.AdSetId == adSet.Id)
            return (campaign.Id, adSet.Id, ad.Id);
        throw new ExternalServiceException("Meta returned insights for an entity that has not been synchronized or has an inconsistent hierarchy.");
    }

    private void ValidateRange(DateOnly since, DateOnly until)
    {
        if (since == DateOnly.MinValue || until == DateOnly.MinValue) throw new ArgumentException("'since' and 'until' are required.");
        if (since > until) throw new ArgumentException("'since' must be on or before 'until'.");
        if (until.DayNumber - since.DayNumber + 1 > 90) throw new ArgumentException("The date range cannot exceed 90 days.");
        if (until > DateOnly.FromDateTime(clock.GetUtcNow().UtcDateTime)) throw new ArgumentException("The date range cannot end in the future.");
    }

    private static IReadOnlyList<InsightSnapshotModel> Map(IReadOnlyList<InsightSnapshot> values) => values.Select(x => new InsightSnapshotModel(x.Id, x.AdAccountId, x.CampaignId, x.AdSetId, x.AdId, x.Level.ToString(), x.SnapshotDate, x.Currency, x.ObservedDataQuality, new(x.Spend, x.Impressions, x.Reach, x.LinkClicks, x.Leads, x.Purchases, x.PurchaseValue), new(x.Frequency, x.Cpm, x.Ctr, x.Cpc, x.Cpl, x.Cpa, x.Roas), x.ObservedAtUtc)).ToArray();
}
