namespace AnaliticAsd.Application.DashboardQueries;

public static class DashboardDataCatalog
{
    private const string Meta = "MetaAds";
    private static readonly string[] AdditiveDimensions = ["none", "date", "campaign"];
    private static readonly string[] DateOnly = ["date"];

    private static readonly MetricCatalogItemModel[] Metrics =
    [
        Metric("spend", "Inversión", "Importe gastado informado por Meta.", "currency", "sum", "decimal", AdditiveDimensions, "Solo puede agregarse con una moneda única."),
        Metric("impressions", "Impresiones", "Cantidad de veces que se mostraron los anuncios.", "count", "sum", "integer", AdditiveDimensions),
        Metric("reach", "Alcance", "Personas o cuentas únicas alcanzadas según Meta.", "count", "nonAdditive", "integer", DateOnly, "No se suma entre días ni entidades."),
        Metric("frequency", "Frecuencia", "Impresiones divididas por alcance para el mismo día.", "ratio", "recomputed", "decimal", DateOnly, "Solo disponible con dimensión de fecha."),
        Metric("linkClicks", "Clics en enlace", "Clics de enlace; no representa todos los tipos de clic.", "count", "sum", "integer", AdditiveDimensions),
        Metric("leads", "Leads", "Acciones de lead atribuidas informadas por Meta.", "count", "sum", "decimal", AdditiveDimensions, "Depende de la atribución y configuración de eventos de Meta."),
        Metric("purchases", "Compras", "Acciones de compra atribuidas informadas por Meta.", "count", "sum", "decimal", AdditiveDimensions, "No equivale a compradores únicos."),
        Metric("purchaseValue", "Valor de compras", "Valor atribuido a compras informado por Meta.", "currency", "sum", "decimal", AdditiveDimensions, "Solo puede agregarse con una moneda única."),
        Metric("cpm", "CPM", "Inversión / impresiones × 1000.", "currency", "ratioOfTotals", "decimal", AdditiveDimensions),
        Metric("ctr", "CTR", "Clics en enlace / impresiones × 100.", "percent", "ratioOfTotals", "decimal", AdditiveDimensions),
        Metric("cpc", "CPC", "Inversión / clics en enlace.", "currency", "ratioOfTotals", "decimal", AdditiveDimensions),
        Metric("cpl", "Costo por lead", "Inversión / leads.", "currency", "ratioOfTotals", "decimal", AdditiveDimensions),
        Metric("cpa", "Costo por compra", "Inversión / compras.", "currency", "ratioOfTotals", "decimal", AdditiveDimensions),
        Metric("roas", "ROAS", "Valor de compras / inversión.", "ratio", "ratioOfTotals", "decimal", AdditiveDimensions)
    ];

    private static readonly DimensionCatalogItemModel[] Dimensions =
    [
        new("none", "Total del período", Meta, "range", Metrics.Where(x => x.SupportedDimensions.Contains("none")).Select(x => x.Key).ToArray(), "Devuelve una sola fila."),
        new("date", "Fecha", Meta, "day", Metrics.Where(x => x.SupportedDimensions.Contains("date")).Select(x => x.Key).ToArray(), null),
        new("campaign", "Campaña", Meta, "entity", Metrics.Where(x => x.SupportedDimensions.Contains("campaign")).Select(x => x.Key).ToArray(), "Alcance y frecuencia no están disponibles porque no son aditivos en el período.")
    ];

    public static DashboardDataCatalogModel Get(string? provider = null)
    {
        if (string.IsNullOrWhiteSpace(provider))
        {
            var additional = new[] { "GoogleAds", "TikTokAds", "GoogleAnalytics4" }.SelectMany(name => Get(name).Metrics)
                .GroupBy(x => x.Key, StringComparer.OrdinalIgnoreCase).Select(x => x.First());
            var allMetrics = Metrics.Concat(additional).GroupBy(x => x.Key, StringComparer.OrdinalIgnoreCase).Select(x => x.First()).ToArray();
            var dimensions = new[] { "none", "date", "campaign" }.Select(key => new DimensionCatalogItemModel(key,
                key == "none" ? "Total del período" : key == "date" ? "Fecha" : "Campaña", "All",
                key == "date" ? "day" : key == "campaign" ? "entity" : "range",
                allMetrics.Where(x => x.SupportedDimensions.Contains(key)).Select(x => x.Key).ToArray(), null)).ToArray();
            return new(2, [Meta, "GoogleAds", "TikTokAds", "GoogleAnalytics4"], allMetrics, dimensions);
        }
        if (!string.IsNullOrWhiteSpace(provider) && !provider.Equals(Meta, StringComparison.OrdinalIgnoreCase))
        {
            var normalized = Enum.TryParse<Domain.DataSources.DataProvider>(provider, true, out var parsed) ? parsed.ToString() : throw new ArgumentException($"Unknown provider '{provider}'.");
            var keys = normalized == "GoogleAnalytics4"
                ? new[] { ("activeUsers", "Usuarios activos", "count"), ("sessions", "Sesiones", "count"), ("views", "Vistas", "count") }
                : new[] { ("spend", "Inversión", "currency"), ("impressions", "Impresiones", "count"), ("clicks", "Clics", "count"), ("conversions", "Conversiones", "count"), ("conversionValue", "Valor de conversión", "currency"), ("ctr", "CTR", "percent"), ("cpc", "CPC", "currency"), ("cpm", "CPM", "currency"), ("cpa", "CPA", "currency"), ("roas", "ROAS", "ratio") };
            var metrics = keys.Select(x => new MetricCatalogItemModel(x.Item1, x.Item2, x.Item2, normalized, x.Item3,
                x.Item1 is "ctr" or "cpc" or "cpm" or "cpa" or "roas" ? "ratioOfTotals" : "sum",
                "decimal", ["none", "date", "campaign"], null)).ToArray();
            var dimensions = new[] { "none", "date", "campaign" }.Select(key => new DimensionCatalogItemModel(key,
                key == "none" ? "Total del período" : key == "date" ? "Fecha" : "Campaña", normalized,
                key == "date" ? "day" : key == "campaign" ? "entity" : "range", metrics.Select(x => x.Key).ToArray(), null)).ToArray();
            return new(2, [Meta, "GoogleAds", "TikTokAds", "GoogleAnalytics4"], metrics, dimensions);
        }
        return new(2, [Meta, "GoogleAds", "TikTokAds", "GoogleAnalytics4"], Metrics, Dimensions);
    }

    public static MetricCatalogItemModel RequiredMetric(string key)
    {
        var metric = Metrics.SingleOrDefault(item => item.Key.Equals(key?.Trim(), StringComparison.OrdinalIgnoreCase));
        return metric ?? throw new ArgumentException($"Unknown metric '{key}'.");
    }

    public static DimensionCatalogItemModel RequiredDimension(string? key)
    {
        var normalized = string.IsNullOrWhiteSpace(key) ? "none" : key.Trim();
        return Dimensions.SingleOrDefault(item => item.Key.Equals(normalized, StringComparison.OrdinalIgnoreCase))
            ?? throw new ArgumentException($"Unknown dimension '{key}'.");
    }

    private static MetricCatalogItemModel Metric(string key, string name, string description, string unit,
        string aggregation, string valueType, IReadOnlyList<string> dimensions, string? limitation = null) =>
        new(key, name, description, Meta, unit, aggregation, valueType, dimensions, limitation);
}
