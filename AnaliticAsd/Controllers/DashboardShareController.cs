using AnaliticAsd.Application.Dashboards;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
namespace AnaliticAsd.Controllers;
[ApiController,Route("api/v1")]
public sealed class DashboardShareController(DashboardShareService service):ControllerBase
{
 [Authorize(Roles="Owner,Admin"),HttpGet("dashboards/{dashboardId:guid}/share-links")]
 public Task<IReadOnlyList<DashboardShareModel>> List(Guid dashboardId,CancellationToken ct)=>service.ListAsync(dashboardId,ct);
 [Authorize(Roles="Owner,Admin"),HttpPost("dashboards/{dashboardId:guid}/share-links")]
 public async Task<ActionResult<CreatedDashboardShareModel>> Create(Guid dashboardId,CreateDashboardShareRequest request,CancellationToken ct)=>StatusCode(201,await service.CreateAsync(dashboardId,request.ExpirationDays,request.Password,request.RecipientEmail,request.AllowFilters,request.AllowExport,request.AllowEmbed,ct));
 [Authorize(Roles="Owner,Admin"),HttpDelete("dashboards/{dashboardId:guid}/share-links/{linkId:guid}")]
 public async Task<IActionResult> Revoke(Guid dashboardId,Guid linkId,CancellationToken ct){await service.RevokeAsync(dashboardId,linkId,ct);return NoContent();}
 [AllowAnonymous,EnableRateLimiting("SharedReportAccess"),HttpPost("shared-dashboards/access")]
 public Task<SharedDashboardModel> Access(AccessDashboardShareRequest request,CancellationToken ct)=>service.AccessAsync(request.AccessToken,request.Password,request.RecipientEmail,request.Embed,ct);
 [AllowAnonymous,EnableRateLimiting("SharedReportAccess"),HttpPost("shared-dashboards/query")]
 public Task<PublicDashboardQueryModel> Query(PublicDashboardQueryRequest r,CancellationToken ct)=>service.QueryAsync(r.AccessToken,r.Password,r.RecipientEmail,r.DataSourceId,r.Since,r.Until,r.Dimension,r.Metrics,ct);
}
public sealed record CreateDashboardShareRequest(int? ExpirationDays,string? Password,string? RecipientEmail,bool AllowFilters,bool AllowExport,bool AllowEmbed);
public sealed record AccessDashboardShareRequest(string AccessToken,string? Password,string? RecipientEmail,bool Embed=false);
public sealed record PublicDashboardQueryRequest(string AccessToken,string? Password,string? RecipientEmail,Guid DataSourceId,DateOnly Since,DateOnly Until,string Dimension,IReadOnlyList<string> Metrics);
