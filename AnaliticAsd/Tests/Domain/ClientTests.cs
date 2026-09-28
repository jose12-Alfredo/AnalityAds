using AnaliticAsd.Domain.Clients;

namespace AnaliticAsd.Tests.Domain;

public sealed class ClientTests
{
    [Fact]
    public void Create_normalizes_name_and_time()
    {
        var localTime = new DateTimeOffset(2026, 9, 3, 8, 0, 0, TimeSpan.FromHours(-4));

        var client = Client.Create(Guid.NewGuid(), "  C&P Bolivia  ", localTime);

        Assert.Equal("C&P Bolivia", client.Name);
        Assert.Equal(TimeSpan.Zero, client.CreatedAtUtc.Offset);
        Assert.True(client.IsActive);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_rejects_empty_names(string name)
    {
        Assert.Throws<ArgumentException>(() => Client.Create(Guid.NewGuid(), name, DateTimeOffset.UtcNow));
    }
}
