using AnaliticAsd.Application.Dashboards;using Microsoft.AspNetCore.Authorization;using Microsoft.AspNetCore.Mvc;
namespace AnaliticAsd.Controllers;
[ApiController,Authorize(Roles="Owner,Admin"),Route("api/v1/dashboard-templates/defaults")]
public sealed class DefaultDashboardTemplatesController(DefaultDashboardTemplateService service):ControllerBase
{[HttpPost]public async Task<ActionResult<object>> Install(CancellationToken ct)=>Ok(new{installed=await service.InstallAsync(ct)});}
