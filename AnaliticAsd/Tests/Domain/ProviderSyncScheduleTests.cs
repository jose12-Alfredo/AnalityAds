using AnaliticAsd.Domain.DataSources;
namespace AnaliticAsd.Tests.Domain;
public sealed class ProviderSyncScheduleTests
{
    [Fact]
    public void Failure_releases_lock_and_uses_bounded_exponential_retry()
    {
        var now = DateTimeOffset.Parse("2026-09-22T12:00:00Z");
        var schedule = ProviderSyncSchedule.Create(Guid.NewGuid(), Guid.NewGuid(), 60, 30, now);
        Assert.True(schedule.TryLock(now));
        schedule.Fail("TemporaryProviderFailure", now);
        Assert.Equal(1, schedule.ConsecutiveFailures);
        Assert.Equal(now.AddMinutes(2), schedule.NextRunAtUtc);
        Assert.True(schedule.TryLock(now.AddMinutes(2)));
    }

    [Fact]
    public void Success_schedules_next_interval_and_clears_failures()
    {
        var now = DateTimeOffset.Parse("2026-09-22T12:00:00Z");
        var schedule = ProviderSyncSchedule.Create(Guid.NewGuid(), Guid.NewGuid(), 60, 7, now);
        schedule.TryLock(now); schedule.Fail("failure", now); schedule.TryLock(now.AddMinutes(2));
        schedule.Complete(now.AddMinutes(2));
        Assert.Equal(0, schedule.ConsecutiveFailures);
        Assert.Equal(now.AddMinutes(62), schedule.NextRunAtUtc);
        Assert.Null(schedule.LastErrorCode);
    }
}
