using AnaliticAsd.Application.DashboardQueries;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
namespace AnaliticAsd.Controllers;
[ApiController, Authorize(Roles="Owner,Admin,Analyst,Viewer"), Route("api/v1/dashboard-data")]
public sealed class CrossChannelQueryController(CrossChannelQueryService service) : ControllerBase
{
 [HttpPost("cross-channel")]
 public Task<CrossChannelResult> Cross(CrossChannelRequest request, CancellationToken ct) => service.QueryAsync(new(request.ClientId, request.DataSourceIds, request.Since, request.Until, request.Dimension, request.Metrics), ct);
 [HttpPost("compare-previous")]
 public Task<DashboardComparisonResult> Compare(ComparisonRequest r, CancellationToken ct) => service.CompareAsync(new(r.ClientId,r.DataSourceId,r.Since,r.Until,r.Dimension,r.Metrics,null,r.Limit,r.SortMetric,r.SortDirection,r.DimensionValues),ct);
}
public sealed record CrossChannelRequest(Guid ClientId,IReadOnlyList<Guid> DataSourceIds,DateOnly Since,DateOnly Until,string Dimension,IReadOnlyList<string> Metrics);
public sealed record ComparisonRequest(Guid ClientId,Guid DataSourceId,DateOnly Since,DateOnly Until,string Dimension,IReadOnlyList<string> Metrics,int? Limit,string? SortMetric,string? SortDirection,IReadOnlyList<string>? DimensionValues);
