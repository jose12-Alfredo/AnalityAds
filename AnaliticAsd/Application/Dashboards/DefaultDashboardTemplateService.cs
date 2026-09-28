using System.Text.Json;using AnaliticAsd.Application.Identity;using AnaliticAsd.Domain.Dashboards;using AnaliticAsd.Domain.Identity;using AnaliticAsd.Infrastructure.Persistence;using Microsoft.EntityFrameworkCore;
namespace AnaliticAsd.Application.Dashboards;
public sealed class DefaultDashboardTemplateService(AnalitiAdsDbContext db,IDashboardDefinitionValidator validator,ICurrentTenant tenant,TimeProvider clock)
{
 private static readonly (string Name,string Metric)[] Defaults=[("Ventas","purchaseValue"),("Captación de leads","leads"),("Mensajes","conversions"),("Reconocimiento de marca","impressions"),("Resumen multicanal","spend")];
 public async Task<int> InstallAsync(CancellationToken ct){if(tenant.Role is not(nameof(AgencyRole.Owner)or nameof(AgencyRole.Admin)))throw new UnauthorizedAccessException();var names=await db.DashboardTemplates.Where(x=>x.AgencyId==tenant.AgencyId&&x.ClientId==null).Select(x=>x.Name).ToArrayAsync(ct);var count=0;foreach(var item in Defaults.Where(x=>!names.Contains(x.Name))){var json=Template(item.Name,item.Metric);var valid=validator.ValidateTemplate(json);db.Add(DashboardTemplate.Create(tenant.AgencyId,null,item.Name,$"Plantilla inicial editable para {item.Name.ToLowerInvariant()}.",valid.SchemaVersion,valid.Json,tenant.UserId,clock.GetUtcNow()));count++;}await db.SaveChangesAsync(ct);return count;}
 private static string Template(string title,string metric)
 {
  object[] components=
  [
   new { id=Guid.NewGuid(),type="text",x=48,y=40,width=700,height=90,zIndex=1,isLocked=false,data=new{sources=Array.Empty<object>(),configuration=new{text=title}},style=new{color="#17202A",fontSize=34,background="transparent"}},
   new { id=Guid.NewGuid(),type="kpiCard",x=48,y=160,width=300,height=180,zIndex=2,isLocked=false,data=new{sources=new[]{new{sourceSlot="primary"}},configuration=new{title,metric,metrics=new[]{metric}}},style=new{background="#FFFFFF",color="#17202A"}}
  ];
  return JsonSerializer.Serialize(new{schemaVersion=1,pages=new[]{new{id=Guid.NewGuid(),name="Resumen",order=0,width=1440,height=900,background="#F5F6F8",components}}});
 }
}
