using AnaliticAsd.Domain.Clients;
using AnaliticAsd.Domain.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AnaliticAsd.Infrastructure.Persistence.Configurations;

internal sealed class ClientAccessConfiguration : IEntityTypeConfiguration<ClientAccess>
{
    public void Configure(EntityTypeBuilder<ClientAccess> builder)
    {
        builder.ToTable("client_accesses");
        builder.HasKey(x => new { x.AgencyId, x.ClientId, x.UserId });
        builder.Property(x => x.AgencyId).HasColumnName("agency_id");
        builder.Property(x => x.ClientId).HasColumnName("client_id");
        builder.Property(x => x.UserId).HasColumnName("user_id");
        builder.Property(x => x.GrantedAtUtc).HasColumnName("granted_at_utc");
        builder.HasOne<Client>().WithMany().HasForeignKey(x => new { x.ClientId, x.AgencyId })
            .HasPrincipalKey(x => new { x.Id, x.AgencyId }).OnDelete(DeleteBehavior.Cascade);
        builder.HasOne<Membership>().WithMany().HasForeignKey(x => new { x.AgencyId, x.UserId }).OnDelete(DeleteBehavior.Cascade);
    }
}

internal sealed class ClientInvitationConfiguration : IEntityTypeConfiguration<ClientInvitation>
{
    public void Configure(EntityTypeBuilder<ClientInvitation> builder)
    {
        builder.ToTable("client_invitations");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).HasColumnName("id");
        builder.Property(x => x.AgencyId).HasColumnName("agency_id");
        builder.Property(x => x.ClientId).HasColumnName("client_id");
        builder.Property(x => x.CreatedByUserId).HasColumnName("created_by_user_id");
        builder.Property(x => x.Email).HasColumnName("email").HasMaxLength(User.MaxEmailLength);
        builder.Property(x => x.NormalizedEmail).HasColumnName("normalized_email").HasMaxLength(User.MaxEmailLength);
        builder.Property(x => x.TokenHash).HasColumnName("token_hash").HasMaxLength(64);
        builder.HasIndex(x => x.TokenHash).IsUnique();
        builder.HasIndex(x => new { x.AgencyId, x.ClientId });
        builder.Property(x => x.CreatedAtUtc).HasColumnName("created_at_utc");
        builder.Property(x => x.ExpiresAtUtc).HasColumnName("expires_at_utc");
        builder.Property(x => x.AcceptedAtUtc).HasColumnName("accepted_at_utc");
        builder.Property(x => x.RevokedAtUtc).HasColumnName("revoked_at_utc");
        builder.Property(x => x.Version).HasColumnName("version").IsConcurrencyToken();
        builder.HasOne<Client>().WithMany().HasForeignKey(x => new { x.ClientId, x.AgencyId })
            .HasPrincipalKey(x => new { x.Id, x.AgencyId }).OnDelete(DeleteBehavior.Cascade);
    }
}
