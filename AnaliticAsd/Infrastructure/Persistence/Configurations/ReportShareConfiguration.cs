using AnaliticAsd.Domain.Identity;
using AnaliticAsd.Domain.Reports;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AnaliticAsd.Infrastructure.Persistence.Configurations;

internal sealed class ReportShareLinkConfiguration : IEntityTypeConfiguration<ReportShareLink>
{
    public void Configure(EntityTypeBuilder<ReportShareLink> b)
    {
        b.ToTable("report_share_links");
        b.HasKey(x => x.Id);
        b.Property(x => x.Id).HasColumnName("id");
        b.Property(x => x.ReportId).HasColumnName("report_id");
        b.Property(x => x.AgencyId).HasColumnName("agency_id");
        b.Property(x => x.ClientId).HasColumnName("client_id");
        b.Property(x => x.CreatedByUserId).HasColumnName("created_by_user_id");
        b.Property(x => x.TokenHash).HasColumnName("token_hash").HasMaxLength(64).IsRequired();
        b.Property(x => x.CreatedAtUtc).HasColumnName("created_at_utc");
        b.Property(x => x.ExpiresAtUtc).HasColumnName("expires_at_utc");
        b.Property(x => x.RevokedAtUtc).HasColumnName("revoked_at_utc");
        b.Property(x => x.Version).HasColumnName("version").IsConcurrencyToken();
        b.HasIndex(x => x.TokenHash).IsUnique();
        b.HasIndex(x => new { x.AgencyId, x.ReportId });
        b.HasOne<Report>().WithMany().HasForeignKey(x => x.ReportId).OnDelete(DeleteBehavior.Cascade);
        b.HasOne<User>().WithMany().HasForeignKey(x => x.CreatedByUserId).OnDelete(DeleteBehavior.Restrict);
    }
}

internal sealed class ReportShareAuditEventConfiguration : IEntityTypeConfiguration<ReportShareAuditEvent>
{
    public void Configure(EntityTypeBuilder<ReportShareAuditEvent> b)
    {
        b.ToTable("report_share_audit_events");
        b.HasKey(x => x.Id);
        b.Property(x => x.Id).HasColumnName("id");
        b.Property(x => x.ShareLinkId).HasColumnName("share_link_id");
        b.Property(x => x.Operation).HasColumnName("operation").HasMaxLength(20).IsRequired();
        b.Property(x => x.OccurredAtUtc).HasColumnName("occurred_at_utc");
        b.HasIndex(x => new { x.ShareLinkId, x.OccurredAtUtc });
        b.HasOne<ReportShareLink>().WithMany().HasForeignKey(x => x.ShareLinkId).OnDelete(DeleteBehavior.Cascade);
    }
}
