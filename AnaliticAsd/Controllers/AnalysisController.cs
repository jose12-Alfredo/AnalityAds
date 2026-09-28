using AnaliticAsd.Application.Analysis;
using AnaliticAsd.Application.Metrics;
using AnaliticAsd.Contracts.Analysis;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AnaliticAsd.Controllers;

[ApiController, Authorize, Route("api/v1/analyses")]
public sealed class AnalysisController(IAnalysisService service) : ControllerBase
{
    [HttpPost]
    public async Task<ActionResult<AnalysisResultModel>> Create(CreateAnalysisRequest request, CancellationToken ct) =>
        Ok(await service.AnalyzeAsync(new(request.AdAccountId, request.Since, request.Until,
            ParseComparison(request.Comparison), request.ComparisonSince, request.ComparisonUntil,
            request.CampaignIds, request.SelectedMetrics), ct));

    private static ComparisonPeriodType? ParseComparison(string? value)
    {
        if (string.IsNullOrWhiteSpace(value)) return null;
        return Enum.TryParse<ComparisonPeriodType>(value, true, out var parsed) && Enum.IsDefined(parsed)
            ? parsed
            : throw new ArgumentException("'comparison' must be PreviousPeriod, PreviousMonth, PreviousYear, Custom, or null.");
    }
}
