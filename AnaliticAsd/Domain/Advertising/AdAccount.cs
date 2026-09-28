using AnaliticAsd.Domain.DataSources;

namespace AnaliticAsd.Domain.Advertising;

public sealed class AdAccount
{
    public const int MaxNameLength = 200;

    private AdAccount()
    {
    }

    private AdAccount(
        Guid id,
        Guid agencyId,
        Guid clientId,
        MetaAdAccountId metaAccountId,
        string name,
        CurrencyCode currency,
        MetaTimeZoneId timeZone,
        DateTimeOffset createdAtUtc,
        Guid? providerConnectionId)
    {
        if (clientId == Guid.Empty)
        {
            throw new ArgumentException("Client ID is required.", nameof(clientId));
        }

        if (agencyId == Guid.Empty)
        {
            throw new ArgumentException("Agency ID is required.", nameof(agencyId));
        }

        Id = id;
        ClientId = clientId;
        MetaAccountId = metaAccountId;
        Name = NormalizeName(name);
        Currency = currency;
        TimeZone = timeZone;
        ConnectionStatus = AdAccountConnectionStatus.Disconnected;
        IsActive = true;
        CreatedAtUtc = createdAtUtc.ToUniversalTime();
        UpdatedAtUtc = CreatedAtUtc;
        DataSourceId = id;
        DataSource = DataSource.Create(id, agencyId, clientId, providerConnectionId, DataProvider.MetaAds,
            DataSourceType.AdvertisingAccount, metaAccountId.Value, Name, currency.Value, timeZone.Value, createdAtUtc);
    }

    public Guid Id { get; private set; }
    public Guid ClientId { get; private set; }
    public Guid DataSourceId { get; private set; }
    public DataSource DataSource { get; private set; } = null!;
    public MetaAdAccountId MetaAccountId { get; private set; } = null!;
    public string Name { get; private set; } = string.Empty;
    public CurrencyCode Currency { get; private set; } = null!;
    public MetaTimeZoneId TimeZone { get; private set; } = null!;
    public AdAccountConnectionStatus ConnectionStatus { get; private set; }
    public bool IsActive { get; private set; }
    public DateTimeOffset CreatedAtUtc { get; private set; }
    public DateTimeOffset UpdatedAtUtc { get; private set; }

    public static AdAccount Create(
        Guid agencyId,
        Guid clientId,
        MetaAdAccountId metaAccountId,
        string name,
        CurrencyCode currency,
        MetaTimeZoneId timeZone,
        DateTimeOffset createdAtUtc,
        Guid? providerConnectionId = null)
    {
        ArgumentNullException.ThrowIfNull(metaAccountId);
        ArgumentNullException.ThrowIfNull(currency);
        ArgumentNullException.ThrowIfNull(timeZone);
        return new AdAccount(
            Guid.NewGuid(), agencyId, clientId, metaAccountId, name, currency, timeZone, createdAtUtc,
            providerConnectionId);
    }

    public void UpdateDetails(
        string name,
        CurrencyCode currency,
        MetaTimeZoneId timeZone,
        bool isActive,
        DateTimeOffset updatedAtUtc)
    {
        ArgumentNullException.ThrowIfNull(currency);
        ArgumentNullException.ThrowIfNull(timeZone);
        Name = NormalizeName(name);
        Currency = currency;
        TimeZone = timeZone;
        IsActive = isActive;
        UpdatedAtUtc = updatedAtUtc.ToUniversalTime();
        DataSource.UpdateDetails(Name, currency.Value, timeZone.Value, isActive, updatedAtUtc);
    }

    public void MarkConnected(DateTimeOffset updatedAtUtc)
    {
        ConnectionStatus = AdAccountConnectionStatus.Connected;
        UpdatedAtUtc = updatedAtUtc.ToUniversalTime();
    }

    private static string NormalizeName(string name)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        var normalized = name.Trim();
        if (normalized.Length > MaxNameLength)
        {
            throw new ArgumentException(
                $"Ad account name cannot exceed {MaxNameLength} characters.",
                nameof(name));
        }

        return normalized;
    }
}
