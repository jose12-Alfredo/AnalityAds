using AnaliticAsd.Application.ProviderIntegrations;
using AnaliticAsd.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace AnaliticAsd.Infrastructure.ProviderIntegrations;
public sealed class ProviderSyncWorker(IServiceScopeFactory scopes, TimeProvider clock, ILogger<ProviderSyncWorker> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(TimeSpan.FromMinutes(1));
        do
        {
            try { await RunDue(stoppingToken); }
            catch (PostgresException error) when (error.SqlState == PostgresErrorCodes.UndefinedTable)
            {
                logger.LogCritical("Scheduled synchronization was disabled because the database schema is outdated. Apply the pending EF Core migrations before using D5/D6 features. Missing relation: {Relation}.", error.TableName ?? "unknown");
                return;
            }
            catch (Exception error) { logger.LogError(error, "Provider sync worker iteration failed."); }
        }
        while (await timer.WaitForNextTickAsync(stoppingToken));
    }
    private async Task RunDue(CancellationToken ct)
    {
        await using var scope = scopes.CreateAsyncScope(); var db = scope.ServiceProvider.GetRequiredService<AnalitiAdsDbContext>(); var now = clock.GetUtcNow();
        var due = await db.ProviderSyncSchedules.Where(x => x.IsEnabled && x.NextRunAtUtc <= now && (x.LockedUntilUtc == null || x.LockedUntilUtc <= now)).OrderBy(x => x.NextRunAtUtc).Take(10).ToArrayAsync(ct);
        foreach (var job in due)
        {
            if (!job.TryLock(now)) continue; await db.SaveChangesAsync(ct);
            try { var until = DateOnly.FromDateTime(now.UtcDateTime); var since = until.AddDays(-(job.LookbackDays - 1)); await scope.ServiceProvider.GetRequiredService<IProviderSyncService>().SyncForAgencyAsync(job.AgencyId, job.DataSourceId, since, until, ct); job.Complete(clock.GetUtcNow()); }
            catch (Exception error) { logger.LogWarning(error, "Scheduled provider sync failed for source {SourceId}.", job.DataSourceId); job.Fail(error.GetType().Name, clock.GetUtcNow()); }
            await db.SaveChangesAsync(ct);
        }
    }
}
