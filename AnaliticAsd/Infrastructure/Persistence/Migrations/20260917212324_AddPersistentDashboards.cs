using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AnaliticAsd.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddPersistentDashboards : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "dashboard_templates",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    agency_id = table.Column<Guid>(type: "uuid", nullable: false),
                    client_id = table.Column<Guid>(type: "uuid", nullable: true),
                    name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    description = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    schema_version = table.Column<int>(type: "integer", nullable: false),
                    definition_json = table.Column<string>(type: "jsonb", nullable: false),
                    created_by_user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    created_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    version = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_dashboard_templates", x => x.id);
                    table.ForeignKey(
                        name: "FK_dashboard_templates_agencies_agency_id",
                        column: x => x.agency_id,
                        principalTable: "agencies",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_dashboard_templates_clients_client_id_agency_id",
                        columns: x => new { x.client_id, x.agency_id },
                        principalTable: "clients",
                        principalColumns: new[] { "id", "agency_id" },
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_dashboard_templates_users_created_by_user_id",
                        column: x => x.created_by_user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "dashboards",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    agency_id = table.Column<Guid>(type: "uuid", nullable: false),
                    client_id = table.Column<Guid>(type: "uuid", nullable: false),
                    folder_id = table.Column<Guid>(type: "uuid", nullable: true),
                    title = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    description = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                    created_by_user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    current_publication_number = table.Column<int>(type: "integer", nullable: true),
                    archived_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    created_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    version = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_dashboards", x => x.id);
                    table.UniqueConstraint("AK_dashboards_id_client_id_agency_id", x => new { x.id, x.client_id, x.agency_id });
                    table.ForeignKey(
                        name: "FK_dashboards_clients_client_id_agency_id",
                        columns: x => new { x.client_id, x.agency_id },
                        principalTable: "clients",
                        principalColumns: new[] { "id", "agency_id" },
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_dashboards_folders_folder_id_client_id_agency_id",
                        columns: x => new { x.folder_id, x.client_id, x.agency_id },
                        principalTable: "folders",
                        principalColumns: new[] { "id", "client_id", "agency_id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_dashboards_users_created_by_user_id",
                        column: x => x.created_by_user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "dashboard_drafts",
                columns: table => new
                {
                    dashboard_id = table.Column<Guid>(type: "uuid", nullable: false),
                    agency_id = table.Column<Guid>(type: "uuid", nullable: false),
                    client_id = table.Column<Guid>(type: "uuid", nullable: false),
                    schema_version = table.Column<int>(type: "integer", nullable: false),
                    revision = table.Column<int>(type: "integer", nullable: false),
                    definition_json = table.Column<string>(type: "jsonb", nullable: false),
                    updated_by_user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    updated_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    version = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_dashboard_drafts", x => x.dashboard_id);
                    table.ForeignKey(
                        name: "FK_dashboard_drafts_dashboards_dashboard_id_client_id_agency_id",
                        columns: x => new { x.dashboard_id, x.client_id, x.agency_id },
                        principalTable: "dashboards",
                        principalColumns: new[] { "id", "client_id", "agency_id" },
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_dashboard_drafts_users_updated_by_user_id",
                        column: x => x.updated_by_user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "dashboard_versions",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    dashboard_id = table.Column<Guid>(type: "uuid", nullable: false),
                    agency_id = table.Column<Guid>(type: "uuid", nullable: false),
                    client_id = table.Column<Guid>(type: "uuid", nullable: false),
                    publication_number = table.Column<int>(type: "integer", nullable: false),
                    draft_revision = table.Column<int>(type: "integer", nullable: false),
                    schema_version = table.Column<int>(type: "integer", nullable: false),
                    definition_json = table.Column<string>(type: "jsonb", nullable: false),
                    definition_hash = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    published_by_user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    published_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_dashboard_versions", x => x.id);
                    table.ForeignKey(
                        name: "FK_dashboard_versions_dashboards_dashboard_id_client_id_agency~",
                        columns: x => new { x.dashboard_id, x.client_id, x.agency_id },
                        principalTable: "dashboards",
                        principalColumns: new[] { "id", "client_id", "agency_id" },
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_dashboard_versions_users_published_by_user_id",
                        column: x => x.published_by_user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_dashboard_drafts_dashboard_id_client_id_agency_id",
                table: "dashboard_drafts",
                columns: new[] { "dashboard_id", "client_id", "agency_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_dashboard_drafts_updated_by_user_id",
                table: "dashboard_drafts",
                column: "updated_by_user_id");

            migrationBuilder.CreateIndex(
                name: "IX_dashboard_templates_client_id_agency_id",
                table: "dashboard_templates",
                columns: new[] { "client_id", "agency_id" });

            migrationBuilder.CreateIndex(
                name: "IX_dashboard_templates_created_by_user_id",
                table: "dashboard_templates",
                column: "created_by_user_id");

            migrationBuilder.CreateIndex(
                name: "ix_dashboard_templates_scope_name",
                table: "dashboard_templates",
                columns: new[] { "agency_id", "client_id", "name" });

            migrationBuilder.CreateIndex(
                name: "IX_dashboard_versions_dashboard_id_client_id_agency_id",
                table: "dashboard_versions",
                columns: new[] { "dashboard_id", "client_id", "agency_id" });

            migrationBuilder.CreateIndex(
                name: "IX_dashboard_versions_published_by_user_id",
                table: "dashboard_versions",
                column: "published_by_user_id");

            migrationBuilder.CreateIndex(
                name: "ux_dashboard_versions_publication",
                table: "dashboard_versions",
                columns: new[] { "dashboard_id", "publication_number" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_dashboards_client_id_agency_id",
                table: "dashboards",
                columns: new[] { "client_id", "agency_id" });

            migrationBuilder.CreateIndex(
                name: "IX_dashboards_created_by_user_id",
                table: "dashboards",
                column: "created_by_user_id");

            migrationBuilder.CreateIndex(
                name: "IX_dashboards_folder_id_client_id_agency_id",
                table: "dashboards",
                columns: new[] { "folder_id", "client_id", "agency_id" });

            migrationBuilder.CreateIndex(
                name: "ix_dashboards_workspace_folder_archive",
                table: "dashboards",
                columns: new[] { "agency_id", "client_id", "folder_id", "archived_at_utc" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "dashboard_drafts");

            migrationBuilder.DropTable(
                name: "dashboard_templates");

            migrationBuilder.DropTable(
                name: "dashboard_versions");

            migrationBuilder.DropTable(
                name: "dashboards");
        }
    }
}
