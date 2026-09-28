namespace AnaliticAsd.Domain.DataSources;

public sealed class ProviderSyncSchedule
{
    private ProviderSyncSchedule() { }
    public Guid Id { get; private set; }
    public Guid AgencyId { get; private set; }
    public Guid DataSourceId { get; private set; }
    public bool IsEnabled { get; private set; }
    public int IntervalMinutes { get; private set; }
    public int LookbackDays { get; private set; }
    public DateTimeOffset NextRunAtUtc { get; private set; }
    public DateTimeOffset? LockedUntilUtc { get; private set; }
    public int ConsecutiveFailures { get; private set; }
    public DateTimeOffset? LastStartedAtUtc { get; private set; }
    public DateTimeOffset? LastSucceededAtUtc { get; private set; }
    public string? LastErrorCode { get; private set; }

    public static ProviderSyncSchedule Create(Guid agencyId, Guid sourceId, int intervalMinutes, int lookbackDays, DateTimeOffset now)
    {
        Validate(intervalMinutes, lookbackDays); return new() { Id = Guid.NewGuid(), AgencyId = agencyId,
            DataSourceId = sourceId, IsEnabled = true, IntervalMinutes = intervalMinutes,
            LookbackDays = lookbackDays, NextRunAtUtc = now.ToUniversalTime() };
    }
    public void Configure(int intervalMinutes, int lookbackDays, bool enabled, DateTimeOffset now)
    { Validate(intervalMinutes, lookbackDays); IntervalMinutes = intervalMinutes; LookbackDays = lookbackDays; IsEnabled = enabled; if (enabled && NextRunAtUtc < now) NextRunAtUtc = now.ToUniversalTime(); }
    public bool TryLock(DateTimeOffset now)
    { if (!IsEnabled || NextRunAtUtc > now || LockedUntilUtc > now) return false; LockedUntilUtc = now.AddMinutes(15); LastStartedAtUtc = now; return true; }
    public void Complete(DateTimeOffset now) { LockedUntilUtc = null; ConsecutiveFailures = 0; LastErrorCode = null; LastSucceededAtUtc = now; NextRunAtUtc = now.AddMinutes(IntervalMinutes); }
    public void Fail(string code, DateTimeOffset now) { LockedUntilUtc = null; ConsecutiveFailures++; LastErrorCode = code[..Math.Min(code.Length, 100)]; NextRunAtUtc = now.AddMinutes(Math.Min(60, 1 << Math.Min(ConsecutiveFailures, 5))); }
    private static void Validate(int interval, int lookback) { if (interval is < 15 or > 10080) throw new ArgumentOutOfRangeException(nameof(interval)); if (lookback is < 1 or > 90) throw new ArgumentOutOfRangeException(nameof(lookback)); }
}
