using AnaliticAsd.Domain.Clients;
using AnaliticAsd.Domain.Folders;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AnaliticAsd.Infrastructure.Persistence.Configurations;

internal sealed class FolderConfiguration : IEntityTypeConfiguration<Folder>
{
    public void Configure(EntityTypeBuilder<Folder> builder)
    {
        builder.ToTable("folders");
        builder.HasKey(folder => folder.Id);
        builder.HasAlternateKey(folder => new { folder.Id, folder.ClientId, folder.AgencyId });
        builder.Property(folder => folder.Id).HasColumnName("id");
        builder.Property(folder => folder.AgencyId).HasColumnName("agency_id");
        builder.Property(folder => folder.ClientId).HasColumnName("client_id");
        builder.Property(folder => folder.ParentFolderId).HasColumnName("parent_folder_id");
        builder.Property(folder => folder.Name).HasColumnName("name").HasMaxLength(Folder.MaxNameLength);
        builder.Property(folder => folder.SortOrder).HasColumnName("sort_order");
        builder.Property(folder => folder.ArchivedAtUtc).HasColumnName("archived_at_utc");
        builder.Property(folder => folder.CreatedAtUtc).HasColumnName("created_at_utc");
        builder.Property(folder => folder.UpdatedAtUtc).HasColumnName("updated_at_utc");
        builder.Property(folder => folder.Version).HasColumnName("version").IsConcurrencyToken();
        builder.Ignore(folder => folder.IsArchived);

        builder.HasOne<Client>().WithMany()
            .HasForeignKey(folder => new { folder.ClientId, folder.AgencyId })
            .HasPrincipalKey(client => new { client.Id, client.AgencyId })
            .OnDelete(DeleteBehavior.Cascade);
        builder.HasOne<Folder>().WithMany()
            .HasForeignKey(folder => new { folder.ParentFolderId, folder.ClientId, folder.AgencyId })
            .HasPrincipalKey(folder => new { folder.Id, folder.ClientId, folder.AgencyId })
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(folder => new { folder.AgencyId, folder.ClientId, folder.ParentFolderId, folder.SortOrder })
            .HasDatabaseName("ix_folders_workspace_parent_sort");
        builder.HasIndex(folder => new { folder.AgencyId, folder.ClientId, folder.ArchivedAtUtc })
            .HasDatabaseName("ix_folders_workspace_archive");
    }
}
