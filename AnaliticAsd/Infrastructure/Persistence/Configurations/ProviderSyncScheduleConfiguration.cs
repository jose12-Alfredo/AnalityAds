using AnaliticAsd.Domain.DataSources;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
namespace AnaliticAsd.Infrastructure.Persistence.Configurations;
internal sealed class ProviderSyncScheduleConfiguration : IEntityTypeConfiguration<ProviderSyncSchedule>
{
 public void Configure(EntityTypeBuilder<ProviderSyncSchedule> b) { b.ToTable("provider_sync_schedules"); b.HasKey(x=>x.Id); b.Property(x=>x.Id).HasColumnName("id"); b.Property(x=>x.AgencyId).HasColumnName("agency_id"); b.Property(x=>x.DataSourceId).HasColumnName("data_source_id"); b.Property(x=>x.IsEnabled).HasColumnName("is_enabled"); b.Property(x=>x.IntervalMinutes).HasColumnName("interval_minutes"); b.Property(x=>x.LookbackDays).HasColumnName("lookback_days"); b.Property(x=>x.NextRunAtUtc).HasColumnName("next_run_at_utc"); b.Property(x=>x.LockedUntilUtc).HasColumnName("locked_until_utc"); b.Property(x=>x.ConsecutiveFailures).HasColumnName("consecutive_failures"); b.Property(x=>x.LastStartedAtUtc).HasColumnName("last_started_at_utc"); b.Property(x=>x.LastSucceededAtUtc).HasColumnName("last_succeeded_at_utc"); b.Property(x=>x.LastErrorCode).HasColumnName("last_error_code").HasMaxLength(100); b.HasIndex(x=>x.DataSourceId).IsUnique(); b.HasIndex(x=>new{x.IsEnabled,x.NextRunAtUtc}); b.HasOne<DataSource>().WithMany().HasForeignKey(x=>x.DataSourceId).OnDelete(DeleteBehavior.Cascade); }
}
