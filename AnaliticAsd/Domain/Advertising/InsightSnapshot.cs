namespace AnaliticAsd.Domain.Advertising;

public enum InsightLevel { Account, Campaign, AdSet, Ad }
public enum InsightSnapshotDataQuality { LegacyZeroNormalized, FieldPresencePreserved }

public sealed class InsightSnapshot
{
    private InsightSnapshot() { }
    public Guid Id { get; private set; }
    public Guid AdAccountId { get; private set; }
    public Guid? CampaignId { get; private set; }
    public Guid? AdSetId { get; private set; }
    public Guid? AdId { get; private set; }
    public InsightLevel Level { get; private set; }
    public DateOnly SnapshotDate { get; private set; }
    public string Currency { get; private set; } = string.Empty;
    public decimal? Spend { get; private set; }
    public long? Impressions { get; private set; }
    public long? Reach { get; private set; }
    public long? LinkClicks { get; private set; }
    public decimal? Leads { get; private set; }
    public decimal? Purchases { get; private set; }
    public decimal? PurchaseValue { get; private set; }
    public decimal? Frequency { get; private set; }
    public decimal? Cpm { get; private set; }
    public decimal? Ctr { get; private set; }
    public decimal? Cpc { get; private set; }
    public decimal? Cpl { get; private set; }
    public decimal? Cpa { get; private set; }
    public decimal? Roas { get; private set; }
    public InsightSnapshotDataQuality ObservedDataQuality { get; private set; }
    public DateTimeOffset ObservedAtUtc { get; private set; }

    public static InsightSnapshot Create(Guid accountId, Guid? campaignId, Guid? adSetId, Guid? adId, InsightLevel level, RemoteInsight value, string currency, DateTimeOffset observedAtUtc)
    {
        var entity = new InsightSnapshot { Id = Guid.NewGuid(), AdAccountId = accountId, CampaignId = campaignId, AdSetId = adSetId, AdId = adId, Level = level, SnapshotDate = value.Date };
        entity.Update(value, currency, observedAtUtc);
        return entity;
    }

    public void Update(RemoteInsight value, string currency, DateTimeOffset observedAtUtc)
    {
        if (value.Date != SnapshotDate) throw new ArgumentException("Snapshot date cannot change.");
        Currency = CurrencyCode.Parse(currency).Value;
        Spend = NonNegative(value.Spend, nameof(value.Spend)); Impressions = NonNegative(value.Impressions, nameof(value.Impressions)); Reach = NonNegative(value.Reach, nameof(value.Reach)); LinkClicks = NonNegative(value.LinkClicks, nameof(value.LinkClicks));
        Leads = NonNegative(value.Leads, nameof(value.Leads)); Purchases = NonNegative(value.Purchases, nameof(value.Purchases)); PurchaseValue = NonNegative(value.PurchaseValue, nameof(value.PurchaseValue));
        Frequency = Divide(Impressions, Reach); Cpm = Ratio(Spend, Impressions, 1000m); Ctr = Ratio(LinkClicks, Impressions, 100m); Cpc = Ratio(Spend, LinkClicks); Cpl = Ratio(Spend, Leads); Cpa = Ratio(Spend, Purchases); Roas = Ratio(PurchaseValue, Spend);
        ObservedDataQuality = InsightSnapshotDataQuality.FieldPresencePreserved;
        ObservedAtUtc = observedAtUtc.ToUniversalTime();
    }

    internal void MarkLegacyZeroNormalized() => ObservedDataQuality = InsightSnapshotDataQuality.LegacyZeroNormalized;

    private static decimal? Divide(long? numerator, long? denominator) => numerator.HasValue && denominator is > 0 ? numerator.Value / (decimal)denominator.Value : null;
    private static decimal? Ratio(decimal? numerator, long? denominator, decimal factor = 1m) => numerator.HasValue && denominator is > 0 ? numerator.Value / denominator.Value * factor : null;
    private static decimal? Ratio(long? numerator, long? denominator, decimal factor = 1m) => numerator.HasValue && denominator is > 0 ? numerator.Value / (decimal)denominator.Value * factor : null;
    private static decimal? Ratio(decimal? numerator, decimal? denominator, decimal factor = 1m) => numerator.HasValue && denominator is > 0m ? numerator.Value / denominator.Value * factor : null;
    private static decimal? NonNegative(decimal? value, string name) => value is null || value >= 0m ? value : throw new ArgumentOutOfRangeException(name, "Observed metrics cannot be negative.");
    private static long? NonNegative(long? value, string name) => value is null || value >= 0 ? value : throw new ArgumentOutOfRangeException(name, "Observed metrics cannot be negative.");
}

public sealed record RemoteInsight(InsightLevel Level, DateOnly Date, string? MetaCampaignId, string? MetaAdSetId, string? MetaAdId, decimal? Spend, long? Impressions, long? Reach, long? LinkClicks, decimal? Leads, decimal? Purchases, decimal? PurchaseValue);
