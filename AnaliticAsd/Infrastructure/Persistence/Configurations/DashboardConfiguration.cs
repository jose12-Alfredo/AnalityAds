using AnaliticAsd.Domain.Clients;
using AnaliticAsd.Domain.Dashboards;
using AnaliticAsd.Domain.Folders;
using AnaliticAsd.Domain.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AnaliticAsd.Infrastructure.Persistence.Configurations;

internal sealed class DashboardConfiguration : IEntityTypeConfiguration<Dashboard>
{
    public void Configure(EntityTypeBuilder<Dashboard> builder)
    {
        builder.ToTable("dashboards");
        builder.HasKey(dashboard => dashboard.Id);
        builder.HasAlternateKey(dashboard => new { dashboard.Id, dashboard.ClientId, dashboard.AgencyId });
        builder.Property(dashboard => dashboard.Id).HasColumnName("id");
        builder.Property(dashboard => dashboard.AgencyId).HasColumnName("agency_id");
        builder.Property(dashboard => dashboard.ClientId).HasColumnName("client_id");
        builder.Property(dashboard => dashboard.FolderId).HasColumnName("folder_id");
        builder.Property(dashboard => dashboard.Title).HasColumnName("title").HasMaxLength(Dashboard.MaxTitleLength);
        builder.Property(dashboard => dashboard.Description).HasColumnName("description")
            .HasMaxLength(Dashboard.MaxDescriptionLength);
        builder.Property(dashboard => dashboard.CreatedByUserId).HasColumnName("created_by_user_id");
        builder.Property(dashboard => dashboard.CurrentPublicationNumber).HasColumnName("current_publication_number");
        builder.Property(dashboard => dashboard.ArchivedAtUtc).HasColumnName("archived_at_utc");
        builder.Property(dashboard => dashboard.CreatedAtUtc).HasColumnName("created_at_utc");
        builder.Property(dashboard => dashboard.UpdatedAtUtc).HasColumnName("updated_at_utc");
        builder.Property(dashboard => dashboard.Version).HasColumnName("version").IsConcurrencyToken();
        builder.Ignore(dashboard => dashboard.IsArchived);

        builder.HasOne<Client>().WithMany()
            .HasForeignKey(dashboard => new { dashboard.ClientId, dashboard.AgencyId })
            .HasPrincipalKey(client => new { client.Id, client.AgencyId })
            .OnDelete(DeleteBehavior.Cascade);
        builder.HasOne<Folder>().WithMany()
            .HasForeignKey(dashboard => new { dashboard.FolderId, dashboard.ClientId, dashboard.AgencyId })
            .HasPrincipalKey(folder => new { folder.Id, folder.ClientId, folder.AgencyId })
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<User>().WithMany().HasForeignKey(dashboard => dashboard.CreatedByUserId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(dashboard => new { dashboard.AgencyId, dashboard.ClientId, dashboard.FolderId,
            dashboard.ArchivedAtUtc }).HasDatabaseName("ix_dashboards_workspace_folder_archive");
    }
}

internal sealed class DashboardDraftConfiguration : IEntityTypeConfiguration<DashboardDraft>
{
    public void Configure(EntityTypeBuilder<DashboardDraft> builder)
    {
        builder.ToTable("dashboard_drafts");
        builder.HasKey(draft => draft.DashboardId);
        builder.Property(draft => draft.DashboardId).HasColumnName("dashboard_id");
        builder.Property(draft => draft.AgencyId).HasColumnName("agency_id");
        builder.Property(draft => draft.ClientId).HasColumnName("client_id");
        builder.Property(draft => draft.SchemaVersion).HasColumnName("schema_version");
        builder.Property(draft => draft.Revision).HasColumnName("revision");
        builder.Property(draft => draft.DefinitionJson).HasColumnName("definition_json").HasColumnType("jsonb");
        builder.Property(draft => draft.UpdatedByUserId).HasColumnName("updated_by_user_id");
        builder.Property(draft => draft.UpdatedAtUtc).HasColumnName("updated_at_utc");
        builder.Property(draft => draft.Version).HasColumnName("version").IsConcurrencyToken();
        builder.HasOne<Dashboard>().WithOne()
            .HasForeignKey<DashboardDraft>(draft => new { draft.DashboardId, draft.ClientId, draft.AgencyId })
            .HasPrincipalKey<Dashboard>(dashboard => new { dashboard.Id, dashboard.ClientId, dashboard.AgencyId })
            .OnDelete(DeleteBehavior.Cascade);
        builder.HasOne<User>().WithMany().HasForeignKey(draft => draft.UpdatedByUserId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

internal sealed class DashboardVersionConfiguration : IEntityTypeConfiguration<DashboardVersion>
{
    public void Configure(EntityTypeBuilder<DashboardVersion> builder)
    {
        builder.ToTable("dashboard_versions");
        builder.HasKey(version => version.Id);
        builder.Property(version => version.Id).HasColumnName("id");
        builder.Property(version => version.DashboardId).HasColumnName("dashboard_id");
        builder.Property(version => version.AgencyId).HasColumnName("agency_id");
        builder.Property(version => version.ClientId).HasColumnName("client_id");
        builder.Property(version => version.PublicationNumber).HasColumnName("publication_number");
        builder.Property(version => version.DraftRevision).HasColumnName("draft_revision");
        builder.Property(version => version.SchemaVersion).HasColumnName("schema_version");
        builder.Property(version => version.DefinitionJson).HasColumnName("definition_json").HasColumnType("jsonb");
        builder.Property(version => version.DefinitionHash).HasColumnName("definition_hash").HasMaxLength(64);
        builder.Property(version => version.PublishedByUserId).HasColumnName("published_by_user_id");
        builder.Property(version => version.PublishedAtUtc).HasColumnName("published_at_utc");
        builder.HasOne<Dashboard>().WithMany()
            .HasForeignKey(version => new { version.DashboardId, version.ClientId, version.AgencyId })
            .HasPrincipalKey(dashboard => new { dashboard.Id, dashboard.ClientId, dashboard.AgencyId })
            .OnDelete(DeleteBehavior.Cascade);
        builder.HasOne<User>().WithMany().HasForeignKey(version => version.PublishedByUserId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(version => new { version.DashboardId, version.PublicationNumber }).IsUnique()
            .HasDatabaseName("ux_dashboard_versions_publication");
    }
}

internal sealed class DashboardTemplateConfiguration : IEntityTypeConfiguration<DashboardTemplate>
{
    public void Configure(EntityTypeBuilder<DashboardTemplate> builder)
    {
        builder.ToTable("dashboard_templates");
        builder.HasKey(template => template.Id);
        builder.Property(template => template.Id).HasColumnName("id");
        builder.Property(template => template.AgencyId).HasColumnName("agency_id");
        builder.Property(template => template.ClientId).HasColumnName("client_id");
        builder.Property(template => template.Name).HasColumnName("name").HasMaxLength(DashboardTemplate.MaxNameLength);
        builder.Property(template => template.Description).HasColumnName("description")
            .HasMaxLength(DashboardTemplate.MaxDescriptionLength);
        builder.Property(template => template.SchemaVersion).HasColumnName("schema_version");
        builder.Property(template => template.DefinitionJson).HasColumnName("definition_json").HasColumnType("jsonb");
        builder.Property(template => template.CreatedByUserId).HasColumnName("created_by_user_id");
        builder.Property(template => template.CreatedAtUtc).HasColumnName("created_at_utc");
        builder.Property(template => template.Version).HasColumnName("version").IsConcurrencyToken();
        builder.HasOne<Client>().WithMany()
            .HasForeignKey(template => new { template.ClientId, template.AgencyId })
            .HasPrincipalKey(client => new { client.Id, client.AgencyId })
            .OnDelete(DeleteBehavior.Cascade);
        builder.HasOne<Agency>().WithMany().HasForeignKey(template => template.AgencyId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<User>().WithMany().HasForeignKey(template => template.CreatedByUserId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(template => new { template.AgencyId, template.ClientId, template.Name })
            .HasDatabaseName("ix_dashboard_templates_scope_name");
    }
}
