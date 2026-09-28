namespace AnaliticAsd.Domain.DataSources;

public enum DataProvider
{
    MetaAds,
    GoogleAds,
    TikTokAds,
    GoogleAnalytics4
}

public enum DataSourceType
{
    AdvertisingAccount,
    AnalyticsProperty
}

public enum ProviderConnectionStatus
{
    Pending,
    Connected,
    Expired,
    Revoked,
    Error
}
