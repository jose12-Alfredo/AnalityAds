using AnaliticAsd.Domain.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AnaliticAsd.Infrastructure.Persistence.Configurations;

public sealed class McpConnectionConfiguration : IEntityTypeConfiguration<McpConnection>
{
    public void Configure(EntityTypeBuilder<McpConnection> b)
    {
        b.ToTable("mcp_connections"); b.HasKey(x => x.Id);
        b.Property(x => x.Id).HasColumnName("id"); b.Property(x => x.AgencyId).HasColumnName("agency_id"); b.Property(x => x.UserId).HasColumnName("user_id");
        b.Property(x => x.Name).HasColumnName("name").HasMaxLength(120).IsRequired(); b.Property(x => x.Scopes).HasColumnName("scopes").HasMaxLength(300).IsRequired();
        b.Property(x => x.CreatedAtUtc).HasColumnName("created_at_utc"); b.Property(x => x.ExpiresAtUtc).HasColumnName("expires_at_utc"); b.Property(x => x.RevokedAtUtc).HasColumnName("revoked_at_utc");
        b.HasIndex(x => new { x.AgencyId, x.UserId });
        b.HasOne<Agency>().WithMany().HasForeignKey(x => x.AgencyId).OnDelete(DeleteBehavior.Cascade);
        b.HasOne<User>().WithMany().HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.Cascade);
    }
}

public sealed class McpAuditEventConfiguration : IEntityTypeConfiguration<McpAuditEvent>
{
    public void Configure(EntityTypeBuilder<McpAuditEvent> b)
    {
        b.ToTable("mcp_audit_events"); b.HasKey(x => x.Id);
        b.Property(x => x.Id).HasColumnName("id"); b.Property(x => x.ConnectionId).HasColumnName("connection_id"); b.Property(x => x.AgencyId).HasColumnName("agency_id"); b.Property(x => x.UserId).HasColumnName("user_id");
        b.Property(x => x.Operation).HasColumnName("operation").HasMaxLength(100).IsRequired(); b.Property(x => x.Succeeded).HasColumnName("succeeded"); b.Property(x => x.OccurredAtUtc).HasColumnName("occurred_at_utc");
        b.HasIndex(x => new { x.ConnectionId, x.OccurredAtUtc });
        b.HasOne<McpConnection>().WithMany().HasForeignKey(x => x.ConnectionId).OnDelete(DeleteBehavior.Cascade);
    }
}
