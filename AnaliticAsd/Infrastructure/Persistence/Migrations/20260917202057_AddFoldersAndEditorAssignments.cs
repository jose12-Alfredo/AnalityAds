using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AnaliticAsd.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddFoldersAndEditorAssignments : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "client_editor_assignments",
                columns: table => new
                {
                    agency_id = table.Column<Guid>(type: "uuid", nullable: false),
                    client_id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    granted_by_user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    granted_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    version = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_client_editor_assignments", x => new { x.agency_id, x.client_id, x.user_id });
                    table.ForeignKey(
                        name: "FK_client_editor_assignments_clients_client_id_agency_id",
                        columns: x => new { x.client_id, x.agency_id },
                        principalTable: "clients",
                        principalColumns: new[] { "id", "agency_id" },
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_client_editor_assignments_memberships_agency_id_user_id",
                        columns: x => new { x.agency_id, x.user_id },
                        principalTable: "memberships",
                        principalColumns: new[] { "agency_id", "user_id" },
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_client_editor_assignments_users_granted_by_user_id",
                        column: x => x.granted_by_user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "folders",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    agency_id = table.Column<Guid>(type: "uuid", nullable: false),
                    client_id = table.Column<Guid>(type: "uuid", nullable: false),
                    parent_folder_id = table.Column<Guid>(type: "uuid", nullable: true),
                    name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    sort_order = table.Column<int>(type: "integer", nullable: false),
                    archived_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    created_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    version = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_folders", x => x.id);
                    table.UniqueConstraint("AK_folders_id_client_id_agency_id", x => new { x.id, x.client_id, x.agency_id });
                    table.ForeignKey(
                        name: "FK_folders_clients_client_id_agency_id",
                        columns: x => new { x.client_id, x.agency_id },
                        principalTable: "clients",
                        principalColumns: new[] { "id", "agency_id" },
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_folders_folders_parent_folder_id_client_id_agency_id",
                        columns: x => new { x.parent_folder_id, x.client_id, x.agency_id },
                        principalTable: "folders",
                        principalColumns: new[] { "id", "client_id", "agency_id" },
                        onDelete: ReferentialAction.Restrict);
                });

            // Before this migration, Analysts could read every client in their agency. Persist those effective
            // grants so deploying the new explicit-assignment rule does not silently remove existing access.
            migrationBuilder.Sql(
                """
                INSERT INTO client_editor_assignments
                    (agency_id, client_id, user_id, granted_by_user_id, granted_at_utc, version)
                SELECT
                    membership.agency_id,
                    client.id,
                    membership.user_id,
                    COALESCE((
                        SELECT grantor.user_id
                        FROM memberships AS grantor
                        WHERE grantor.agency_id = membership.agency_id
                          AND grantor.role IN ('Owner', 'Admin')
                        ORDER BY CASE grantor.role WHEN 'Owner' THEN 0 ELSE 1 END,
                                 grantor.created_at_utc,
                                 grantor.user_id
                        LIMIT 1
                    ), membership.user_id),
                    GREATEST(membership.created_at_utc, client.created_at_utc),
                    client.id
                FROM memberships AS membership
                INNER JOIN clients AS client ON client.agency_id = membership.agency_id
                WHERE membership.role = 'Analyst';
                """);

            migrationBuilder.CreateIndex(
                name: "ix_client_editor_assignments_agency_user",
                table: "client_editor_assignments",
                columns: new[] { "agency_id", "user_id" });

            migrationBuilder.CreateIndex(
                name: "IX_client_editor_assignments_client_id_agency_id",
                table: "client_editor_assignments",
                columns: new[] { "client_id", "agency_id" });

            migrationBuilder.CreateIndex(
                name: "IX_client_editor_assignments_granted_by_user_id",
                table: "client_editor_assignments",
                column: "granted_by_user_id");

            migrationBuilder.CreateIndex(
                name: "IX_folders_client_id_agency_id",
                table: "folders",
                columns: new[] { "client_id", "agency_id" });

            migrationBuilder.CreateIndex(
                name: "IX_folders_parent_folder_id_client_id_agency_id",
                table: "folders",
                columns: new[] { "parent_folder_id", "client_id", "agency_id" });

            migrationBuilder.CreateIndex(
                name: "ix_folders_workspace_archive",
                table: "folders",
                columns: new[] { "agency_id", "client_id", "archived_at_utc" });

            migrationBuilder.CreateIndex(
                name: "ix_folders_workspace_parent_sort",
                table: "folders",
                columns: new[] { "agency_id", "client_id", "parent_folder_id", "sort_order" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "client_editor_assignments");

            migrationBuilder.DropTable(
                name: "folders");
        }
    }
}
