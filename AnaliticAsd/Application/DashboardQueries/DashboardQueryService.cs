using AnaliticAsd.Application.Common;
using AnaliticAsd.Application.Metrics;
using AnaliticAsd.Domain.Advertising;
using AnaliticAsd.Domain.DataSources;

namespace AnaliticAsd.Application.DashboardQueries;

public sealed class DashboardQueryService(IDashboardQueryRepository repository, TimeProvider clock)
    : IDashboardQueryService
{
    public DashboardDataCatalogModel Catalog(string? provider = null) => DashboardDataCatalog.Get(provider);

    public async Task<IReadOnlyList<DashboardDataSourceModel>> ListSourcesAsync(Guid clientId,
        CancellationToken cancellationToken = default)
    {
        if (clientId == Guid.Empty) throw new ArgumentException("Client id is required.", nameof(clientId));
        var values = await repository.ListSourcesAsync(clientId, cancellationToken);
        return values.Select(MapSource).ToArray();
    }

    public async Task<DashboardQueryResultModel> QueryAsync(DashboardQueryCommand command,
        CancellationToken cancellationToken = default)
    {
        Validate(command);
        var source = await repository.GetSourceAsync(command.ClientId, command.DataSourceId, cancellationToken)
            ?? throw new EntityNotFoundException("The data source was not found in the authorized client.");
        if (!source.IsActive || source.ArchivedAtUtc is not null)
            throw new ConflictException("The data source is inactive or archived.");
        if (source.Provider != DataProvider.MetaAds)
            return await QueryProvider(source, command, cancellationToken);

        var dimension = DashboardDataCatalog.RequiredDimension(command.Dimension);
        var metrics = command.Metrics.Select(DashboardDataCatalog.RequiredMetric).DistinctBy(item => item.Key).ToArray();
        var incompatible = metrics.FirstOrDefault(metric => !metric.SupportedDimensions.Contains(dimension.Key));
        if (incompatible is not null)
            throw new ArgumentException($"Metric '{incompatible.Key}' is not compatible with dimension '{dimension.Key}'. {incompatible.Limitation}");

        var rows = dimension.Key switch
        {
            "none" => await TotalRows(source, command, metrics, cancellationToken),
            "date" => await DateRows(source, command, metrics, cancellationToken),
            "campaign" => await CampaignRows(source, command, metrics, cancellationToken),
            _ => throw new ArgumentException("Unsupported dimension.")
        };
        var allSnapshots = await repository.ListSnapshotsAsync(source.Id,
            dimension.Key == "campaign" || command.CampaignIds is { Count: > 0 }
                ? InsightLevel.Campaign : InsightLevel.Account,
            command.Since, command.Until, cancellationToken);
        if (command.CampaignIds is { Count: > 0 })
            allSnapshots = allSnapshots.Where(item => item.CampaignId is { } id && command.CampaignIds.Contains(id)).ToArray();
        var coverage = Coverage(command.Since, command.Until, allSnapshots);
        return new(command.ClientId, source.Id, source.Provider.ToString(), dimension.Key, command.Since,
            command.Until, Currency(allSnapshots, source.Currency), source.TimeZone, source.LastSyncedAtUtc,
            coverage, rows);
    }

    private async Task<DashboardQueryResultModel> QueryProvider(DataSource source, DashboardQueryCommand command, CancellationToken ct)
    {
        var catalog = DashboardDataCatalog.Get(source.Provider.ToString());
        var dimension = string.IsNullOrWhiteSpace(command.Dimension) ? "none" : command.Dimension;
        if (dimension is not ("none" or "date" or "campaign")) throw new ArgumentException($"Unknown dimension '{dimension}'.");
        var selected = command.Metrics.Distinct(StringComparer.OrdinalIgnoreCase).Select(key =>
            catalog.Metrics.SingleOrDefault(x => x.Key.Equals(key, StringComparison.OrdinalIgnoreCase)) ?? throw new ArgumentException($"Unknown metric '{key}' for {source.Provider}.")).ToArray();
        var snapshots = await repository.ListProviderSnapshotsAsync(source.Id, command.Since, command.Until, ct);
        if (command.DimensionValues is { Count: > 0 })
        {
            var allowed = command.DimensionValues.ToHashSet(StringComparer.Ordinal);
            snapshots = snapshots.Where(x => allowed.Contains(dimension == "date" ? x.Date.ToString("yyyy-MM-dd") : x.DimensionKey)).ToArray();
        }
        IEnumerable<IGrouping<string, ProviderMetricSnapshot>> groups = dimension switch
        {
            "date" => snapshots.GroupBy(x => x.Date.ToString("yyyy-MM-dd")),
            "campaign" => snapshots.GroupBy(x => x.DimensionKey),
            _ => snapshots.GroupBy(_ => "total")
        };
        var rows = groups.Select(group =>
        {
            var first = group.First(); var label = dimension == "date" ? first.Date.ToString("dd/MM/yyyy") : dimension == "campaign" ? first.DimensionName : "Total";
            return new DashboardQueryRowModel(group.Key, label, dimension == "none" ? null : group.Key,
                selected.ToDictionary(x => x.Key, x => ProviderValue(x, group), StringComparer.OrdinalIgnoreCase));
        }).ToArray();
        var sort = command.SortMetric ?? selected[0].Key;
        rows = (command.SortDirection?.Equals("asc", StringComparison.OrdinalIgnoreCase) == true
            ? rows.OrderBy(x => x.Metrics[sort].Value ?? decimal.MaxValue)
            : dimension == "date" ? rows.OrderBy(x => x.Key) : rows.OrderByDescending(x => x.Metrics[sort].Value ?? decimal.MinValue)).Take(command.Limit ?? 100).ToArray();
        var dates = snapshots.Select(x => x.Date).Distinct().ToArray();
        return new(command.ClientId, source.Id, source.Provider.ToString(), dimension, command.Since, command.Until,
            source.Currency, source.TimeZone, source.LastSyncedAtUtc, new(command.Until.DayNumber-command.Since.DayNumber+1,
                dates.Length, dates.Length == 0 ? null : dates.Min(), dates.Length == 0 ? null : dates.Max()), rows);
    }

    private static DashboardQueryMetricModel ProviderValue(MetricCatalogItemModel metric, IEnumerable<ProviderMetricSnapshot> values)
    {
        var rows = values.ToArray(); decimal? SumDecimal(Func<ProviderMetricSnapshot, decimal?> f) { var v=rows.Select(f).Where(x=>x.HasValue).Select(x=>x!.Value).ToArray(); return v.Length==0?null:v.Sum(); }
        decimal? SumLong(Func<ProviderMetricSnapshot,long?> f) { var v=rows.Select(f).Where(x=>x.HasValue).Select(x=>(decimal)x!.Value).ToArray(); return v.Length==0?null:v.Sum(); }
        var spend=SumDecimal(x=>x.Spend); var impressions=SumLong(x=>x.Impressions); var clicks=SumLong(x=>x.Clicks); var conversions=SumDecimal(x=>x.Conversions); var revenue=SumDecimal(x=>x.ConversionValue);
        decimal? Divide(decimal? a, decimal? b, decimal factor=1) => a.HasValue && b is > 0 ? a.Value / b.Value * factor : null;
        var value = metric.Key switch { "spend"=>spend, "impressions"=>impressions, "clicks"=>clicks, "conversions"=>conversions,
            "conversionValue"=>revenue, "activeUsers"=>SumLong(x=>x.ActiveUsers), "sessions"=>SumLong(x=>x.Sessions), "views"=>SumLong(x=>x.Views),
            "ctr"=>Divide(clicks,impressions,100), "cpc"=>Divide(spend,clicks), "cpm"=>Divide(spend,impressions,1000), "cpa"=>Divide(spend,conversions), "roas"=>Divide(revenue,spend), _=>null };
        return new(value, value.HasValue ? MetricAvailability.CompleteForSnapshots.ToString() : MetricAvailability.Incomplete.ToString(), metric.Unit);
    }

    private async Task<IReadOnlyList<DashboardQueryRowModel>> TotalRows(DataSource source,
        DashboardQueryCommand command, IReadOnlyList<MetricCatalogItemModel> metrics, CancellationToken ct)
    {
        var level = command.CampaignIds is { Count: > 0 } ? InsightLevel.Campaign : InsightLevel.Account;
        var snapshots = await repository.ListSnapshotsAsync(source.Id, level, command.Since, command.Until, ct);
        if (command.CampaignIds is { Count: > 0 })
        {
            var names = await RequiredCampaigns(source.Id, command.CampaignIds, ct);
            snapshots = snapshots.Where(item => item.CampaignId is { } id && names.ContainsKey(id)).ToArray();
        }
        var summary = RangeMetricsCalculator.Summarize(source.Id, null, null, null, InsightLevel.Account,
            command.Since, command.Until, snapshots);
        return [new("total", "Total", null, Values(summary, metrics))];
    }

    private async Task<IReadOnlyList<DashboardQueryRowModel>> DateRows(DataSource source,
        DashboardQueryCommand command, IReadOnlyList<MetricCatalogItemModel> metrics, CancellationToken ct)
    {
        if (command.CampaignIds is { Count: > 0 })
            throw new ArgumentException("Campaign filters cannot be combined with the date dimension in D3.1.");
        var snapshots = await repository.ListSnapshotsAsync(source.Id, InsightLevel.Account,
            command.Since, command.Until, ct);
        var ordered = command.SortDirection?.Equals("desc", StringComparison.OrdinalIgnoreCase) == true
            ? snapshots.OrderByDescending(item => item.SnapshotDate)
            : snapshots.OrderBy(item => item.SnapshotDate);
        return ordered.Take(command.Limit ?? 100)
            .Select(item => new DashboardQueryRowModel(item.SnapshotDate.ToString("yyyy-MM-dd"),
                item.SnapshotDate.ToString("dd/MM/yyyy"), item.SnapshotDate.ToString("yyyy-MM-dd"),
                DateValues(item, metrics))).ToArray();
    }

    private async Task<IReadOnlyList<DashboardQueryRowModel>> CampaignRows(DataSource source,
        DashboardQueryCommand command, IReadOnlyList<MetricCatalogItemModel> metrics, CancellationToken ct)
    {
        var names = await repository.CampaignNamesAsync(source.Id, ct);
        if (command.CampaignIds is { Count: > 0 }) names = await RequiredCampaigns(source.Id, command.CampaignIds, ct);
        var snapshots = await repository.ListSnapshotsAsync(source.Id, InsightLevel.Campaign,
            command.Since, command.Until, ct);
        var rows = snapshots.Where(item => item.CampaignId is { } id && names.ContainsKey(id))
            .GroupBy(item => item.CampaignId!.Value)
            .Select(group =>
            {
                var summary = RangeMetricsCalculator.Summarize(source.Id, group.Key, null, null,
                    InsightLevel.Campaign, command.Since, command.Until, group.ToArray());
                return new DashboardQueryRowModel(group.Key.ToString(), names[group.Key], group.Key.ToString(),
                    Values(summary, metrics));
            })
            .ToArray();
        var sortMetric = string.IsNullOrWhiteSpace(command.SortMetric) ? metrics[0].Key : command.SortMetric;
        return (command.SortDirection?.Equals("asc", StringComparison.OrdinalIgnoreCase) == true
                ? rows.OrderBy(row => row.Metrics.GetValueOrDefault(sortMetric)?.Value ?? decimal.MaxValue)
                : rows.OrderByDescending(row => row.Metrics.GetValueOrDefault(sortMetric)?.Value ?? decimal.MinValue))
            .ThenBy(row => row.Label, StringComparer.OrdinalIgnoreCase).Take(command.Limit ?? 100).ToArray();
    }

    private async Task<IReadOnlyDictionary<Guid, string>> RequiredCampaigns(Guid sourceId,
        IReadOnlyList<Guid> requested, CancellationToken ct)
    {
        var names = await repository.CampaignNamesAsync(sourceId, ct);
        var unique = requested.Distinct().ToArray();
        var missing = unique.FirstOrDefault(id => !names.ContainsKey(id));
        if (missing != Guid.Empty) throw new ArgumentException($"Campaign '{missing}' does not belong to the selected source.");
        return names.Where(pair => unique.Contains(pair.Key)).ToDictionary();
    }

    private static IReadOnlyDictionary<string, DashboardQueryMetricModel> Values(RangeMetricsSummaryModel summary,
        IReadOnlyList<MetricCatalogItemModel> metrics) => metrics.ToDictionary(metric => metric.Key,
            metric => metric.Key switch
            {
                "spend" => Value(summary.Observed.Spend, metric),
                "impressions" => Value(summary.Observed.Impressions, metric),
                "reach" => Value(summary.Observed.Reach, metric),
                "linkClicks" => Value(summary.Observed.LinkClicks, metric),
                "leads" => Value(summary.Observed.Leads, metric),
                "purchases" => Value(summary.Observed.Purchases, metric),
                "purchaseValue" => Value(summary.Observed.PurchaseValue, metric),
                "frequency" => Value(summary.Derived.Frequency, metric),
                "cpm" => Value(summary.Derived.Cpm, metric),
                "ctr" => Value(summary.Derived.Ctr, metric),
                "cpc" => Value(summary.Derived.Cpc, metric),
                "cpl" => Value(summary.Derived.Cpl, metric),
                "cpa" => Value(summary.Derived.Cpa, metric),
                "roas" => Value(summary.Derived.Roas, metric),
                _ => throw new ArgumentException($"Metric '{metric.Key}' is not implemented.")
            }, StringComparer.OrdinalIgnoreCase);

    private static IReadOnlyDictionary<string, DashboardQueryMetricModel> DateValues(InsightSnapshot snapshot,
        IReadOnlyList<MetricCatalogItemModel> metrics)
    {
        var availability = snapshot.ObservedDataQuality == InsightSnapshotDataQuality.LegacyZeroNormalized
            ? MetricAvailability.LegacyZeroNormalized : MetricAvailability.CompleteForSnapshots;
        DashboardQueryMetricModel Map(decimal? number, MetricCatalogItemModel metric) =>
            new(number, number.HasValue ? availability.ToString() : MetricAvailability.Incomplete.ToString(), metric.Unit);
        return metrics.ToDictionary(metric => metric.Key, metric => metric.Key switch
        {
            "spend" => Map(snapshot.Spend, metric),
            "impressions" => Map(snapshot.Impressions, metric),
            "reach" => Map(snapshot.Reach, metric),
            "linkClicks" => Map(snapshot.LinkClicks, metric),
            "leads" => Map(snapshot.Leads, metric),
            "purchases" => Map(snapshot.Purchases, metric),
            "purchaseValue" => Map(snapshot.PurchaseValue, metric),
            "frequency" => Map(snapshot.Frequency, metric),
            "cpm" => Map(snapshot.Cpm, metric),
            "ctr" => Map(snapshot.Ctr, metric),
            "cpc" => Map(snapshot.Cpc, metric),
            "cpl" => Map(snapshot.Cpl, metric),
            "cpa" => Map(snapshot.Cpa, metric),
            "roas" => Map(snapshot.Roas, metric),
            _ => throw new ArgumentException($"Metric '{metric.Key}' is not implemented.")
        }, StringComparer.OrdinalIgnoreCase);
    }

    private static DashboardQueryMetricModel Value(DecimalRangeMetricModel value, MetricCatalogItemModel metric) =>
        new(value.Value, value.Availability.ToString(), metric.Unit);
    private static DashboardQueryMetricModel Value(LongRangeMetricModel value, MetricCatalogItemModel metric) =>
        new(value.Value, value.Availability.ToString(), metric.Unit);
    private static DashboardQueryCoverageModel Coverage(DateOnly since, DateOnly until,
        IReadOnlyList<InsightSnapshot> snapshots) => new(until.DayNumber - since.DayNumber + 1,
        snapshots.Select(item => item.SnapshotDate).Distinct().Count(),
        snapshots.Count == 0 ? null : snapshots.Min(item => item.SnapshotDate),
        snapshots.Count == 0 ? null : snapshots.Max(item => item.SnapshotDate));
    private static string? Currency(IReadOnlyList<InsightSnapshot> snapshots, string? fallback)
    {
        var currencies = snapshots.Select(item => item.Currency).Distinct(StringComparer.Ordinal).ToArray();
        return currencies.Length switch { 0 => fallback, 1 => currencies[0], _ => null };
    }
    private static DashboardDataSourceModel MapSource(DataSource source) => new(source.Id, source.ClientId,
        source.Provider.ToString(), source.SourceType.ToString(), source.Name, source.Currency, source.TimeZone,
        source.IsActive && source.ArchivedAtUtc is null, source.AvailableSince, source.AvailableUntil,
        source.LastSyncedAtUtc);

    private void Validate(DashboardQueryCommand command)
    {
        if (command.ClientId == Guid.Empty) throw new ArgumentException("Client id is required.");
        if (command.DataSourceId == Guid.Empty) throw new ArgumentException("Data source id is required.");
        if (command.Since == DateOnly.MinValue || command.Until == DateOnly.MinValue || command.Since > command.Until)
            throw new ArgumentException("A valid date range is required.");
        if (command.Until > DateOnly.FromDateTime(clock.GetUtcNow().UtcDateTime))
            throw new ArgumentException("The date range cannot end in the future.");
        if (command.Until.DayNumber - command.Since.DayNumber + 1 > 90)
            throw new ArgumentException("The date range cannot exceed 90 days for Meta queries.");
        if (command.Metrics is null || command.Metrics.Count is < 1 or > 10
            || command.Metrics.Any(string.IsNullOrWhiteSpace))
            throw new ArgumentException("Choose between 1 and 10 metrics.");
        if (command.CampaignIds is { Count: > 100 } || command.CampaignIds?.Any(id => id == Guid.Empty) == true)
            throw new ArgumentException("Campaign filters may contain up to 100 valid ids.");
        if (command.Limit is < 1 or > 100) throw new ArgumentException("Limit must be between 1 and 100.");
        if (command.DimensionValues is { Count: > 100 } || command.DimensionValues?.Any(string.IsNullOrWhiteSpace) == true)
            throw new ArgumentException("Dimension filters may contain up to 100 non-empty values.");
        if (!string.IsNullOrWhiteSpace(command.SortMetric)
            && !command.Metrics.Any(metric => metric.Equals(command.SortMetric, StringComparison.OrdinalIgnoreCase)))
            throw new ArgumentException("Sort metric must be included in the selected metrics.");
        if (!string.IsNullOrWhiteSpace(command.SortDirection)
            && !command.SortDirection.Equals("asc", StringComparison.OrdinalIgnoreCase)
            && !command.SortDirection.Equals("desc", StringComparison.OrdinalIgnoreCase))
            throw new ArgumentException("Sort direction must be 'asc' or 'desc'.");
    }
}
