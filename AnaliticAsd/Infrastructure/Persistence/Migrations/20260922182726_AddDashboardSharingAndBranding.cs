using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AnaliticAsd.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddDashboardSharingAndBranding : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "brand_profiles",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    agency_id = table.Column<Guid>(type: "uuid", nullable: false),
                    client_id = table.Column<Guid>(type: "uuid", nullable: true),
                    logo_url = table.Column<string>(type: "character varying(2048)", maxLength: 2048, nullable: true),
                    primary_color = table.Column<string>(type: "character varying(7)", maxLength: 7, nullable: false),
                    secondary_color = table.Column<string>(type: "character varying(7)", maxLength: 7, nullable: false),
                    background_color = table.Column<string>(type: "character varying(7)", maxLength: 7, nullable: false),
                    font_family = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    updated_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    version = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_brand_profiles", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "dashboard_share_links",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    dashboard_id = table.Column<Guid>(type: "uuid", nullable: false),
                    agency_id = table.Column<Guid>(type: "uuid", nullable: false),
                    client_id = table.Column<Guid>(type: "uuid", nullable: false),
                    created_by_user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    token_hash = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    password_hash = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: true),
                    recipient_email = table.Column<string>(type: "character varying(320)", maxLength: 320, nullable: true),
                    expires_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    allow_filters = table.Column<bool>(type: "boolean", nullable: false),
                    allow_export = table.Column<bool>(type: "boolean", nullable: false),
                    allow_embed = table.Column<bool>(type: "boolean", nullable: false),
                    created_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    revoked_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    last_accessed_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    access_count = table.Column<long>(type: "bigint", nullable: false),
                    version = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_dashboard_share_links", x => x.id);
                    table.ForeignKey(
                        name: "FK_dashboard_share_links_dashboards_dashboard_id",
                        column: x => x.dashboard_id,
                        principalTable: "dashboards",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_dashboard_share_links_users_created_by_user_id",
                        column: x => x.created_by_user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_brand_profiles_agency_id_client_id",
                table: "brand_profiles",
                columns: new[] { "agency_id", "client_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_dashboard_share_links_agency_id_dashboard_id",
                table: "dashboard_share_links",
                columns: new[] { "agency_id", "dashboard_id" });

            migrationBuilder.CreateIndex(
                name: "IX_dashboard_share_links_created_by_user_id",
                table: "dashboard_share_links",
                column: "created_by_user_id");

            migrationBuilder.CreateIndex(
                name: "IX_dashboard_share_links_dashboard_id",
                table: "dashboard_share_links",
                column: "dashboard_id");

            migrationBuilder.CreateIndex(
                name: "IX_dashboard_share_links_token_hash",
                table: "dashboard_share_links",
                column: "token_hash",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "brand_profiles");

            migrationBuilder.DropTable(
                name: "dashboard_share_links");
        }
    }
}
