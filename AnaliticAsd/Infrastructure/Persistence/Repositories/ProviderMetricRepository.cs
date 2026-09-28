using AnaliticAsd.Application.ProviderIntegrations;
using AnaliticAsd.Domain.DataSources;
using Microsoft.EntityFrameworkCore;

namespace AnaliticAsd.Infrastructure.Persistence.Repositories;
public sealed class ProviderMetricRepository(AnalitiAdsDbContext db) : IProviderMetricRepository
{
    public async Task UpsertAsync(Guid agencyId, Guid sourceId, IReadOnlyList<ProviderDailyMetric> values,
        DateTimeOffset observedAtUtc, CancellationToken cancellationToken = default)
    {
        if (values.Count == 0) return;
        var since = values.Min(x => x.Date); var until = values.Max(x => x.Date);
        var existing = await db.ProviderMetricSnapshots.Where(x => x.AgencyId == agencyId && x.DataSourceId == sourceId && x.Date >= since && x.Date <= until).ToListAsync(cancellationToken);
        foreach (var value in values)
        {
            var row = existing.SingleOrDefault(x => x.Date == value.Date && x.DimensionKey == value.DimensionKey);
            if (row is null) { row = ProviderMetricSnapshot.Create(agencyId, sourceId, value.Date, value.DimensionKey, value.DimensionName, observedAtUtc); db.ProviderMetricSnapshots.Add(row); }
            row.Replace(value.DimensionName, value.Spend, value.Impressions, value.Clicks, value.Conversions,
                value.ConversionValue, value.ActiveUsers, value.Sessions, value.Views, observedAtUtc);
        }
    }
}
