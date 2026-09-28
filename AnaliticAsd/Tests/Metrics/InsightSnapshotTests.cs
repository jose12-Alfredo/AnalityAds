using AnaliticAsd.Domain.Advertising;

namespace AnaliticAsd.Tests.Metrics;

public sealed class InsightSnapshotTests
{
    [Fact]
    public void Separates_observed_values_and_calculates_derived_metrics()
    {
        var remote = new RemoteInsight(InsightLevel.Account, new DateOnly(2026, 9, 1), null, null, null, 100m, 10000, 4000, 200, 10m, 4m, 500m);
        var value = InsightSnapshot.Create(Guid.NewGuid(), null, null, null, InsightLevel.Account, remote, "usd", DateTimeOffset.Parse("2026-09-02T03:00:00-04:00"));
        Assert.Equal(100m, value.Spend); Assert.Equal(10000, value.Impressions); Assert.Equal(2.5m, value.Frequency); Assert.Equal(10m, value.Cpm); Assert.Equal(2m, value.Ctr); Assert.Equal(.5m, value.Cpc); Assert.Equal(10m, value.Cpl); Assert.Equal(25m, value.Cpa); Assert.Equal(5m, value.Roas); Assert.Equal(TimeSpan.Zero, value.ObservedAtUtc.Offset);
    }

    [Fact]
    public void Missing_denominators_produce_null_instead_of_invalid_numbers()
    {
        var remote = new RemoteInsight(InsightLevel.Account, new DateOnly(2026, 9, 1), null, null, null, 0m, 0, 0, 0, 0m, 0m, 0m);
        var value = InsightSnapshot.Create(Guid.NewGuid(), null, null, null, InsightLevel.Account, remote, "USD", DateTimeOffset.UtcNow);
        Assert.Null(value.Frequency); Assert.Null(value.Cpm); Assert.Null(value.Ctr); Assert.Null(value.Cpc); Assert.Null(value.Cpl); Assert.Null(value.Cpa); Assert.Null(value.Roas);
    }

    [Fact]
    public void Missing_observed_fields_remain_missing_and_do_not_produce_derived_values()
    {
        var remote = new RemoteInsight(InsightLevel.Account, new DateOnly(2026, 9, 1), null, null, null, null, null, null, null, null, null, null);
        var value = InsightSnapshot.Create(Guid.NewGuid(), null, null, null, InsightLevel.Account, remote, "USD", DateTimeOffset.UtcNow);

        Assert.Null(value.Spend); Assert.Null(value.Impressions); Assert.Null(value.Reach); Assert.Null(value.LinkClicks);
        Assert.Null(value.Leads); Assert.Null(value.Purchases); Assert.Null(value.PurchaseValue);
        Assert.Null(value.Frequency); Assert.Null(value.Cpm); Assert.Null(value.Ctr); Assert.Null(value.Cpc); Assert.Null(value.Cpl); Assert.Null(value.Cpa); Assert.Null(value.Roas);
        Assert.Equal(InsightSnapshotDataQuality.FieldPresencePreserved, value.ObservedDataQuality);
    }

    [Fact]
    public void Explicit_zero_remains_an_observed_zero()
    {
        var remote = new RemoteInsight(InsightLevel.Account, new DateOnly(2026, 9, 1), null, null, null, 0m, 0, 0, 0, 0m, 0m, 0m);
        var value = InsightSnapshot.Create(Guid.NewGuid(), null, null, null, InsightLevel.Account, remote, "USD", DateTimeOffset.UtcNow);

        Assert.Equal(0m, value.Spend); Assert.Equal(0, value.Impressions); Assert.Equal(0, value.Reach); Assert.Equal(0, value.LinkClicks);
        Assert.Equal(0m, value.Leads); Assert.Equal(0m, value.Purchases); Assert.Equal(0m, value.PurchaseValue);
        Assert.Equal(InsightSnapshotDataQuality.FieldPresencePreserved, value.ObservedDataQuality);
    }
}
