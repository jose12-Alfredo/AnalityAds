using AnaliticAsd.Domain.Advertising;
using AnaliticAsd.Domain.Clients;
using AnaliticAsd.Domain.DataSources;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AnaliticAsd.Infrastructure.Persistence.Configurations;

internal sealed class AdAccountConfiguration : IEntityTypeConfiguration<AdAccount>
{
    public void Configure(EntityTypeBuilder<AdAccount> builder)
    {
        builder.ToTable("ad_accounts");
        builder.HasKey(account => account.Id);
        builder.Property(account => account.Id).HasColumnName("id");
        builder.Property(account => account.ClientId).HasColumnName("client_id").IsRequired();
        builder.Property(account => account.DataSourceId).HasColumnName("data_source_id").IsRequired();
        builder.Property(account => account.MetaAccountId)
            .HasConversion(id => id.Value, value => MetaAdAccountId.Parse(value))
            .HasColumnName("meta_account_id")
            .HasMaxLength(MetaAdAccountId.MaxLength)
            .IsRequired();
        builder.Property(account => account.Name)
            .HasColumnName("name")
            .HasMaxLength(AdAccount.MaxNameLength)
            .IsRequired();
        builder.Property(account => account.Currency)
            .HasConversion(currency => currency.Value, value => CurrencyCode.Parse(value))
            .HasColumnName("currency")
            .HasMaxLength(CurrencyCode.Length)
            .IsFixedLength()
            .IsRequired();
        builder.Property(account => account.TimeZone)
            .HasConversion(timeZone => timeZone.Value, value => MetaTimeZoneId.Parse(value))
            .HasColumnName("time_zone")
            .HasMaxLength(MetaTimeZoneId.MaxLength)
            .IsRequired();
        builder.Property(account => account.ConnectionStatus)
            .HasConversion<string>()
            .HasColumnName("connection_status")
            .HasMaxLength(40)
            .IsRequired();
        builder.Property(account => account.IsActive).HasColumnName("is_active").IsRequired();
        builder.Property(account => account.CreatedAtUtc).HasColumnName("created_at_utc").IsRequired();
        builder.Property(account => account.UpdatedAtUtc).HasColumnName("updated_at_utc").IsRequired();
        builder.HasIndex(account => account.MetaAccountId)
            .IsUnique()
            .HasDatabaseName("ux_ad_accounts_meta_account_id");
        builder.HasIndex(account => account.ClientId).HasDatabaseName("ix_ad_accounts_client_id");
        builder.HasIndex(account => account.DataSourceId).IsUnique().HasDatabaseName("ux_ad_accounts_data_source_id");
        builder.HasOne(account => account.DataSource)
            .WithOne()
            .HasForeignKey<AdAccount>(account => new { account.DataSourceId, account.ClientId })
            .HasPrincipalKey<DataSource>(source => new { source.Id, source.ClientId })
            .OnDelete(DeleteBehavior.Cascade);
        builder.HasOne<Client>()
            .WithMany()
            .HasForeignKey(account => account.ClientId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
