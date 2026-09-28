using AnaliticAsd.Application.Abstractions;
using AnaliticAsd.Application.Common;
using AnaliticAsd.Application.DataSources;
using AnaliticAsd.Application.Identity;
using AnaliticAsd.Domain.DataSources;

namespace AnaliticAsd.Application.ProviderIntegrations;
public sealed class ProviderSyncService(ICurrentTenant tenant, IProviderSourceRepository sources,
    IProviderConnectionRepository connections, IProviderCredentialProtector protector,
    IGoogleProviderClient google, ITikTokProviderClient tiktok, IProviderMetricRepository metrics,
    IUnitOfWork unitOfWork, TimeProvider clock) : IProviderSyncService
{
    public async Task<ProviderSyncModel> SyncAsync(Guid sourceId, DateOnly since, DateOnly until, CancellationToken ct = default)
        => await SyncForAgencyAsync(tenant.AgencyId, sourceId, since, until, ct);

    public async Task<ProviderSyncModel> SyncForAgencyAsync(Guid agencyId, Guid sourceId, DateOnly since, DateOnly until, CancellationToken ct = default)
    {
        if (since > until || until.DayNumber - since.DayNumber > 366) throw new ArgumentException("El intervalo debe contener entre 1 y 367 días.");
        var source = await sources.GetByIdAsync(agencyId, sourceId, true, ct) ?? throw new EntityNotFoundException("The data source was not found.");
        if (source.Provider == DataProvider.MetaAds) throw new ConflictException("Meta Ads uses its existing synchronization endpoint.");
        var connection = await connections.GetLatestAsync(agencyId, source.Provider, true, ct) ?? throw new ConflictException("The provider is not connected.");
        var credential = protector.Unprotect(connection.ProtectedCredentialPayload);
        if (credential.ExpiresAtUtc <= clock.GetUtcNow() && source.Provider is DataProvider.GoogleAds or DataProvider.GoogleAnalytics4)
        {
            credential = await google.RefreshAsync(credential, ct);
            connection.UpdateAuthorization(connection.DisplayName, protector.Protect(credential),
                connection.ExternalSubjectId, credential.ExpiresAtUtc, clock.GetUtcNow());
        }
        else if (credential.ExpiresAtUtc <= clock.GetUtcNow())
            throw new ConflictException("The provider authorization expired. Reconnect it.");
        IReadOnlyList<ProviderDailyMetric> rows = source.Provider == DataProvider.TikTokAds
            ? await tiktok.ReadDailyMetricsAsync(credential.AccessToken, source.ExternalId, since, until, ct)
            : await google.ReadDailyMetricsAsync(credential.AccessToken, source.Provider, source.ExternalId, since, until, ct);
        var now = clock.GetUtcNow(); await metrics.UpsertAsync(agencyId, source.Id, rows, now, ct);
        source.RecordSynchronization(since, until, now); connection.RecordSuccess(now); await unitOfWork.SaveChangesAsync(ct);
        return new(source.Id, source.Provider, since, until, rows.Count, now);
    }
}
