using AnaliticAsd.Domain.Dashboards;
using AnaliticAsd.Domain.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AnaliticAsd.Infrastructure.Persistence.Configurations;

internal sealed class DashboardExportConfiguration : IEntityTypeConfiguration<DashboardExport>
{
    public void Configure(EntityTypeBuilder<DashboardExport> b)
    {
        b.ToTable("dashboard_exports"); b.HasKey(x=>x.Id);
        b.Property(x=>x.Id).HasColumnName("id"); b.Property(x=>x.DashboardId).HasColumnName("dashboard_id");
        b.Property(x=>x.AgencyId).HasColumnName("agency_id"); b.Property(x=>x.ClientId).HasColumnName("client_id");
        b.Property(x=>x.RequestedByUserId).HasColumnName("requested_by_user_id"); b.Property(x=>x.ShareLinkId).HasColumnName("share_link_id");
        b.Property(x=>x.DeliveryScheduleId).HasColumnName("delivery_schedule_id"); b.Property(x=>x.PublicationNumber).HasColumnName("publication_number");
        b.Property(x=>x.Format).HasColumnName("format").HasConversion<string>().HasMaxLength(16);
        b.Property(x=>x.Status).HasColumnName("status").HasConversion<string>().HasMaxLength(16);
        b.Property(x=>x.FileName).HasColumnName("file_name").HasMaxLength(240); b.Property(x=>x.ContentType).HasColumnName("content_type").HasMaxLength(120);
        b.Property(x=>x.Content).HasColumnName("content"); b.Property(x=>x.ErrorCode).HasColumnName("error_code").HasMaxLength(120);
        b.Property(x=>x.CreatedAtUtc).HasColumnName("created_at_utc"); b.Property(x=>x.StartedAtUtc).HasColumnName("started_at_utc"); b.Property(x=>x.CompletedAtUtc).HasColumnName("completed_at_utc");
        b.HasIndex(x=>new{x.AgencyId,x.DashboardId,x.CreatedAtUtc}); b.HasIndex(x=>new{x.Status,x.CreatedAtUtc});
        b.HasOne<Dashboard>().WithMany().HasForeignKey(x=>x.DashboardId).OnDelete(DeleteBehavior.Cascade);
        b.HasOne<User>().WithMany().HasForeignKey(x=>x.RequestedByUserId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne<DashboardShareLink>().WithMany().HasForeignKey(x=>x.ShareLinkId).OnDelete(DeleteBehavior.SetNull);
        b.HasOne<DashboardDeliverySchedule>().WithMany().HasForeignKey(x=>x.DeliveryScheduleId).OnDelete(DeleteBehavior.SetNull);
    }
}

internal sealed class DashboardDeliveryScheduleConfiguration : IEntityTypeConfiguration<DashboardDeliverySchedule>
{
    public void Configure(EntityTypeBuilder<DashboardDeliverySchedule> b)
    {
        b.ToTable("dashboard_delivery_schedules"); b.HasKey(x=>x.Id);
        b.Property(x=>x.Id).HasColumnName("id"); b.Property(x=>x.DashboardId).HasColumnName("dashboard_id");
        b.Property(x=>x.AgencyId).HasColumnName("agency_id"); b.Property(x=>x.ClientId).HasColumnName("client_id"); b.Property(x=>x.CreatedByUserId).HasColumnName("created_by_user_id");
        b.Property(x=>x.RecipientsJson).HasColumnName("recipients_json").HasColumnType("jsonb"); b.Property(x=>x.Format).HasColumnName("format").HasConversion<string>().HasMaxLength(16);
        b.Property(x=>x.Frequency).HasColumnName("frequency").HasConversion<string>().HasMaxLength(16); b.Property(x=>x.HourUtc).HasColumnName("hour_utc");
        b.Property(x=>x.DayOfWeek).HasColumnName("day_of_week"); b.Property(x=>x.DayOfMonth).HasColumnName("day_of_month"); b.Property(x=>x.IsEnabled).HasColumnName("is_enabled");
        b.Property(x=>x.NextRunAtUtc).HasColumnName("next_run_at_utc"); b.Property(x=>x.LastRunAtUtc).HasColumnName("last_run_at_utc"); b.Property(x=>x.LastSucceededAtUtc).HasColumnName("last_succeeded_at_utc");
        b.Property(x=>x.LastErrorCode).HasColumnName("last_error_code").HasMaxLength(120); b.Property(x=>x.Version).HasColumnName("version").IsConcurrencyToken();
        b.HasIndex(x=>new{x.IsEnabled,x.NextRunAtUtc}); b.HasIndex(x=>new{x.AgencyId,x.DashboardId});
        b.HasOne<Dashboard>().WithMany().HasForeignKey(x=>x.DashboardId).OnDelete(DeleteBehavior.Cascade);
        b.HasOne<User>().WithMany().HasForeignKey(x=>x.CreatedByUserId).OnDelete(DeleteBehavior.Restrict);
    }
}
