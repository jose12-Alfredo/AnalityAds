using AnaliticAsd.Application.Metrics;
using AnaliticAsd.Application.Reports;
using AnaliticAsd.Contracts.Reports;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AnaliticAsd.Controllers;

[ApiController, Authorize, Route("api/v1")]
public sealed class ReportsController(IReportService service) : ControllerBase
{
    [Authorize(Roles = "Owner,Admin,Analyst")]
    [HttpPost("reports")]
    public async Task<ActionResult<ReportDataModel>> Create(CreateReportRequest request, CancellationToken ct)
    {
        var report = await service.CreateAsync(new(request.Title, request.AdAccountId, request.Since, request.Until,
            ParseComparison(request.Comparison), request.ComparisonSince, request.ComparisonUntil,
            request.CampaignIds, request.SelectedMetrics), ct);
        return CreatedAtAction(nameof(Get), new { reportId = report.ReportId }, report);
    }

    [HttpGet("clients/{clientId:guid}/reports")]
    public async Task<ActionResult<IReadOnlyList<ReportListItemModel>>> List(Guid clientId,
        [FromQuery] int page = 1, [FromQuery] int pageSize = 20, CancellationToken ct = default) =>
        Ok(await service.ListAsync(clientId, page, pageSize, ct));

    [HttpGet("reports/{reportId:guid}")]
    public async Task<ActionResult<ReportDataModel>> Get(Guid reportId, CancellationToken ct) =>
        Ok(await service.GetAsync(reportId, ct));

    [HttpGet("reports/{reportId:guid}/pdf")]
    public async Task<IActionResult> Pdf(Guid reportId, CancellationToken ct)
    {
        var bytes = await service.GetPdfAsync(reportId, ct);
        return File(bytes, "application/pdf", $"analitiads-report-{reportId:N}.pdf");
    }

    [Authorize(Roles = "Owner,Admin")]
    [HttpDelete("reports/{reportId:guid}")]
    public async Task<IActionResult> Delete(Guid reportId, CancellationToken ct)
    {
        await service.DeleteAsync(reportId, ct);
        return NoContent();
    }

    private static ComparisonPeriodType? ParseComparison(string? value)
    {
        if (string.IsNullOrWhiteSpace(value)) return null;
        return Enum.TryParse<ComparisonPeriodType>(value, true, out var parsed) && Enum.IsDefined(parsed)
            ? parsed
            : throw new ArgumentException("'comparison' must be PreviousPeriod, PreviousMonth, PreviousYear, Custom, or null.");
    }
}
