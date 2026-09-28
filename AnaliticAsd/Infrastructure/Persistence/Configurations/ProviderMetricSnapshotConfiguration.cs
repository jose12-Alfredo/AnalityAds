using AnaliticAsd.Domain.DataSources;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AnaliticAsd.Infrastructure.Persistence.Configurations;
internal sealed class ProviderMetricSnapshotConfiguration : IEntityTypeConfiguration<ProviderMetricSnapshot>
{
    public void Configure(EntityTypeBuilder<ProviderMetricSnapshot> b)
    {
        b.ToTable("provider_metric_snapshots"); b.HasKey(x => x.Id);
        b.Property(x => x.Id).HasColumnName("id"); b.Property(x => x.AgencyId).HasColumnName("agency_id");
        b.Property(x => x.DataSourceId).HasColumnName("data_source_id"); b.Property(x => x.Date).HasColumnName("date");
        b.Property(x => x.DimensionKey).HasColumnName("dimension_key").HasMaxLength(300);
        b.Property(x => x.DimensionName).HasColumnName("dimension_name").HasMaxLength(300);
        b.Property(x => x.Spend).HasColumnName("spend").HasPrecision(20, 6); b.Property(x => x.Impressions).HasColumnName("impressions");
        b.Property(x => x.Clicks).HasColumnName("clicks"); b.Property(x => x.Conversions).HasColumnName("conversions").HasPrecision(28, 10);
        b.Property(x => x.ConversionValue).HasColumnName("conversion_value").HasPrecision(20, 6);
        b.Property(x => x.ActiveUsers).HasColumnName("active_users"); b.Property(x => x.Sessions).HasColumnName("sessions");
        b.Property(x => x.Views).HasColumnName("views"); b.Property(x => x.ObservedAtUtc).HasColumnName("observed_at_utc");
        b.HasIndex(x => new { x.DataSourceId, x.Date, x.DimensionKey }).IsUnique();
        b.HasOne<DataSource>().WithMany().HasForeignKey(x => x.DataSourceId).OnDelete(DeleteBehavior.Cascade);
    }
}
