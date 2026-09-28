using System.Text.Json;
using AnaliticAsd.Application.Common;
using AnaliticAsd.Application.Dashboards;

namespace AnaliticAsd.Tests.Dashboards;

public sealed class PublishedDashboardQueryPolicyTests
{
    private readonly Guid sourceId = Guid.NewGuid();

    [Fact]
    public void Accepts_the_exact_query_persisted_in_the_publication()
    {
        using var definition = Definition(sourceId, "campaign", ["spend", "impressions"]);
        PublishedDashboardQueryPolicy.Validate(definition.RootElement, sourceId,
            new DateOnly(2026, 9, 1), new DateOnly(2026, 9, 21), "campaign", ["spend"], false);
    }

    [Fact]
    public void Rejects_a_source_that_is_not_part_of_the_publication()
    {
        using var definition = Definition(sourceId, "campaign", ["spend"]);
        Assert.Throws<EntityNotFoundException>(() => PublishedDashboardQueryPolicy.Validate(
            definition.RootElement, Guid.NewGuid(), new DateOnly(2026, 9, 1),
            new DateOnly(2026, 9, 21), "campaign", ["spend"], true));
    }

    [Fact]
    public void Rejects_metrics_and_dimensions_not_exposed_by_the_component()
    {
        using var definition = Definition(sourceId, "campaign", ["spend"]);
        Assert.Throws<EntityNotFoundException>(() => PublishedDashboardQueryPolicy.Validate(
            definition.RootElement, sourceId, new DateOnly(2026, 9, 1),
            new DateOnly(2026, 9, 21), "date", ["conversionValue"], true));
    }

    [Fact]
    public void Rejects_date_changes_when_the_link_disables_filters()
    {
        using var definition = Definition(sourceId, "campaign", ["spend"]);
        Assert.Throws<ForbiddenException>(() => PublishedDashboardQueryPolicy.Validate(
            definition.RootElement, sourceId, new DateOnly(2026, 9, 2),
            new DateOnly(2026, 9, 21), "campaign", ["spend"], false));
    }

    [Fact]
    public void Allows_date_changes_when_the_link_enables_filters()
    {
        using var definition = Definition(sourceId, "campaign", ["spend"]);
        PublishedDashboardQueryPolicy.Validate(definition.RootElement, sourceId,
            new DateOnly(2026, 9, 2), new DateOnly(2026, 9, 20), "campaign", ["spend"], true);
    }

    private static JsonDocument Definition(Guid sourceId, string dimension, string[] metrics) =>
        JsonDocument.Parse(JsonSerializer.Serialize(new
        {
            schemaVersion = 1,
            pages = new[]
            {
                new
                {
                    components = new[]
                    {
                        new
                        {
                            type = "table",
                            data = new
                            {
                                sources = new[] { new { dataSourceId = sourceId } },
                                configuration = new
                                {
                                    since = "2026-09-01", until = "2026-09-21", dimension, metrics
                                }
                            }
                        }
                    }
                }
            }
        }));
}
