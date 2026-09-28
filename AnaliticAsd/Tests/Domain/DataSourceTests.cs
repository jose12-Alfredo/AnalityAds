using AnaliticAsd.Domain.DataSources;

namespace AnaliticAsd.Tests.Domain;

public sealed class DataSourceTests
{
    [Theory]
    [InlineData(DataProvider.MetaAds)]
    [InlineData(DataProvider.GoogleAds)]
    [InlineData(DataProvider.TikTokAds)]
    public void Advertising_providers_create_advertising_account_sources(DataProvider provider)
    {
        var source = DataSource.Create(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), null, provider,
            DataSourceType.AdvertisingAccount, " external-id ", " Account ", "usd", "America/La_Paz",
            DateTimeOffset.UtcNow);

        Assert.Equal("external-id", source.ExternalId);
        Assert.Equal("Account", source.Name);
        Assert.Equal("USD", source.Currency);
        Assert.True(source.IsActive);
    }

    [Fact]
    public void Google_analytics_creates_an_analytics_property_source()
    {
        var source = DataSource.Create(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), null,
            DataProvider.GoogleAnalytics4, DataSourceType.AnalyticsProperty, "properties/123", "GA4", null,
            "America/La_Paz", DateTimeOffset.UtcNow);

        Assert.Equal(DataSourceType.AnalyticsProperty, source.SourceType);
        Assert.Null(source.Currency);
    }

    [Fact]
    public void Provider_and_source_type_must_be_compatible()
    {
        Assert.Throws<ArgumentException>(() => DataSource.Create(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(),
            null, DataProvider.GoogleAnalytics4, DataSourceType.AdvertisingAccount, "123", "GA4", null, "UTC",
            DateTimeOffset.UtcNow));
        Assert.Throws<ArgumentException>(() => DataSource.Create(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(),
            null, DataProvider.TikTokAds, DataSourceType.AnalyticsProperty, "123", "TikTok", "USD", "UTC",
            DateTimeOffset.UtcNow));
    }

    [Fact]
    public void Synchronization_expands_the_known_range_without_losing_history()
    {
        var source = DataSource.Create(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), null, DataProvider.GoogleAds,
            DataSourceType.AdvertisingAccount, "123", "Google Ads", "USD", "UTC", DateTimeOffset.UtcNow);

        source.RecordSynchronization(new DateOnly(2026, 2, 1), new DateOnly(2026, 2, 28),
            DateTimeOffset.UtcNow);
        source.RecordSynchronization(new DateOnly(2026, 1, 1), new DateOnly(2026, 2, 15),
            DateTimeOffset.UtcNow.AddMinutes(1));

        Assert.Equal(new DateOnly(2026, 1, 1), source.AvailableSince);
        Assert.Equal(new DateOnly(2026, 2, 28), source.AvailableUntil);
    }

    [Fact]
    public void Provider_connection_can_be_reauthorized_without_changing_identity()
    {
        var connection = ProviderConnection.Create(Guid.NewGuid(), DataProvider.MetaAds, "Meta", "protected-1",
            null, null, DateTimeOffset.UtcNow);
        var id = connection.Id;

        connection.RecordError("TOKEN_EXPIRED", DateTimeOffset.UtcNow.AddMinutes(1));
        connection.UpdateAuthorization("Meta Ads", "protected-2", "user-123", DateTimeOffset.UtcNow.AddDays(60),
            DateTimeOffset.UtcNow.AddMinutes(2));

        Assert.Equal(id, connection.Id);
        Assert.Equal(ProviderConnectionStatus.Connected, connection.Status);
        Assert.Equal("protected-2", connection.ProtectedCredentialPayload);
        Assert.Null(connection.LastErrorCode);
    }
}
