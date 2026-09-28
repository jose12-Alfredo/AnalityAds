using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AnaliticAsd.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddReportShareLinks : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "report_share_links",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    report_id = table.Column<Guid>(type: "uuid", nullable: false),
                    agency_id = table.Column<Guid>(type: "uuid", nullable: false),
                    client_id = table.Column<Guid>(type: "uuid", nullable: false),
                    created_by_user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    token_hash = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    created_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    expires_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    revoked_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    version = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_report_share_links", x => x.id);
                    table.ForeignKey(
                        name: "FK_report_share_links_reports_report_id",
                        column: x => x.report_id,
                        principalTable: "reports",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_report_share_links_users_created_by_user_id",
                        column: x => x.created_by_user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "report_share_audit_events",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    share_link_id = table.Column<Guid>(type: "uuid", nullable: false),
                    operation = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    occurred_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_report_share_audit_events", x => x.id);
                    table.ForeignKey(
                        name: "FK_report_share_audit_events_report_share_links_share_link_id",
                        column: x => x.share_link_id,
                        principalTable: "report_share_links",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_report_share_audit_events_share_link_id_occurred_at_utc",
                table: "report_share_audit_events",
                columns: new[] { "share_link_id", "occurred_at_utc" });

            migrationBuilder.CreateIndex(
                name: "IX_report_share_links_agency_id_report_id",
                table: "report_share_links",
                columns: new[] { "agency_id", "report_id" });

            migrationBuilder.CreateIndex(
                name: "IX_report_share_links_created_by_user_id",
                table: "report_share_links",
                column: "created_by_user_id");

            migrationBuilder.CreateIndex(
                name: "IX_report_share_links_report_id",
                table: "report_share_links",
                column: "report_id");

            migrationBuilder.CreateIndex(
                name: "IX_report_share_links_token_hash",
                table: "report_share_links",
                column: "token_hash",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "report_share_audit_events");

            migrationBuilder.DropTable(
                name: "report_share_links");
        }
    }
}
