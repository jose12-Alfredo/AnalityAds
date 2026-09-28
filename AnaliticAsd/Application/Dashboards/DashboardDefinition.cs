using System.Text.Json;
using System.Text.Json.Serialization;

namespace AnaliticAsd.Application.Dashboards;

public enum DashboardComponentType
{
    KpiCard,
    Table,
    PivotTable,
    TimeSeries,
    HorizontalBar,
    Column,
    StackedBar,
    Stacked100Bar,
    Pie,
    Donut,
    Area,
    Combo,
    Funnel,
    Scatter,
    Bubble,
    GoalGauge,
    Map,
    Bullet,
    Treemap,
    Sankey,
    Waterfall,
    BoxPlot,
    Candlestick,
    Timeline,
    Text,
    Image,
    Logo,
    Shape,
    Divider,
    FilterControl,
    DateRangeControl
}

public sealed class DashboardDefinitionV1
{
    public int SchemaVersion { get; init; }
    public List<DashboardPageV1> Pages { get; init; } = [];
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public JsonElement Theme { get; init; }
    [JsonExtensionData]
    public Dictionary<string, JsonElement>? ExtensionData { get; init; }
}

public sealed class DashboardPageV1
{
    public Guid Id { get; init; }
    public string Name { get; init; } = string.Empty;
    public int Order { get; init; }
    public int Width { get; init; }
    public int Height { get; init; }
    public string? Background { get; init; }
    public List<DashboardComponentV1> Components { get; init; } = [];
    [JsonExtensionData]
    public Dictionary<string, JsonElement>? ExtensionData { get; init; }
}

public sealed class DashboardComponentV1
{
    public Guid Id { get; init; }
    public DashboardComponentType Type { get; init; }
    public int X { get; init; }
    public int Y { get; init; }
    public int Width { get; init; }
    public int Height { get; init; }
    public int ZIndex { get; init; }
    public bool IsLocked { get; init; }
    public Guid? GroupId { get; init; }
    public DashboardComponentDataV1 Data { get; init; } = new();
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public JsonElement Style { get; init; }
    [JsonExtensionData]
    public Dictionary<string, JsonElement>? ExtensionData { get; init; }
}

public sealed class DashboardComponentDataV1
{
    public List<DashboardSourceReferenceV1> Sources { get; init; } = [];
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public JsonElement Configuration { get; init; }
    [JsonExtensionData]
    public Dictionary<string, JsonElement>? ExtensionData { get; init; }
}

public sealed class DashboardSourceReferenceV1
{
    public Guid? DataSourceId { get; set; }
    public string? SourceSlot { get; set; }
    public string? Alias { get; init; }
    [JsonExtensionData]
    public Dictionary<string, JsonElement>? ExtensionData { get; init; }
}

public sealed record ValidatedDashboardDefinition(int SchemaVersion, string Json);

public interface IDashboardSourceAccess
{
    Task<bool> AllBelongToClientAsync(Guid clientId, IReadOnlyCollection<Guid> dataSourceIds,
        CancellationToken cancellationToken = default);
}

public interface IDashboardDefinitionValidator
{
    string CreateBlank();
    Task<ValidatedDashboardDefinition> ValidateDashboardAsync(string json, Guid clientId,
        CancellationToken cancellationToken = default);
    Task<ValidatedDashboardDefinition> ValidateForPublicationAsync(string json, Guid clientId,
        CancellationToken cancellationToken = default);
    ValidatedDashboardDefinition ValidateTemplate(string json);
    Task<ValidatedDashboardDefinition> BindTemplateAsync(string templateJson,
        IReadOnlyDictionary<string, Guid> sourceBindings, Guid clientId,
        CancellationToken cancellationToken = default);
    Task<ValidatedDashboardDefinition> PrepareDuplicateAsync(string dashboardJson, Guid sourceClientId,
        Guid destinationClientId, CancellationToken cancellationToken = default);
}
