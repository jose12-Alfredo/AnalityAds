using AnaliticAsd.Domain.Clients;
using AnaliticAsd.Domain.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AnaliticAsd.Infrastructure.Persistence.Configurations;

internal sealed class ClientEditorAssignmentConfiguration : IEntityTypeConfiguration<ClientEditorAssignment>
{
    public void Configure(EntityTypeBuilder<ClientEditorAssignment> builder)
    {
        builder.ToTable("client_editor_assignments");
        builder.HasKey(assignment => new { assignment.AgencyId, assignment.ClientId, assignment.UserId });
        builder.Property(assignment => assignment.AgencyId).HasColumnName("agency_id");
        builder.Property(assignment => assignment.ClientId).HasColumnName("client_id");
        builder.Property(assignment => assignment.UserId).HasColumnName("user_id");
        builder.Property(assignment => assignment.GrantedByUserId).HasColumnName("granted_by_user_id");
        builder.Property(assignment => assignment.GrantedAtUtc).HasColumnName("granted_at_utc");
        builder.Property(assignment => assignment.Version).HasColumnName("version").IsConcurrencyToken();

        builder.HasOne<Client>().WithMany()
            .HasForeignKey(assignment => new { assignment.ClientId, assignment.AgencyId })
            .HasPrincipalKey(client => new { client.Id, client.AgencyId })
            .OnDelete(DeleteBehavior.Cascade);
        builder.HasOne<Membership>().WithMany()
            .HasForeignKey(assignment => new { assignment.AgencyId, assignment.UserId })
            .OnDelete(DeleteBehavior.Cascade);
        builder.HasOne<User>().WithMany()
            .HasForeignKey(assignment => assignment.GrantedByUserId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(assignment => new { assignment.AgencyId, assignment.UserId })
            .HasDatabaseName("ix_client_editor_assignments_agency_user");
    }
}
