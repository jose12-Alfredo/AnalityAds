namespace AnaliticAsd.Domain.DataSources;

public sealed class DataSource
{
    public const int MaxExternalIdLength = 300;
    public const int MaxNameLength = 200;
    public const int MaxTimeZoneLength = 100;

    private DataSource() { }

    public Guid Id { get; private set; }
    public Guid AgencyId { get; private set; }
    public Guid ClientId { get; private set; }
    public Guid? ProviderConnectionId { get; private set; }
    public DataProvider Provider { get; private set; }
    public DataSourceType SourceType { get; private set; }
    public string ExternalId { get; private set; } = string.Empty;
    public string Name { get; private set; } = string.Empty;
    public string? Currency { get; private set; }
    public string TimeZone { get; private set; } = string.Empty;
    public bool IsActive { get; private set; }
    public DateOnly? AvailableSince { get; private set; }
    public DateOnly? AvailableUntil { get; private set; }
    public DateTimeOffset? LastSyncedAtUtc { get; private set; }
    public DateTimeOffset? ArchivedAtUtc { get; private set; }
    public DateTimeOffset CreatedAtUtc { get; private set; }
    public DateTimeOffset UpdatedAtUtc { get; private set; }
    public Guid Version { get; private set; }

    public static DataSource Create(Guid id, Guid agencyId, Guid clientId, Guid? providerConnectionId,
        DataProvider provider, DataSourceType sourceType, string externalId, string name,
        string? currency, string timeZone, DateTimeOffset now)
    {
        if (id == Guid.Empty) throw new ArgumentException("Data source id is required.", nameof(id));
        if (agencyId == Guid.Empty) throw new ArgumentException("Agency id is required.", nameof(agencyId));
        if (clientId == Guid.Empty) throw new ArgumentException("Client id is required.", nameof(clientId));
        if (provider == DataProvider.GoogleAnalytics4 && sourceType != DataSourceType.AnalyticsProperty)
            throw new ArgumentException("Google Analytics 4 sources must be analytics properties.", nameof(sourceType));
        if (provider != DataProvider.GoogleAnalytics4 && sourceType != DataSourceType.AdvertisingAccount)
            throw new ArgumentException("Advertising providers must use advertising account sources.", nameof(sourceType));

        var utcNow = now.ToUniversalTime();
        return new DataSource
        {
            Id = id,
            AgencyId = agencyId,
            ClientId = clientId,
            ProviderConnectionId = providerConnectionId,
            Provider = provider,
            SourceType = sourceType,
            ExternalId = NormalizeRequired(externalId, MaxExternalIdLength, nameof(externalId)),
            Name = NormalizeRequired(name, MaxNameLength, nameof(name)),
            Currency = NormalizeCurrency(currency),
            TimeZone = NormalizeRequired(timeZone, MaxTimeZoneLength, nameof(timeZone)),
            IsActive = true,
            CreatedAtUtc = utcNow,
            UpdatedAtUtc = utcNow,
            Version = Guid.NewGuid()
        };
    }

    public void UpdateDetails(string name, string? currency, string timeZone, bool isActive, DateTimeOffset now)
    {
        Name = NormalizeRequired(name, MaxNameLength, nameof(name));
        Currency = NormalizeCurrency(currency);
        TimeZone = NormalizeRequired(timeZone, MaxTimeZoneLength, nameof(timeZone));
        IsActive = isActive;
        Touch(now);
    }

    public void RecordSynchronization(DateOnly since, DateOnly until, DateTimeOffset now)
    {
        if (since > until) throw new ArgumentException("Synchronization range is invalid.");
        AvailableSince = AvailableSince is null || since < AvailableSince ? since : AvailableSince;
        AvailableUntil = AvailableUntil is null || until > AvailableUntil ? until : AvailableUntil;
        LastSyncedAtUtc = now.ToUniversalTime();
        Touch(now);
    }

    private void Touch(DateTimeOffset now)
    {
        UpdatedAtUtc = now.ToUniversalTime();
        Version = Guid.NewGuid();
    }

    private static string NormalizeRequired(string value, int maxLength, string parameter)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(value, parameter);
        var normalized = value.Trim();
        if (normalized.Length > maxLength) throw new ArgumentException($"{parameter} cannot exceed {maxLength} characters.", parameter);
        return normalized;
    }

    private static string? NormalizeCurrency(string? value)
    {
        if (string.IsNullOrWhiteSpace(value)) return null;
        var normalized = value.Trim().ToUpperInvariant();
        if (normalized.Length != 3 || normalized.Any(character => character is < 'A' or > 'Z'))
            throw new ArgumentException("Currency must be a three-letter ISO-style code.", nameof(value));
        return normalized;
    }
}
