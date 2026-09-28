using AnaliticAsd.Application.Common;
using AnaliticAsd.Application.Identity;
using AnaliticAsd.Domain.DataSources;
using AnaliticAsd.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace AnaliticAsd.Application.ProviderIntegrations;
public sealed record ProviderScheduleModel(Guid SourceId, bool IsEnabled, int IntervalMinutes, int LookbackDays,
    DateTimeOffset NextRunAtUtc, int ConsecutiveFailures, DateTimeOffset? LastSucceededAtUtc, string? LastErrorCode);
public sealed class ProviderScheduleService(AnalitiAdsDbContext db, ICurrentTenant tenant, TimeProvider clock)
{
    public async Task<ProviderScheduleModel> GetAsync(Guid sourceId, CancellationToken ct)
    {
        await RequiredSource(sourceId, ct); var value = await db.ProviderSyncSchedules.AsNoTracking().SingleOrDefaultAsync(x => x.DataSourceId == sourceId && x.AgencyId == tenant.AgencyId, ct);
        return value is null ? new(sourceId, false, 1440, 30, clock.GetUtcNow(), 0, null, null) : Map(value);
    }
    public async Task<ProviderScheduleModel> ConfigureAsync(Guid sourceId, int intervalMinutes, int lookbackDays, bool enabled, CancellationToken ct)
    {
        await RequiredSource(sourceId, ct); var value = await db.ProviderSyncSchedules.SingleOrDefaultAsync(x => x.DataSourceId == sourceId && x.AgencyId == tenant.AgencyId, ct);
        if (value is null) { value = ProviderSyncSchedule.Create(tenant.AgencyId, sourceId, intervalMinutes, lookbackDays, clock.GetUtcNow()); db.Add(value); }
        value.Configure(intervalMinutes, lookbackDays, enabled, clock.GetUtcNow()); await db.SaveChangesAsync(ct); return Map(value);
    }
    private async Task RequiredSource(Guid id, CancellationToken ct) { if (!await db.DataSources.AnyAsync(x => x.Id == id && x.AgencyId == tenant.AgencyId, ct)) throw new EntityNotFoundException("The data source was not found."); }
    private static ProviderScheduleModel Map(ProviderSyncSchedule x) => new(x.DataSourceId, x.IsEnabled, x.IntervalMinutes, x.LookbackDays, x.NextRunAtUtc, x.ConsecutiveFailures, x.LastSucceededAtUtc, x.LastErrorCode);
}
