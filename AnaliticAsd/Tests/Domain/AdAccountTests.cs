using AnaliticAsd.Domain.Advertising;

namespace AnaliticAsd.Tests.Domain;

public sealed class AdAccountTests
{
    [Fact]
    public void Create_normalizes_meta_id_and_currency()
    {
        var account = AdAccount.Create(
            Guid.NewGuid(),
            Guid.NewGuid(),
            MetaAdAccountId.Parse("act_123456789"),
            " Main Account ",
            CurrencyCode.Parse("usd"),
            MetaTimeZoneId.Parse("America/La_Paz"),
            DateTimeOffset.UtcNow);

        Assert.Equal("123456789", account.MetaAccountId.Value);
        Assert.Equal("USD", account.Currency.Value);
        Assert.Equal("Main Account", account.Name);
        Assert.Equal(AdAccountConnectionStatus.Disconnected, account.ConnectionStatus);
        Assert.Equal(account.Id, account.DataSourceId);
        Assert.Equal("123456789", account.DataSource.ExternalId);
    }

    [Theory]
    [InlineData("act_")]
    [InlineData("123-456")]
    [InlineData("account")]
    public void Meta_account_id_rejects_non_numeric_values(string value)
    {
        Assert.Throws<ArgumentException>(() => MetaAdAccountId.Parse(value));
    }

    [Theory]
    [InlineData("US")]
    [InlineData("USDD")]
    [InlineData("12A")]
    public void Currency_rejects_invalid_codes(string value)
    {
        Assert.Throws<ArgumentException>(() => CurrencyCode.Parse(value));
    }
}
