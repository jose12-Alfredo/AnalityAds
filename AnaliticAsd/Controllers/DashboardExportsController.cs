using AnaliticAsd.Application.Dashboards;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace AnaliticAsd.Controllers;
[ApiController,Route("api/v1")]
public sealed class DashboardExportsController(DashboardExportService service):ControllerBase
{
 [Authorize(Roles="Owner,Admin,Analyst,Viewer"),HttpGet("dashboards/{dashboardId:guid}/exports")] public Task<IReadOnlyList<DashboardExportModel>> List(Guid dashboardId,CancellationToken ct)=>service.ListAsync(dashboardId,ct);
 [Authorize(Roles="Owner,Admin,Analyst,Viewer"),HttpPost("dashboards/{dashboardId:guid}/exports")] public async Task<ActionResult<DashboardExportModel>> CreateExport(Guid dashboardId,CreateDashboardExportRequest r,CancellationToken ct)=>Accepted(await service.RequestAsync(dashboardId,r.Format,ct));
 [Authorize(Roles="Owner,Admin,Analyst,Viewer"),HttpGet("dashboards/{dashboardId:guid}/exports/{exportId:guid}/file")] public async Task<IActionResult> Download(Guid dashboardId,Guid exportId,CancellationToken ct){var f=await service.DownloadAsync(dashboardId,exportId,ct);return File(f.Content,f.ContentType,f.FileName);}
 [Authorize(Roles="Owner,Admin"),HttpGet("dashboards/{dashboardId:guid}/delivery-schedules")] public Task<IReadOnlyList<DashboardDeliveryScheduleModel>> Schedules(Guid dashboardId,CancellationToken ct)=>service.ListSchedulesAsync(dashboardId,ct);
 [Authorize(Roles="Owner,Admin"),HttpPost("dashboards/{dashboardId:guid}/delivery-schedules")] public async Task<ActionResult<DashboardDeliveryScheduleModel>> Schedule(Guid dashboardId,CreateDashboardDeliveryScheduleRequest r,CancellationToken ct)=>StatusCode(201,await service.CreateScheduleAsync(dashboardId,r.Recipients,r.Format,r.Frequency,r.HourUtc,r.DayOfWeek,r.DayOfMonth,r.IsEnabled,ct));
 [Authorize(Roles="Owner,Admin"),HttpDelete("dashboards/{dashboardId:guid}/delivery-schedules/{scheduleId:guid}")] public async Task<IActionResult> Delete(Guid dashboardId,Guid scheduleId,CancellationToken ct){await service.DeleteScheduleAsync(dashboardId,scheduleId,ct);return NoContent();}
 [AllowAnonymous,EnableRateLimiting("SharedReportAccess"),HttpPost("shared-dashboards/exports")] public async Task<ActionResult<DashboardExportModel>> PublicRequest(PublicDashboardExportRequest r,CancellationToken ct)=>Accepted(await service.RequestPublicAsync(r.AccessToken,r.Password,r.RecipientEmail,r.Format,ct));
 [AllowAnonymous,EnableRateLimiting("SharedReportAccess"),HttpPost("shared-dashboards/exports/{exportId:guid}/status")] public Task<DashboardExportModel> PublicStatus(Guid exportId,PublicDashboardExportAccess r,CancellationToken ct)=>service.StatusPublicAsync(r.AccessToken,exportId,ct);
 [AllowAnonymous,EnableRateLimiting("SharedReportAccess"),HttpPost("shared-dashboards/exports/{exportId:guid}/file")] public async Task<IActionResult> PublicDownload(Guid exportId,PublicDashboardExportAccess r,CancellationToken ct){var f=await service.DownloadPublicAsync(r.AccessToken,exportId,ct);return File(f.Content,f.ContentType,f.FileName);}
}
public sealed record CreateDashboardExportRequest(string Format);
public sealed record PublicDashboardExportRequest(string AccessToken,string? Password,string? RecipientEmail,string Format);
public sealed record PublicDashboardExportAccess(string AccessToken);
public sealed record CreateDashboardDeliveryScheduleRequest(IReadOnlyList<string> Recipients,string Format,string Frequency,int HourUtc,int? DayOfWeek,int? DayOfMonth,bool IsEnabled=true);
