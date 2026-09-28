using AnaliticAsd.Domain.Advertising;
using AnaliticAsd.Domain.Clients;
using AnaliticAsd.Domain.Identity;
using AnaliticAsd.Domain.Reports;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AnaliticAsd.Infrastructure.Persistence.Configurations;

internal sealed class ReportConfiguration : IEntityTypeConfiguration<Report>
{
    public void Configure(EntityTypeBuilder<Report> b)
    {
        b.ToTable("reports");
        b.HasKey(x => x.Id);
        b.Property(x => x.Id).HasColumnName("id");
        b.Property(x => x.AgencyId).HasColumnName("agency_id");
        b.Property(x => x.ClientId).HasColumnName("client_id");
        b.Property(x => x.AdAccountId).HasColumnName("ad_account_id");
        b.Property(x => x.CreatedByUserId).HasColumnName("created_by_user_id");
        b.Property(x => x.Title).HasColumnName("title").HasMaxLength(Report.MaxTitleLength).IsRequired();
        b.Property(x => x.Since).HasColumnName("since");
        b.Property(x => x.Until).HasColumnName("until");
        b.Property(x => x.CreatedAtUtc).HasColumnName("created_at_utc");
        b.Property(x => x.SchemaVersion).HasColumnName("schema_version");
        b.Property(x => x.SnapshotJson).HasColumnName("snapshot_json").HasColumnType("jsonb").IsRequired();
        b.Property(x => x.SnapshotHash).HasColumnName("snapshot_hash").HasMaxLength(64).IsRequired();
        b.HasIndex(x => new { x.AgencyId, x.ClientId, x.CreatedAtUtc });
        b.HasOne<Client>().WithMany().HasForeignKey(x => new { x.ClientId, x.AgencyId })
            .HasPrincipalKey(x => new { x.Id, x.AgencyId }).OnDelete(DeleteBehavior.Restrict);
        b.HasOne<AdAccount>().WithMany().HasForeignKey(x => x.AdAccountId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne<User>().WithMany().HasForeignKey(x => x.CreatedByUserId).OnDelete(DeleteBehavior.Restrict);
    }
}
