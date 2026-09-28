using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace AnaliticAsd.Contracts.Reports;

[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
public sealed record CreateReportRequest(
    [Required, StringLength(200)] string Title,
    Guid AdAccountId,
    DateOnly Since,
    DateOnly Until,
    string? Comparison,
    DateOnly? ComparisonSince,
    DateOnly? ComparisonUntil,
    IReadOnlyList<Guid>? CampaignIds,
    IReadOnlyList<string>? SelectedMetrics);
