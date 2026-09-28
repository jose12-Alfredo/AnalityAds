namespace AnaliticAsd.Domain.DataSources;

public sealed class ProviderMetricSnapshot
{
    private ProviderMetricSnapshot() { }
    public Guid Id { get; private set; }
    public Guid AgencyId { get; private set; }
    public Guid DataSourceId { get; private set; }
    public DateOnly Date { get; private set; }
    public string DimensionKey { get; private set; } = string.Empty;
    public string DimensionName { get; private set; } = string.Empty;
    public decimal? Spend { get; private set; }
    public long? Impressions { get; private set; }
    public long? Clicks { get; private set; }
    public decimal? Conversions { get; private set; }
    public decimal? ConversionValue { get; private set; }
    public long? ActiveUsers { get; private set; }
    public long? Sessions { get; private set; }
    public long? Views { get; private set; }
    public DateTimeOffset ObservedAtUtc { get; private set; }

    public static ProviderMetricSnapshot Create(Guid agencyId, Guid sourceId, DateOnly date, string key,
        string name, DateTimeOffset now) => new() { Id = Guid.NewGuid(), AgencyId = agencyId,
        DataSourceId = sourceId, Date = date, DimensionKey = key.Trim(), DimensionName = name.Trim(), ObservedAtUtc = now.ToUniversalTime() };
    public void Replace(string name, decimal? spend, long? impressions, long? clicks, decimal? conversions,
        decimal? conversionValue, long? activeUsers, long? sessions, long? views, DateTimeOffset now)
    {
        DimensionName = name; Spend = spend; Impressions = impressions; Clicks = clicks;
        Conversions = conversions; ConversionValue = conversionValue; ActiveUsers = activeUsers;
        Sessions = sessions; Views = views; ObservedAtUtc = now.ToUniversalTime();
    }
}
