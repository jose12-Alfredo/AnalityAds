using AnaliticAsd.Domain.Advertising;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AnaliticAsd.Infrastructure.Persistence.Configurations;

internal sealed class InsightSnapshotConfiguration : IEntityTypeConfiguration<InsightSnapshot>
{
    public void Configure(EntityTypeBuilder<InsightSnapshot> b)
    {
        b.ToTable("insight_snapshots"); b.HasKey(x => x.Id); b.Property(x => x.Id).HasColumnName("id"); b.Property(x => x.AdAccountId).HasColumnName("ad_account_id"); b.Property(x => x.CampaignId).HasColumnName("campaign_id"); b.Property(x => x.AdSetId).HasColumnName("ad_set_id"); b.Property(x => x.AdId).HasColumnName("ad_id"); b.Property(x => x.Level).HasColumnName("level").HasConversion<string>().HasMaxLength(20); b.Property(x => x.SnapshotDate).HasColumnName("snapshot_date"); b.Property(x => x.Currency).HasColumnName("currency").HasMaxLength(3);
        Money(b.Property(x => x.Spend).HasColumnName("spend")); b.Property(x => x.Impressions).HasColumnName("impressions"); b.Property(x => x.Reach).HasColumnName("reach"); b.Property(x => x.LinkClicks).HasColumnName("link_clicks"); Metric(b.Property(x => x.Leads).HasColumnName("leads")); Metric(b.Property(x => x.Purchases).HasColumnName("purchases")); Money(b.Property(x => x.PurchaseValue).HasColumnName("purchase_value"));
        Metric(b.Property(x => x.Frequency).HasColumnName("frequency")); Metric(b.Property(x => x.Cpm).HasColumnName("cpm")); Metric(b.Property(x => x.Ctr).HasColumnName("ctr")); Metric(b.Property(x => x.Cpc).HasColumnName("cpc")); Metric(b.Property(x => x.Cpl).HasColumnName("cpl")); Metric(b.Property(x => x.Cpa).HasColumnName("cpa")); Metric(b.Property(x => x.Roas).HasColumnName("roas")); b.Property(x => x.ObservedDataQuality).HasColumnName("observed_data_quality").HasConversion<string>().HasMaxLength(32).HasDefaultValue(InsightSnapshotDataQuality.LegacyZeroNormalized); b.Property(x => x.ObservedAtUtc).HasColumnName("observed_at_utc");
        b.HasOne<AdAccount>().WithMany().HasForeignKey(x => x.AdAccountId).OnDelete(DeleteBehavior.Cascade); b.HasOne<Campaign>().WithMany().HasForeignKey(x => x.CampaignId).OnDelete(DeleteBehavior.Cascade); b.HasOne<AdSet>().WithMany().HasForeignKey(x => x.AdSetId).OnDelete(DeleteBehavior.Cascade); b.HasOne<Ad>().WithMany().HasForeignKey(x => x.AdId).OnDelete(DeleteBehavior.Cascade);
        b.HasIndex(x => new { x.AdAccountId, x.SnapshotDate }).IsUnique().HasFilter("level = 'Account'").HasDatabaseName("ux_insight_snapshots_account_date");
        b.HasIndex(x => new { x.CampaignId, x.SnapshotDate }).IsUnique().HasFilter("level = 'Campaign'").HasDatabaseName("ux_insight_snapshots_campaign_date");
        b.HasIndex(x => new { x.AdSetId, x.SnapshotDate }).IsUnique().HasFilter("level = 'AdSet'").HasDatabaseName("ux_insight_snapshots_adset_date");
        b.HasIndex(x => new { x.AdId, x.SnapshotDate }).IsUnique().HasFilter("level = 'Ad'").HasDatabaseName("ux_insight_snapshots_ad_date");
    }
    private static void Money(PropertyBuilder<decimal?> p) => p.HasPrecision(20, 6); private static void Metric(PropertyBuilder<decimal?> p) => p.HasPrecision(28, 10);
}
