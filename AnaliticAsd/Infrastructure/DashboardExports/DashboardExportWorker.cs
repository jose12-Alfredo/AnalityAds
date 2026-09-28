using System.Net;
using System.Net.Mail;
using System.Text.Json;
using AnaliticAsd.Domain.Dashboards;
using AnaliticAsd.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace AnaliticAsd.Infrastructure.DashboardExports;

public interface IDashboardDeliverySender { Task SendAsync(IReadOnlyList<string> recipients,string subject,string body,RenderedDashboardExport attachment,CancellationToken ct); }
public sealed class SmtpDashboardDeliverySender(IConfiguration configuration):IDashboardDeliverySender
{
 public async Task SendAsync(IReadOnlyList<string> recipients,string subject,string body,RenderedDashboardExport attachment,CancellationToken ct)
 {
  var host=configuration["Email:Smtp:Host"]??throw new InvalidOperationException("Email:Smtp:Host is not configured.");var from=configuration["Email:From"]??throw new InvalidOperationException("Email:From is not configured.");
  using var message=new MailMessage{From=new MailAddress(from),Subject=subject,Body=body};foreach(var recipient in recipients)message.To.Add(recipient);message.Attachments.Add(new Attachment(new MemoryStream(attachment.Content),attachment.FileName,attachment.ContentType));
  using var client=new SmtpClient(host,int.TryParse(configuration["Email:Smtp:Port"],out var port)?port:587){EnableSsl=!bool.TryParse(configuration["Email:Smtp:UseSsl"],out var ssl)||ssl};var user=configuration["Email:Smtp:Username"];if(!string.IsNullOrWhiteSpace(user))client.Credentials=new NetworkCredential(user,configuration["Email:Smtp:Password"]);
  ct.ThrowIfCancellationRequested();await client.SendMailAsync(message,ct);
 }
}

public sealed class DashboardExportWorker(IServiceScopeFactory scopes,TimeProvider clock,ILogger<DashboardExportWorker> logger):BackgroundService
{
 protected override async Task ExecuteAsync(CancellationToken stoppingToken){using var timer=new PeriodicTimer(TimeSpan.FromSeconds(10));do{try{await Run(stoppingToken);}catch(PostgresException e)when(e.SqlState==PostgresErrorCodes.UndefinedTable){logger.LogWarning("Dashboard export worker is disabled until the D7 migration is applied.");return;}catch(Exception e){logger.LogError(e,"Dashboard export worker iteration failed.");}}while(await timer.WaitForNextTickAsync(stoppingToken));}
 private async Task Run(CancellationToken ct)
 {
  await using var scope=scopes.CreateAsyncScope();var db=scope.ServiceProvider.GetRequiredService<AnalitiAdsDbContext>();var now=clock.GetUtcNow();
  var schedules=await db.DashboardDeliverySchedules.Where(x=>x.IsEnabled&&x.NextRunAtUtc<=now).OrderBy(x=>x.NextRunAtUtc).Take(10).ToArrayAsync(ct);
  foreach(var schedule in schedules){var dashboard=await db.Dashboards.SingleOrDefaultAsync(x=>x.Id==schedule.DashboardId,ct);if(dashboard?.CurrentPublicationNumber is not int publication){schedule.Fail("DashboardNotPublished",now);continue;}if(!await db.DashboardExports.AnyAsync(x=>x.DeliveryScheduleId==schedule.Id&&x.CreatedAtUtc>=schedule.NextRunAtUtc.AddMinutes(-1),ct))db.Add(DashboardExport.Create(dashboard,publication,schedule.Format,now,scheduleId:schedule.Id));}await db.SaveChangesAsync(ct);
  var jobs=await db.DashboardExports.Where(x=>x.Status==DashboardExportStatus.Pending).OrderBy(x=>x.CreatedAtUtc).Take(10).ToArrayAsync(ct);
  foreach(var job in jobs){job.Start(clock.GetUtcNow());await db.SaveChangesAsync(ct);try{var dashboard=await db.Dashboards.AsNoTracking().SingleAsync(x=>x.Id==job.DashboardId,ct);var version=await db.DashboardVersions.AsNoTracking().SingleAsync(x=>x.DashboardId==job.DashboardId&&x.PublicationNumber==job.PublicationNumber,ct);var rendered=await scope.ServiceProvider.GetRequiredService<IDashboardExportRenderer>().RenderAsync(dashboard,version,job.Format,ct);job.Complete(rendered.FileName,rendered.ContentType,rendered.Content,clock.GetUtcNow());if(job.DeliveryScheduleId is Guid scheduleId){var schedule=await db.DashboardDeliverySchedules.SingleAsync(x=>x.Id==scheduleId,ct);var recipients=JsonSerializer.Deserialize<string[]>(schedule.RecipientsJson)??[];await scope.ServiceProvider.GetRequiredService<IDashboardDeliverySender>().SendAsync(recipients,$"Dashboard: {dashboard.Title}","Adjuntamos la publicación programada de tu dashboard.",rendered,ct);schedule.Complete(clock.GetUtcNow());}}catch(Exception e){logger.LogWarning(e,"Dashboard export {ExportId} failed.",job.Id);job.Fail(e.GetType().Name,clock.GetUtcNow());if(job.DeliveryScheduleId is Guid scheduleId){var schedule=await db.DashboardDeliverySchedules.SingleOrDefaultAsync(x=>x.Id==scheduleId,ct);schedule?.Fail(e.GetType().Name,clock.GetUtcNow());}}await db.SaveChangesAsync(ct);}
 }
}
