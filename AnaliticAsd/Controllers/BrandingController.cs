using AnaliticAsd.Application.Common;using AnaliticAsd.Application.Identity;using AnaliticAsd.Domain.Dashboards;using AnaliticAsd.Infrastructure.Persistence;using Microsoft.AspNetCore.Authorization;using Microsoft.AspNetCore.Mvc;using Microsoft.EntityFrameworkCore;
namespace AnaliticAsd.Controllers;
[ApiController,Authorize(Roles="Owner,Admin"),Route("api/v1/branding")]
public sealed class BrandingController(AnalitiAdsDbContext db,ICurrentTenant tenant,TimeProvider clock):ControllerBase
{
 [HttpGet("agency")]public async Task<BrandProfileResponse> Agency(CancellationToken ct)=>Map(await db.BrandProfiles.AsNoTracking().SingleOrDefaultAsync(x=>x.AgencyId==tenant.AgencyId&&x.ClientId==null,ct));
 [HttpPut("agency")]public Task<BrandProfileResponse> AgencyPut(BrandProfileRequest r,CancellationToken ct)=>Save(null,r,ct);
 [HttpGet("clients/{clientId:guid}")]public async Task<BrandProfileResponse> Client(Guid clientId,CancellationToken ct){await RequiredClient(clientId,ct);return Map(await db.BrandProfiles.AsNoTracking().SingleOrDefaultAsync(x=>x.AgencyId==tenant.AgencyId&&x.ClientId==clientId,ct));}
 [HttpPut("clients/{clientId:guid}")]public async Task<BrandProfileResponse> ClientPut(Guid clientId,BrandProfileRequest r,CancellationToken ct){await RequiredClient(clientId,ct);return await Save(clientId,r,ct);}
 private async Task<BrandProfileResponse> Save(Guid? clientId,BrandProfileRequest r,CancellationToken ct){var x=await db.BrandProfiles.SingleOrDefaultAsync(v=>v.AgencyId==tenant.AgencyId&&v.ClientId==clientId,ct);if(x is null){x=BrandProfile.Create(tenant.AgencyId,clientId,r.LogoUrl,r.PrimaryColor,r.SecondaryColor,r.BackgroundColor,r.FontFamily,clock.GetUtcNow());db.Add(x);}else x.Update(r.LogoUrl,r.PrimaryColor,r.SecondaryColor,r.BackgroundColor,r.FontFamily,clock.GetUtcNow());await db.SaveChangesAsync(ct);return Map(x);}
 private async Task RequiredClient(Guid id,CancellationToken ct){if(!await db.Clients.AnyAsync(x=>x.Id==id&&x.AgencyId==tenant.AgencyId,ct))throw new EntityNotFoundException("Client was not found.");}
 private static BrandProfileResponse Map(BrandProfile?x)=>x is null?new(null,"#8054D8","#3C9CA0","#F5F6F8","Arial"):new(x.LogoUrl,x.PrimaryColor,x.SecondaryColor,x.BackgroundColor,x.FontFamily);
}
public sealed record BrandProfileRequest(string? LogoUrl,string PrimaryColor,string SecondaryColor,string BackgroundColor,string FontFamily);
public sealed record BrandProfileResponse(string? LogoUrl,string PrimaryColor,string SecondaryColor,string BackgroundColor,string FontFamily);
