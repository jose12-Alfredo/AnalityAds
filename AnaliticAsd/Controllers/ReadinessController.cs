using AnaliticAsd.Infrastructure.Persistence;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AnaliticAsd.Controllers;

[ApiController,Route("api/system")]
public sealed class ReadinessController(AnalitiAdsDbContext db,TimeProvider clock):ControllerBase
{
 [HttpGet("readiness")]
 public async Task<IActionResult> Get(CancellationToken ct)
 {
  var started=clock.GetTimestamp();
  try
  {
   if(!await db.Database.CanConnectAsync(ct))return StatusCode(503,new ReadinessResponse("Unavailable",false,clock.GetElapsedTime(started).TotalMilliseconds,clock.GetUtcNow()));
   return Ok(new ReadinessResponse("Ready",true,clock.GetElapsedTime(started).TotalMilliseconds,clock.GetUtcNow()));
  }
  catch(Exception error)when(error is not OperationCanceledException)
  {return StatusCode(503,new ReadinessResponse("Unavailable",false,clock.GetElapsedTime(started).TotalMilliseconds,clock.GetUtcNow()));}
 }
}
public sealed record ReadinessResponse(string Status,bool Database,double DatabaseLatencyMs,DateTimeOffset TimestampUtc);
