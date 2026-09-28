using AnaliticAsd.Application.Common;
using AnaliticAsd.Application.DashboardQueries;
using AnaliticAsd.Domain.Advertising;
using AnaliticAsd.Domain.DataSources;

namespace AnaliticAsd.Tests.DashboardQueries;

public sealed class DashboardQueryServiceTests
{
    private static readonly Guid AgencyId = Guid.Parse("10000000-0000-0000-0000-000000000001");
    private static readonly Guid ClientId = Guid.Parse("20000000-0000-0000-0000-000000000002");
    private static readonly Guid SourceId = Guid.Parse("30000000-0000-0000-0000-000000000003");
    private static readonly DateOnly Day1 = new(2026, 9, 1);
    private static readonly DateTimeOffset Now = new(2026, 9, 21, 12, 0, 0, TimeSpan.Zero);

    [Fact]
    public void Catalog_DescribesNonAdditiveReach()
    {
        var catalog = DashboardDataCatalog.Get("MetaAds");
        var reach = Assert.Single(catalog.Metrics, metric => metric.Key == "reach");
        Assert.Equal("nonAdditive", reach.Aggregation);
        Assert.Equal(["date"], reach.SupportedDimensions);
        Assert.Contains("No se suma", reach.Limitation);
    }

    [Fact]
    public async Task Query_Total_RecomputesRatiosFromPeriodTotals()
    {
        var repository = Repository(
            Snapshot(Day1, 10m, 100, 10),
            Snapshot(Day1.AddDays(1), 90m, 900, 45));
        var result = await Service(repository).QueryAsync(Command(["spend", "impressions", "linkClicks", "ctr", "cpc"]));
        var metrics = Assert.Single(result.Rows).Metrics;
        Assert.Equal(100m, metrics["spend"].Value);
        Assert.Equal(1000m, metrics["impressions"].Value);
        Assert.Equal(55m, metrics["linkClicks"].Value);
        Assert.Equal(5.5m, metrics["ctr"].Value);
        Assert.Equal(100m / 55m, metrics["cpc"].Value);
    }

    [Fact]
    public async Task Query_ZeroDenominator_IsUndefinedAndNotZero()
    {
        var result = await Service(Repository(Snapshot(Day1, 10m, 100, 0)))
            .QueryAsync(Command(["cpc"]));
        var metric = Assert.Single(result.Rows).Metrics["cpc"];
        Assert.Null(metric.Value);
        Assert.Equal("Undefined", metric.Availability);
    }

    [Fact]
    public async Task Query_MixedCurrency_DoesNotSumSpend()
    {
        var first = Snapshot(Day1, 10m, 100, 10, "USD");
        var second = Snapshot(Day1.AddDays(1), 20m, 100, 10, "BOB");
        var result = await Service(Repository(first, second)).QueryAsync(Command(["spend"]));
        Assert.Null(result.Currency);
        Assert.Null(Assert.Single(result.Rows).Metrics["spend"].Value);
        Assert.Equal("MixedCurrency", Assert.Single(result.Rows).Metrics["spend"].Availability);
    }

    [Fact]
    public async Task Query_RejectsReachWithoutDateDimension()
    {
        var error = await Assert.ThrowsAsync<ArgumentException>(() =>
            Service(Repository()).QueryAsync(Command(["reach"])));
        Assert.Contains("not compatible", error.Message);
    }

    [Fact]
    public async Task Query_HidesSourceOutsideAuthorizedClient()
    {
        var repository = Repository(); repository.HideSource = true;
        await Assert.ThrowsAsync<EntityNotFoundException>(() =>
            Service(repository).QueryAsync(Command(["spend"])));
    }

    [Fact]
    public async Task Query_RejectsCampaignFromAnotherSource()
    {
        var repository = Repository();
        var command = Command(["spend"]) with { CampaignIds = [Guid.NewGuid()] };
        var error = await Assert.ThrowsAsync<ArgumentException>(() => Service(repository).QueryAsync(command));
        Assert.Contains("does not belong", error.Message);
    }

    [Fact]
    public async Task Query_RejectsSortMetricThatIsNotSelected()
    {
        var command = Command(["spend"]) with { SortMetric = "impressions", SortDirection = "desc" };
        var error = await Assert.ThrowsAsync<ArgumentException>(() => Service(Repository()).QueryAsync(command));
        Assert.Contains("included", error.Message);
    }

    [Fact]
    public async Task Query_CampaignRows_RespectAscendingMetricOrder()
    {
        var firstId = Guid.NewGuid(); var secondId = Guid.NewGuid();
        var repository = Repository(
            CampaignSnapshot(firstId, Day1, 20m), CampaignSnapshot(secondId, Day1, 5m));
        repository.Campaigns = new Dictionary<Guid, string> { [firstId] = "Mayor", [secondId] = "Menor" };
        var command = Command(["spend"]) with { Dimension = "campaign", SortMetric = "spend", SortDirection = "asc" };

        var result = await Service(repository).QueryAsync(command);

        Assert.Equal(["Menor", "Mayor"], result.Rows.Select(row => row.Label));
    }

    [Fact]
    public async Task Query_GoogleAds_ComputesRatiosFromPeriodTotals()
    {
        var source = DataSource.Create(SourceId, AgencyId, ClientId, null, DataProvider.GoogleAds,
            DataSourceType.AdvertisingAccount, "123", "Google", "USD", "UTC", Now);
        var repository = new FakeRepository(source, []);
        var row = ProviderMetricSnapshot.Create(AgencyId, SourceId, Day1, "campaign-1", "Campaign", Now);
        row.Replace("Campaign", 50m, 1000, 100, 5m, 200m, null, null, null, Now);
        repository.ProviderSnapshots = [row];
        var command = Command(["ctr", "cpc", "roas"]) with { Dimension = "none" };

        var result = await Service(repository).QueryAsync(command);

        var metrics = Assert.Single(result.Rows).Metrics;
        Assert.Equal(10m, metrics["ctr"].Value);
        Assert.Equal(.5m, metrics["cpc"].Value);
        Assert.Equal(4m, metrics["roas"].Value);
    }

    private static DashboardQueryService Service(FakeRepository repository) => new(repository, new FixedTimeProvider(Now));
    private static DashboardQueryCommand Command(IReadOnlyList<string> metrics) =>
        new(ClientId, SourceId, Day1, Day1.AddDays(1), "none", metrics, null, null, null, null);
    private static FakeRepository Repository(params InsightSnapshot[] snapshots) => new(Source(), snapshots);
    private static DataSource Source() => DataSource.Create(SourceId, AgencyId, ClientId, null,
        DataProvider.MetaAds, DataSourceType.AdvertisingAccount, "act_123", "Meta principal", "USD",
        "America/La_Paz", Now);
    private static InsightSnapshot Snapshot(DateOnly date, decimal? spend, long? impressions,
        long? clicks, string currency = "USD") => InsightSnapshot.Create(SourceId, null, null, null,
        InsightLevel.Account, new(InsightLevel.Account, date, null, null, null, spend, impressions,
            impressions, clicks, null, null, null), currency, Now);
    private static InsightSnapshot CampaignSnapshot(Guid campaignId, DateOnly date, decimal spend) =>
        InsightSnapshot.Create(SourceId, campaignId, null, null, InsightLevel.Campaign,
            new(InsightLevel.Campaign, date, "meta", null, null, spend, 100, null, 10, null, null, null),
            "USD", Now);

    private sealed class FixedTimeProvider(DateTimeOffset value) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => value;
    }

    private sealed class FakeRepository(DataSource source, IReadOnlyList<InsightSnapshot> snapshots)
        : IDashboardQueryRepository
    {
        public bool HideSource { get; set; }
        public IReadOnlyDictionary<Guid, string> Campaigns { get; set; } = new Dictionary<Guid, string>();
        public IReadOnlyList<ProviderMetricSnapshot> ProviderSnapshots { get; set; } = [];
        public Task<IReadOnlyList<DataSource>> ListSourcesAsync(Guid clientId, CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<DataSource>>(HideSource || clientId != source.ClientId ? [] : [source]);
        public Task<DataSource?> GetSourceAsync(Guid clientId, Guid sourceId, CancellationToken cancellationToken = default) =>
            Task.FromResult(HideSource || clientId != source.ClientId || sourceId != source.Id ? null : source);
        public Task<IReadOnlyList<InsightSnapshot>> ListSnapshotsAsync(Guid sourceId, InsightLevel level,
            DateOnly since, DateOnly until, CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<InsightSnapshot>>(snapshots.Where(item => item.AdAccountId == sourceId
                && item.Level == level && item.SnapshotDate >= since && item.SnapshotDate <= until).ToArray());
        public Task<IReadOnlyDictionary<Guid, string>> CampaignNamesAsync(Guid sourceId,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(Campaigns);
        public Task<IReadOnlyList<ProviderMetricSnapshot>> ListProviderSnapshotsAsync(Guid sourceId,
            DateOnly since, DateOnly until, CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<ProviderMetricSnapshot>>(ProviderSnapshots.Where(x => x.DataSourceId == sourceId && x.Date >= since && x.Date <= until).ToArray());
    }
}
