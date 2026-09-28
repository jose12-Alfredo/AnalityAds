using AnaliticAsd.Domain.Identity;
using AnaliticAsd.Domain.Meta;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AnaliticAsd.Infrastructure.Persistence.Configurations;

internal sealed class MetaConnectionConfiguration : IEntityTypeConfiguration<MetaConnection>
{
    public void Configure(EntityTypeBuilder<MetaConnection> builder)
    {
        builder.ToTable("meta_connections");
        builder.HasKey(x => x.AgencyId);
        builder.Property(x => x.AgencyId).HasColumnName("agency_id");
        builder.Property(x => x.ProtectedAccessToken).HasColumnName("protected_access_token").HasMaxLength(4096).IsRequired();
        builder.Property(x => x.ExpiresAtUtc).HasColumnName("expires_at_utc");
        builder.Property(x => x.CreatedAtUtc).HasColumnName("created_at_utc").IsRequired();
        builder.Property(x => x.UpdatedAtUtc).HasColumnName("updated_at_utc").IsRequired();
        builder.HasOne<Agency>().WithOne().HasForeignKey<MetaConnection>(x => x.AgencyId).OnDelete(DeleteBehavior.Cascade);
    }
}
