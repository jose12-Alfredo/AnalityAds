using AnaliticAsd.Application.Reports;
using AnaliticAsd.Contracts.Reports;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace AnaliticAsd.Controllers;

[ApiController, Route("api/v1")]
public sealed class ReportShareController(IReportShareService service) : ControllerBase
{
    [Authorize(Roles = "Owner,Admin")]
    [HttpGet("reports/{reportId:guid}/share-links")]
    public async Task<ActionResult<IReadOnlyList<ReportShareLinkModel>>> List(Guid reportId, CancellationToken ct) =>
        Ok(await service.ListAsync(reportId, ct));

    [Authorize(Roles = "Owner,Admin")]
    [HttpPost("reports/{reportId:guid}/share-links")]
    public async Task<ActionResult<CreatedReportShareLinkModel>> Create(Guid reportId,
        CreateReportShareLinkRequest request, CancellationToken ct) =>
        StatusCode(StatusCodes.Status201Created, await service.CreateAsync(reportId, request.ExpirationDays, ct));

    [Authorize(Roles = "Owner,Admin")]
    [HttpDelete("reports/{reportId:guid}/share-links/{shareLinkId:guid}")]
    public async Task<IActionResult> Revoke(Guid reportId, Guid shareLinkId, CancellationToken ct)
    {
        await service.RevokeAsync(reportId, shareLinkId, ct);
        return NoContent();
    }

    [AllowAnonymous, EnableRateLimiting("SharedReportAccess")]
    [HttpPost("shared-reports/access")]
    public async Task<ActionResult<SharedReportDataModel>> Access(AccessSharedReportRequest request, CancellationToken ct) =>
        Ok(await service.AccessAsync(request.AccessToken, "View", ct));

    [AllowAnonymous, EnableRateLimiting("SharedReportAccess")]
    [HttpPost("shared-reports/pdf")]
    public async Task<IActionResult> Pdf(AccessSharedReportRequest request, CancellationToken ct) =>
        File(await service.PdfAsync(request.AccessToken, ct), "application/pdf", "analitiads-shared-report.pdf");
}
