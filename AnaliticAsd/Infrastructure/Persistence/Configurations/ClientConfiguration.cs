using AnaliticAsd.Domain.Clients;
using AnaliticAsd.Domain.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AnaliticAsd.Infrastructure.Persistence.Configurations;

internal sealed class ClientConfiguration : IEntityTypeConfiguration<Client>
{
    public void Configure(EntityTypeBuilder<Client> builder)
    {
        builder.ToTable("clients");
        builder.HasKey(client => client.Id);
        builder.HasAlternateKey(client => new { client.Id, client.AgencyId });
        builder.Property(client => client.Id).HasColumnName("id");
        builder.Property(client => client.AgencyId).HasColumnName("agency_id").IsRequired();
        builder.Property(client => client.Name)
            .HasColumnName("name")
            .HasMaxLength(Client.MaxNameLength)
            .IsRequired();
        builder.Property(client => client.IsActive).HasColumnName("is_active").IsRequired();
        builder.Property(client => client.CreatedAtUtc).HasColumnName("created_at_utc").IsRequired();
        builder.Property(client => client.UpdatedAtUtc).HasColumnName("updated_at_utc").IsRequired();
        builder.HasOne<Agency>().WithMany().HasForeignKey(client => client.AgencyId).OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(client => new { client.AgencyId, client.Name }).HasDatabaseName("ix_clients_agency_id_name");
    }
}
