using System.Text.Json;
using AnaliticAsd.Application.Dashboards;

namespace AnaliticAsd.Tests.Dashboards;

public sealed class DashboardDefinitionValidatorTests
{
    [Fact]
    public async Task Valid_definition_is_canonicalized_and_its_sources_are_checked()
    {
        var sourceId = Guid.NewGuid();
        var validator = new DashboardDefinitionValidator(new SourceAccess(sourceId));

        var validated = await validator.ValidateDashboardAsync(Definition(dataSourceId: sourceId), Guid.NewGuid());

        Assert.Equal(1, validated.SchemaVersion);
        using var document = JsonDocument.Parse(validated.Json);
        Assert.Equal(sourceId, FirstSource(document.RootElement).GetProperty("dataSourceId").GetGuid());
    }

    [Fact]
    public async Task Foreign_source_and_sensitive_free_form_values_are_rejected()
    {
        var validator = new DashboardDefinitionValidator(new SourceAccess());

        await Assert.ThrowsAsync<ArgumentException>(() =>
            validator.ValidateDashboardAsync(Definition(dataSourceId: Guid.NewGuid()), Guid.NewGuid()));
        await Assert.ThrowsAsync<ArgumentException>(() =>
            validator.ValidateDashboardAsync(Definition(configuration: new { accessToken = "secret" }),
                Guid.NewGuid()));
    }

    [Fact]
    public async Task Template_slots_bind_only_to_sources_owned_by_the_destination_client()
    {
        var sourceId = Guid.NewGuid();
        var validator = new DashboardDefinitionValidator(new SourceAccess(sourceId));
        var template = validator.ValidateTemplate(Definition(sourceSlot: "primary"));

        var bound = await validator.BindTemplateAsync(template.Json,
            new Dictionary<string, Guid> { ["primary"] = sourceId }, Guid.NewGuid());

        using var document = JsonDocument.Parse(bound.Json);
        var source = FirstSource(document.RootElement);
        Assert.Equal(sourceId, source.GetProperty("dataSourceId").GetGuid());
        Assert.False(source.TryGetProperty("sourceSlot", out _));
        await Assert.ThrowsAsync<ArgumentException>(() => validator.BindTemplateAsync(template.Json,
            new Dictionary<string, Guid> { ["other"] = sourceId }, Guid.NewGuid()));
    }

    [Fact]
    public async Task Cross_client_copy_removes_source_ids_and_cannot_publish_until_rebound()
    {
        var sourceId = Guid.NewGuid();
        var validator = new DashboardDefinitionValidator(new SourceAccess(sourceId));
        var sourceClientId = Guid.NewGuid();

        var duplicate = await validator.PrepareDuplicateAsync(Definition(dataSourceId: sourceId),
            sourceClientId, Guid.NewGuid());

        using var document = JsonDocument.Parse(duplicate.Json);
        var source = FirstSource(document.RootElement);
        Assert.False(source.TryGetProperty("dataSourceId", out _));
        Assert.Equal("rebind-source-1", source.GetProperty("sourceSlot").GetString());
        await Assert.ThrowsAsync<ArgumentException>(() =>
            validator.ValidateForPublicationAsync(duplicate.Json, Guid.NewGuid()));
    }

    [Fact]
    public async Task Invalid_geometry_and_unknown_component_type_are_rejected()
    {
        var validator = new DashboardDefinitionValidator(new SourceAccess());

        await Assert.ThrowsAsync<ArgumentException>(() => validator.ValidateDashboardAsync(
            Definition(x: 1400, width: 100), Guid.NewGuid()));
        await Assert.ThrowsAsync<ArgumentException>(() => validator.ValidateDashboardAsync(
            Definition(componentType: "unknown"), Guid.NewGuid()));
    }

    [Theory]
    [InlineData("pivotTable")]
    [InlineData("horizontalBar")]
    [InlineData("column")]
    [InlineData("stackedBar")]
    [InlineData("stacked100Bar")]
    [InlineData("pie")]
    [InlineData("donut")]
    [InlineData("area")]
    [InlineData("combo")]
    [InlineData("funnel")]
    [InlineData("scatter")]
    [InlineData("bubble")]
    [InlineData("goalGauge")]
    [InlineData("image")]
    [InlineData("divider")]
    public async Task D3_3_component_names_match_the_persisted_contract(string componentType)
    {
        var validator = new DashboardDefinitionValidator(new SourceAccess());

        var validated = await validator.ValidateDashboardAsync(Definition(componentType: componentType), Guid.NewGuid());

        using var document = JsonDocument.Parse(validated.Json);
        Assert.Equal(componentType, document.RootElement.GetProperty("pages")[0].GetProperty("components")[0]
            .GetProperty("type").GetString());
    }

    private static string Definition(Guid? dataSourceId = null, string? sourceSlot = null,
        object? configuration = null, int x = 10, int width = 300, string componentType = "kpiCard")
    {
        var sources = new List<object>();
        if (dataSourceId is not null) sources.Add(new { dataSourceId });
        if (sourceSlot is not null) sources.Add(new { sourceSlot });
        return JsonSerializer.Serialize(new
        {
            schemaVersion = 1,
            pages = new[]
            {
                new
                {
                    id = Guid.NewGuid(), name = "Resumen", order = 0, width = 1440, height = 900,
                    components = new[]
                    {
                        new
                        {
                            id = Guid.NewGuid(), type = componentType, x, y = 10, width, height = 180,
                            zIndex = 0, isLocked = false,
                            data = new { sources, configuration = configuration ?? new { metric = "spend" } },
                            style = new { }
                        }
                    }
                }
            },
            theme = new { }
        });
    }

    private static JsonElement FirstSource(JsonElement definition) => definition.GetProperty("pages")[0]
        .GetProperty("components")[0].GetProperty("data").GetProperty("sources")[0];

    private sealed class SourceAccess(params Guid[] validSourceIds) : IDashboardSourceAccess
    {
        private readonly HashSet<Guid> valid = validSourceIds.ToHashSet();

        public Task<bool> AllBelongToClientAsync(Guid clientId, IReadOnlyCollection<Guid> dataSourceIds,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(dataSourceIds.All(valid.Contains));
    }
}
