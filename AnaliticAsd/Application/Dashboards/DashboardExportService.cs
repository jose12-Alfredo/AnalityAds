using System.Text.Json;
using AnaliticAsd.Application.Common;
using AnaliticAsd.Application.Identity;
using AnaliticAsd.Domain.Dashboards;
using AnaliticAsd.Domain.Identity;
using AnaliticAsd.Infrastructure.Persistence;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace AnaliticAsd.Application.Dashboards;

public sealed record DashboardExportModel(Guid Id,Guid DashboardId,int PublicationNumber,string Format,string Status,
    string? FileName,string? ErrorCode,DateTimeOffset CreatedAtUtc,DateTimeOffset? CompletedAtUtc);
public sealed record DashboardDeliveryScheduleModel(Guid Id,Guid DashboardId,IReadOnlyList<string> Recipients,string Format,
    string Frequency,int HourUtc,int? DayOfWeek,int? DayOfMonth,bool IsEnabled,DateTimeOffset NextRunAtUtc,
    DateTimeOffset? LastSucceededAtUtc,string? LastErrorCode,Guid Version);
public sealed record DashboardExportFile(string FileName,string ContentType,byte[] Content);

public sealed class DashboardExportService(AnalitiAdsDbContext db,ICurrentTenant tenant,TimeProvider clock,
    IPasswordHasher<DashboardShareLink> passwords)
{
    public async Task<DashboardExportModel> RequestAsync(Guid dashboardId,string format,CancellationToken ct)
    {
        var dashboard=await RequiredDashboard(dashboardId,ct);var publication=dashboard.CurrentPublicationNumber??throw new ConflictException("Publish the dashboard before exporting it.");
        var job=DashboardExport.Create(dashboard,publication,ParseFormat(format),clock.GetUtcNow(),userId:tenant.UserId);db.Add(job);await db.SaveChangesAsync(ct);return Map(job);
    }
    public async Task<DashboardExportModel> RequestPublicAsync(string token,string? password,string? email,string format,CancellationToken ct)
    {
        if(string.IsNullOrWhiteSpace(token)||token.Length!=43)throw NotFound();var hash=DashboardShareService.Hash(token);
        var link=await db.DashboardShareLinks.SingleOrDefaultAsync(x=>x.TokenHash==hash,ct);var now=clock.GetUtcNow();
        if(link is null||!link.IsAvailable(now)||!link.AllowExport)throw NotFound();
        if(link.PasswordHash is not null&&passwords.VerifyHashedPassword(link,link.PasswordHash,password??"")==PasswordVerificationResult.Failed)throw NotFound();
        if(link.RecipientEmail is not null&&!string.Equals(link.RecipientEmail,email?.Trim(),StringComparison.OrdinalIgnoreCase))throw NotFound();
        var dashboard=await db.Dashboards.SingleOrDefaultAsync(x=>x.Id==link.DashboardId,ct)??throw NotFound();var publication=dashboard.CurrentPublicationNumber??throw NotFound();
        var job=DashboardExport.Create(dashboard,publication,ParseFormat(format),now,shareLinkId:link.Id);db.Add(job);await db.SaveChangesAsync(ct);return Map(job);
    }
    public async Task<IReadOnlyList<DashboardExportModel>> ListAsync(Guid dashboardId,CancellationToken ct)
    {await RequiredDashboard(dashboardId,ct);return (await db.DashboardExports.AsNoTracking().Where(x=>x.AgencyId==tenant.AgencyId&&x.DashboardId==dashboardId).OrderByDescending(x=>x.CreatedAtUtc).Take(100).ToArrayAsync(ct)).Select(Map).ToArray();}
    public async Task<DashboardExportModel> StatusPublicAsync(string token,Guid exportId,CancellationToken ct)
    {var link=await RequiredPublicLink(token,ct);var job=await db.DashboardExports.AsNoTracking().SingleOrDefaultAsync(x=>x.Id==exportId&&x.ShareLinkId==link.Id,ct)??throw NotFound();return Map(job);}
    public async Task<DashboardExportFile> DownloadAsync(Guid dashboardId,Guid exportId,CancellationToken ct)
    {await RequiredDashboard(dashboardId,ct);var job=await db.DashboardExports.AsNoTracking().SingleOrDefaultAsync(x=>x.Id==exportId&&x.DashboardId==dashboardId&&x.AgencyId==tenant.AgencyId,ct)??throw new EntityNotFoundException("Export was not found.");return File(job);}
    public async Task<DashboardExportFile> DownloadPublicAsync(string token,Guid exportId,CancellationToken ct)
    {var link=await RequiredPublicLink(token,ct);if(!link.AllowExport)throw NotFound();var job=await db.DashboardExports.AsNoTracking().SingleOrDefaultAsync(x=>x.Id==exportId&&x.ShareLinkId==link.Id,ct)??throw NotFound();return File(job);}
    public async Task<IReadOnlyList<DashboardDeliveryScheduleModel>> ListSchedulesAsync(Guid dashboardId,CancellationToken ct)
    {RequireManager();await RequiredDashboard(dashboardId,ct);return(await db.DashboardDeliverySchedules.AsNoTracking().Where(x=>x.AgencyId==tenant.AgencyId&&x.DashboardId==dashboardId).OrderBy(x=>x.NextRunAtUtc).ToArrayAsync(ct)).Select(Map).ToArray();}
    public async Task<DashboardDeliveryScheduleModel> CreateScheduleAsync(Guid dashboardId,IReadOnlyList<string> recipients,string format,string frequency,int hourUtc,int? dayOfWeek,int? dayOfMonth,bool enabled,CancellationToken ct)
    {
        RequireManager();var dashboard=await RequiredDashboard(dashboardId,ct);if(dashboard.CurrentPublicationNumber is null)throw new ConflictException("Publish the dashboard before scheduling delivery.");
        var emails=NormalizeEmails(recipients);var schedule=DashboardDeliverySchedule.Create(dashboard,tenant.UserId,JsonSerializer.Serialize(emails),ParseFormat(format),Enum.Parse<DashboardDeliveryFrequency>(frequency,true),hourUtc,dayOfWeek,dayOfMonth,enabled,clock.GetUtcNow());db.Add(schedule);await db.SaveChangesAsync(ct);return Map(schedule);
    }
    public async Task DeleteScheduleAsync(Guid dashboardId,Guid scheduleId,CancellationToken ct){RequireManager();await RequiredDashboard(dashboardId,ct);var value=await db.DashboardDeliverySchedules.SingleOrDefaultAsync(x=>x.Id==scheduleId&&x.DashboardId==dashboardId&&x.AgencyId==tenant.AgencyId,ct)??throw new EntityNotFoundException("Delivery schedule was not found.");db.Remove(value);await db.SaveChangesAsync(ct);}
    private async Task<Dashboard> RequiredDashboard(Guid id,CancellationToken ct)=>await new AuthorizedData(db,tenant).Dashboards.SingleOrDefaultAsync(x=>x.Id==id,ct)??throw new EntityNotFoundException("Dashboard was not found.");
    private async Task<DashboardShareLink> RequiredPublicLink(string token,CancellationToken ct){if(string.IsNullOrWhiteSpace(token)||token.Length!=43)throw NotFound();var value=await db.DashboardShareLinks.AsNoTracking().SingleOrDefaultAsync(x=>x.TokenHash==DashboardShareService.Hash(token),ct);return value is not null&&value.IsAvailable(clock.GetUtcNow())?value:throw NotFound();}
    private void RequireManager(){if(tenant.Role is not(nameof(AgencyRole.Owner)or nameof(AgencyRole.Admin)))throw new ForbiddenException("Only Owner and Admin can manage scheduled deliveries.");}
    private static DashboardExportFormat ParseFormat(string value)=>Enum.TryParse<DashboardExportFormat>(value,true,out var parsed)?parsed:throw new ArgumentException("Format must be Pdf, Csv or Excel.");
    private static string[] NormalizeEmails(IReadOnlyList<string> values){if(values is null||values.Count is <1 or >20)throw new ArgumentException("Choose between 1 and 20 recipients.");var result=values.Select(x=>x.Trim().ToLowerInvariant()).Distinct().ToArray();if(result.Any(x=>!System.Net.Mail.MailAddress.TryCreate(x,out _)))throw new ArgumentException("A recipient email is invalid.");return result;}
    private static DashboardExportModel Map(DashboardExport x)=>new(x.Id,x.DashboardId,x.PublicationNumber,x.Format.ToString(),x.Status.ToString(),x.FileName,x.ErrorCode,x.CreatedAtUtc,x.CompletedAtUtc);
    private static DashboardDeliveryScheduleModel Map(DashboardDeliverySchedule x)=>new(x.Id,x.DashboardId,JsonSerializer.Deserialize<string[]>(x.RecipientsJson)??[],x.Format.ToString(),x.Frequency.ToString(),x.HourUtc,x.DayOfWeek,x.DayOfMonth,x.IsEnabled,x.NextRunAtUtc,x.LastSucceededAtUtc,x.LastErrorCode,x.Version);
    private static DashboardExportFile File(DashboardExport x)=>x.Status==DashboardExportStatus.Completed&&x.Content is not null?new(x.FileName!,x.ContentType!,x.Content):throw new ConflictException("The export is not ready.");
    private static EntityNotFoundException NotFound()=>new("Export access was not found.");
}
