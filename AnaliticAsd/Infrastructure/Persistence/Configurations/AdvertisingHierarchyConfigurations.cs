using AnaliticAsd.Domain.Advertising;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AnaliticAsd.Infrastructure.Persistence.Configurations;

internal sealed class CampaignConfiguration : IEntityTypeConfiguration<Campaign>
{
    public void Configure(EntityTypeBuilder<Campaign> b)
    {
        b.ToTable("campaigns"); b.HasKey(x => x.Id); b.Property(x => x.Id).HasColumnName("id"); b.Property(x => x.AdAccountId).HasColumnName("ad_account_id");
        Text(b.Property(x => x.MetaCampaignId).HasColumnName("meta_campaign_id")); Text(b.Property(x => x.Name).HasColumnName("name")); Text(b.Property(x => x.Objective).HasColumnName("objective")); Text(b.Property(x => x.ConfiguredStatus).HasColumnName("configured_status")); Text(b.Property(x => x.EffectiveStatus).HasColumnName("effective_status"));
        b.Property(x => x.StartsAtUtc).HasColumnName("starts_at_utc"); b.Property(x => x.StopsAtUtc).HasColumnName("stops_at_utc"); b.Property(x => x.MetaCreatedAtUtc).HasColumnName("meta_created_at_utc"); b.Property(x => x.MetaUpdatedAtUtc).HasColumnName("meta_updated_at_utc"); b.Property(x => x.LastSyncedAtUtc).HasColumnName("last_synced_at_utc"); b.Property(x => x.IsPresentOnMeta).HasColumnName("is_present_on_meta");
        b.HasOne<AdAccount>().WithMany().HasForeignKey(x => x.AdAccountId).OnDelete(DeleteBehavior.Cascade); b.HasIndex(x => new { x.AdAccountId, x.MetaCampaignId }).IsUnique().HasDatabaseName("ux_campaigns_account_meta_id");
    }
    private static void Text(PropertyBuilder<string> property) => property.HasMaxLength(200).IsRequired();
}

internal sealed class AdSetConfiguration : IEntityTypeConfiguration<AdSet>
{
    public void Configure(EntityTypeBuilder<AdSet> b)
    {
        b.ToTable("ad_sets"); b.HasKey(x => x.Id); b.Property(x => x.Id).HasColumnName("id"); b.Property(x => x.CampaignId).HasColumnName("campaign_id");
        Text(b.Property(x => x.MetaAdSetId).HasColumnName("meta_ad_set_id")); Text(b.Property(x => x.Name).HasColumnName("name")); Text(b.Property(x => x.OptimizationGoal).HasColumnName("optimization_goal")); Text(b.Property(x => x.BillingEvent).HasColumnName("billing_event")); Text(b.Property(x => x.ConfiguredStatus).HasColumnName("configured_status")); Text(b.Property(x => x.EffectiveStatus).HasColumnName("effective_status"));
        b.Property(x => x.StartsAtUtc).HasColumnName("starts_at_utc"); b.Property(x => x.EndsAtUtc).HasColumnName("ends_at_utc"); b.Property(x => x.MetaCreatedAtUtc).HasColumnName("meta_created_at_utc"); b.Property(x => x.MetaUpdatedAtUtc).HasColumnName("meta_updated_at_utc"); b.Property(x => x.LastSyncedAtUtc).HasColumnName("last_synced_at_utc"); b.Property(x => x.IsPresentOnMeta).HasColumnName("is_present_on_meta");
        b.HasOne<Campaign>().WithMany().HasForeignKey(x => x.CampaignId).OnDelete(DeleteBehavior.Cascade); b.HasIndex(x => new { x.CampaignId, x.MetaAdSetId }).IsUnique().HasDatabaseName("ux_ad_sets_campaign_meta_id");
    }
    private static void Text(PropertyBuilder<string> property) => property.HasMaxLength(200).IsRequired();
}

internal sealed class AdConfiguration : IEntityTypeConfiguration<Ad>
{
    public void Configure(EntityTypeBuilder<Ad> b)
    {
        b.ToTable("ads"); b.HasKey(x => x.Id); b.Property(x => x.Id).HasColumnName("id"); b.Property(x => x.AdSetId).HasColumnName("ad_set_id");
        Text(b.Property(x => x.MetaAdId).HasColumnName("meta_ad_id")); Text(b.Property(x => x.Name).HasColumnName("name")); Text(b.Property(x => x.ConfiguredStatus).HasColumnName("configured_status")); Text(b.Property(x => x.EffectiveStatus).HasColumnName("effective_status"));
        b.Property(x => x.MetaCreatedAtUtc).HasColumnName("meta_created_at_utc"); b.Property(x => x.MetaUpdatedAtUtc).HasColumnName("meta_updated_at_utc"); b.Property(x => x.LastSyncedAtUtc).HasColumnName("last_synced_at_utc"); b.Property(x => x.IsPresentOnMeta).HasColumnName("is_present_on_meta");
        b.HasOne<AdSet>().WithMany().HasForeignKey(x => x.AdSetId).OnDelete(DeleteBehavior.Cascade); b.HasIndex(x => new { x.AdSetId, x.MetaAdId }).IsUnique().HasDatabaseName("ux_ads_ad_set_meta_id");
    }
    private static void Text(PropertyBuilder<string> property) => property.HasMaxLength(200).IsRequired();
}

internal sealed class AdAccountSyncConfiguration : IEntityTypeConfiguration<AdAccountSync>
{
    public void Configure(EntityTypeBuilder<AdAccountSync> b)
    {
        b.ToTable("ad_account_syncs"); b.HasKey(x => x.AdAccountId); b.Property(x => x.AdAccountId).HasColumnName("ad_account_id"); b.Property(x => x.Status).HasColumnName("status").HasConversion<string>().HasMaxLength(30); b.Property(x => x.StartedAtUtc).HasColumnName("started_at_utc"); b.Property(x => x.CompletedAtUtc).HasColumnName("completed_at_utc"); b.Property(x => x.CampaignsSynced).HasColumnName("campaigns_synced"); b.Property(x => x.AdSetsSynced).HasColumnName("ad_sets_synced"); b.Property(x => x.AdsSynced).HasColumnName("ads_synced"); b.Property(x => x.ErrorCode).HasColumnName("error_code").HasMaxLength(100); b.HasOne<AdAccount>().WithOne().HasForeignKey<AdAccountSync>(x => x.AdAccountId).OnDelete(DeleteBehavior.Cascade);
    }
}
