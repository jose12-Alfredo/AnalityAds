using AnaliticAsd.Application.ProviderIntegrations;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
namespace AnaliticAsd.Controllers;
[ApiController, Authorize(Roles="Owner,Admin,Analyst"), Route("api/v1/data-sources/{sourceId:guid}/schedule")]
public sealed class ProviderSchedulesController(ProviderScheduleService service) : ControllerBase
{
 [HttpGet] public Task<ProviderScheduleModel> Get(Guid sourceId, CancellationToken ct) => service.GetAsync(sourceId, ct);
 [HttpPut] public Task<ProviderScheduleModel> Put(Guid sourceId, ProviderScheduleRequest request, CancellationToken ct) => service.ConfigureAsync(sourceId, request.IntervalMinutes, request.LookbackDays, request.IsEnabled, ct);
}
public sealed record ProviderScheduleRequest(bool IsEnabled, int IntervalMinutes, int LookbackDays);
