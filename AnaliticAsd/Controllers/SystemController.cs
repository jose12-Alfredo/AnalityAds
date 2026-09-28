using AnaliticAsd.Contracts;
using Microsoft.AspNetCore.Mvc;

namespace AnaliticAsd.Controllers;

[ApiController]
[Route("api/system")]
public sealed class SystemController(TimeProvider timeProvider) : ControllerBase
{
    [HttpGet("status")]
    [ProducesResponseType<SystemStatusResponse>(StatusCodes.Status200OK)]
    public ActionResult<SystemStatusResponse> GetStatus()
    {
        var version = typeof(Program).Assembly.GetName().Version?.ToString() ?? "unknown";
        return Ok(new SystemStatusResponse(
            "AnaliticAsd.Api",
            "Healthy",
            version,
            timeProvider.GetUtcNow()));
    }
}
