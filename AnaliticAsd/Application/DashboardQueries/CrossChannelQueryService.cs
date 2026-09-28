using AnaliticAsd.Application.Common;

namespace AnaliticAsd.Application.DashboardQueries;
public sealed record CrossChannelQueryCommand(Guid ClientId, IReadOnlyList<Guid> DataSourceIds, DateOnly Since,
    DateOnly Until, string Dimension, IReadOnlyList<string> Metrics);
public sealed record CrossChannelSeries(Guid DataSourceId, string Provider, string? Currency,
    IReadOnlyList<DashboardQueryRowModel> Rows);
public sealed record CrossChannelResult(DateOnly Since, DateOnly Until, string Dimension,
    IReadOnlyList<CrossChannelSeries> Series, bool CanCombineMonetaryValues, string? Limitation);
public sealed record DashboardComparisonResult(DashboardQueryResultModel Current, DashboardQueryResultModel Previous);

public sealed class CrossChannelQueryService(IDashboardQueryService queries)
{
    public async Task<CrossChannelResult> QueryAsync(CrossChannelQueryCommand command, CancellationToken ct)
    {
        if (command.DataSourceIds is not { Count: >= 2 and <= 10 } || command.DataSourceIds.Distinct().Count() != command.DataSourceIds.Count)
            throw new ArgumentException("Choose between 2 and 10 distinct sources.");
        var results = new List<DashboardQueryResultModel>();
        foreach (var sourceId in command.DataSourceIds)
            results.Add(await queries.QueryAsync(new(command.ClientId, sourceId, command.Since, command.Until,
                command.Dimension, command.Metrics, null, 100, null, "asc"), ct));
        var monetary = command.Metrics.Any(x => x is "spend" or "conversionValue" or "purchaseValue" or "cpc" or "cpm" or "cpa" or "roas");
        var currencies = results.Select(x => x.Currency).Where(x => x is not null).Distinct(StringComparer.Ordinal).ToArray();
        var canCombine = !monetary || currencies.Length <= 1;
        return new(command.Since, command.Until, command.Dimension,
            results.Select(x => new CrossChannelSeries(x.DataSourceId, x.Provider, x.Currency, x.Rows)).ToArray(),
            canCombine, canCombine ? "Las series permanecen identificadas por plataforma; las conversiones no representan personas deduplicadas." : "No se pueden sumar valores monetarios de monedas distintas sin una conversión explícita.");
    }

    public async Task<DashboardComparisonResult> CompareAsync(DashboardQueryCommand current, CancellationToken ct)
    {
        var days = current.Until.DayNumber - current.Since.DayNumber + 1;
        var previous = current with { Since = current.Since.AddDays(-days), Until = current.Since.AddDays(-1) };
        return new(await queries.QueryAsync(current, ct), await queries.QueryAsync(previous, ct));
    }
}
