using AnaliticAsd.Domain.Clients;
using AnaliticAsd.Domain.DataSources;
using AnaliticAsd.Domain.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AnaliticAsd.Infrastructure.Persistence.Configurations;

internal sealed class ProviderConnectionConfiguration : IEntityTypeConfiguration<ProviderConnection>
{
    public void Configure(EntityTypeBuilder<ProviderConnection> builder)
    {
        builder.ToTable("provider_connections");
        builder.HasKey(x => x.Id);
        builder.HasAlternateKey(x => new { x.Id, x.AgencyId });
        builder.Property(x => x.Id).HasColumnName("id");
        builder.Property(x => x.AgencyId).HasColumnName("agency_id");
        builder.Property(x => x.Provider).HasConversion<string>().HasColumnName("provider").HasMaxLength(40);
        builder.Property(x => x.DisplayName).HasColumnName("display_name").HasMaxLength(ProviderConnection.MaxDisplayNameLength);
        builder.Property(x => x.ExternalSubjectId).HasColumnName("external_subject_id").HasMaxLength(ProviderConnection.MaxExternalSubjectIdLength);
        builder.Property(x => x.ProtectedCredentialPayload).HasColumnName("protected_credential_payload").HasMaxLength(ProviderConnection.MaxProtectedCredentialLength);
        builder.Property(x => x.ExpiresAtUtc).HasColumnName("expires_at_utc");
        builder.Property(x => x.Status).HasConversion<string>().HasColumnName("status").HasMaxLength(40);
        builder.Property(x => x.AuthorizedAtUtc).HasColumnName("authorized_at_utc");
        builder.Property(x => x.LastSucceededAtUtc).HasColumnName("last_succeeded_at_utc");
        builder.Property(x => x.LastErrorCode).HasColumnName("last_error_code").HasMaxLength(ProviderConnection.MaxErrorCodeLength);
        builder.Property(x => x.CreatedAtUtc).HasColumnName("created_at_utc");
        builder.Property(x => x.UpdatedAtUtc).HasColumnName("updated_at_utc");
        builder.Property(x => x.Version).HasColumnName("version").IsConcurrencyToken();
        builder.HasIndex(x => new { x.AgencyId, x.Provider });
        builder.HasOne<Agency>().WithMany().HasForeignKey(x => x.AgencyId).OnDelete(DeleteBehavior.Cascade);
    }
}

internal sealed class DataSourceConfiguration : IEntityTypeConfiguration<DataSource>
{
    public void Configure(EntityTypeBuilder<DataSource> builder)
    {
        builder.ToTable("data_sources");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).HasColumnName("id");
        builder.Property(x => x.AgencyId).HasColumnName("agency_id");
        builder.Property(x => x.ClientId).HasColumnName("client_id");
        builder.Property(x => x.ProviderConnectionId).HasColumnName("provider_connection_id");
        builder.Property(x => x.Provider).HasConversion<string>().HasColumnName("provider").HasMaxLength(40);
        builder.Property(x => x.SourceType).HasConversion<string>().HasColumnName("source_type").HasMaxLength(40);
        builder.Property(x => x.ExternalId).HasColumnName("external_id").HasMaxLength(DataSource.MaxExternalIdLength);
        builder.Property(x => x.Name).HasColumnName("name").HasMaxLength(DataSource.MaxNameLength);
        builder.Property(x => x.Currency).HasColumnName("currency").HasMaxLength(3).IsFixedLength();
        builder.Property(x => x.TimeZone).HasColumnName("time_zone").HasMaxLength(DataSource.MaxTimeZoneLength);
        builder.Property(x => x.IsActive).HasColumnName("is_active");
        builder.Property(x => x.AvailableSince).HasColumnName("available_since");
        builder.Property(x => x.AvailableUntil).HasColumnName("available_until");
        builder.Property(x => x.LastSyncedAtUtc).HasColumnName("last_synced_at_utc");
        builder.Property(x => x.ArchivedAtUtc).HasColumnName("archived_at_utc");
        builder.Property(x => x.CreatedAtUtc).HasColumnName("created_at_utc");
        builder.Property(x => x.UpdatedAtUtc).HasColumnName("updated_at_utc");
        builder.Property(x => x.Version).HasColumnName("version").IsConcurrencyToken();
        builder.HasIndex(x => new { x.AgencyId, x.Provider, x.SourceType, x.ExternalId }).IsUnique();
        builder.HasIndex(x => new { x.AgencyId, x.ClientId });
        builder.HasAlternateKey(x => new { x.Id, x.ClientId });
        builder.HasOne<Agency>().WithMany().HasForeignKey(x => x.AgencyId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<Client>().WithMany().HasForeignKey(x => new { x.ClientId, x.AgencyId })
            .HasPrincipalKey(x => new { x.Id, x.AgencyId }).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<ProviderConnection>().WithMany()
            .HasForeignKey(x => new { x.ProviderConnectionId, x.AgencyId })
            .HasPrincipalKey(x => new { x.Id, x.AgencyId })
            .OnDelete(DeleteBehavior.Restrict);
    }
}
