using AnaliticAsd.Contracts;
using AnaliticAsd.Controllers;
using Microsoft.AspNetCore.Mvc;

namespace AnaliticAsd.Tests;

public sealed class SystemControllerTests
{
    [Fact]
    public void Status_returns_service_information_and_utc_timestamp()
    {
        var timestamp = new DateTimeOffset(2026, 9, 3, 12, 30, 0, TimeSpan.Zero);
        var controller = new SystemController(new FixedTimeProvider(timestamp));

        var action = controller.GetStatus();

        var ok = Assert.IsType<OkObjectResult>(action.Result);
        var response = Assert.IsType<SystemStatusResponse>(ok.Value);
        Assert.Equal("AnaliticAsd.Api", response.Service);
        Assert.Equal("Healthy", response.Status);
        Assert.Equal(timestamp, response.TimestampUtc);
        Assert.NotEmpty(response.Version);
    }

    private sealed class FixedTimeProvider(DateTimeOffset utcNow) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => utcNow;
    }
}
