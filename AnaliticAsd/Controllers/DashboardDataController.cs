using AnaliticAsd.Application.DashboardQueries;
using AnaliticAsd.Contracts.DashboardQueries;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AnaliticAsd.Controllers;

[ApiController, Authorize(Roles = "Owner,Admin,Analyst,Viewer"), Route("api/v1/dashboard-data")]
public sealed class DashboardDataController(IDashboardQueryService service) : ControllerBase
{
    [HttpGet("catalog")]
    public ActionResult<DashboardDataCatalogModel> Catalog([FromQuery] string? provider = null) =>
        Ok(service.Catalog(provider));

    [HttpGet("clients/{clientId:guid}/sources")]
    public async Task<ActionResult<IReadOnlyList<DashboardDataSourceModel>>> Sources(Guid clientId,
        CancellationToken cancellationToken) => Ok(await service.ListSourcesAsync(clientId, cancellationToken));

    [HttpPost("query")]
    public async Task<ActionResult<DashboardQueryResultModel>> Query(DashboardQueryRequest request,
        CancellationToken cancellationToken) => Ok(await service.QueryAsync(new(request.ClientId,
            request.DataSourceId, request.Since, request.Until, request.Dimension, request.Metrics,
            request.CampaignIds, request.Limit, request.SortMetric, request.SortDirection, request.DimensionValues), cancellationToken));
}
