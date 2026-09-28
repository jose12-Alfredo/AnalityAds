using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AnaliticAsd.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddDashboardExportsAndDeliveries : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "dashboard_delivery_schedules",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    dashboard_id = table.Column<Guid>(type: "uuid", nullable: false),
                    agency_id = table.Column<Guid>(type: "uuid", nullable: false),
                    client_id = table.Column<Guid>(type: "uuid", nullable: false),
                    created_by_user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    recipients_json = table.Column<string>(type: "jsonb", nullable: false),
                    format = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    frequency = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    hour_utc = table.Column<int>(type: "integer", nullable: false),
                    day_of_week = table.Column<int>(type: "integer", nullable: true),
                    day_of_month = table.Column<int>(type: "integer", nullable: true),
                    is_enabled = table.Column<bool>(type: "boolean", nullable: false),
                    next_run_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    last_run_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    last_succeeded_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    last_error_code = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: true),
                    version = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_dashboard_delivery_schedules", x => x.id);
                    table.ForeignKey(
                        name: "FK_dashboard_delivery_schedules_dashboards_dashboard_id",
                        column: x => x.dashboard_id,
                        principalTable: "dashboards",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_dashboard_delivery_schedules_users_created_by_user_id",
                        column: x => x.created_by_user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "dashboard_exports",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    dashboard_id = table.Column<Guid>(type: "uuid", nullable: false),
                    agency_id = table.Column<Guid>(type: "uuid", nullable: false),
                    client_id = table.Column<Guid>(type: "uuid", nullable: false),
                    requested_by_user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    share_link_id = table.Column<Guid>(type: "uuid", nullable: true),
                    delivery_schedule_id = table.Column<Guid>(type: "uuid", nullable: true),
                    publication_number = table.Column<int>(type: "integer", nullable: false),
                    format = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    status = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    file_name = table.Column<string>(type: "character varying(240)", maxLength: 240, nullable: true),
                    content_type = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: true),
                    content = table.Column<byte[]>(type: "bytea", nullable: true),
                    error_code = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: true),
                    created_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    started_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    completed_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_dashboard_exports", x => x.id);
                    table.ForeignKey(
                        name: "FK_dashboard_exports_dashboard_delivery_schedules_delivery_sch~",
                        column: x => x.delivery_schedule_id,
                        principalTable: "dashboard_delivery_schedules",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_dashboard_exports_dashboard_share_links_share_link_id",
                        column: x => x.share_link_id,
                        principalTable: "dashboard_share_links",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_dashboard_exports_dashboards_dashboard_id",
                        column: x => x.dashboard_id,
                        principalTable: "dashboards",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_dashboard_exports_users_requested_by_user_id",
                        column: x => x.requested_by_user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_dashboard_delivery_schedules_agency_id_dashboard_id",
                table: "dashboard_delivery_schedules",
                columns: new[] { "agency_id", "dashboard_id" });

            migrationBuilder.CreateIndex(
                name: "IX_dashboard_delivery_schedules_created_by_user_id",
                table: "dashboard_delivery_schedules",
                column: "created_by_user_id");

            migrationBuilder.CreateIndex(
                name: "IX_dashboard_delivery_schedules_dashboard_id",
                table: "dashboard_delivery_schedules",
                column: "dashboard_id");

            migrationBuilder.CreateIndex(
                name: "IX_dashboard_delivery_schedules_is_enabled_next_run_at_utc",
                table: "dashboard_delivery_schedules",
                columns: new[] { "is_enabled", "next_run_at_utc" });

            migrationBuilder.CreateIndex(
                name: "IX_dashboard_exports_agency_id_dashboard_id_created_at_utc",
                table: "dashboard_exports",
                columns: new[] { "agency_id", "dashboard_id", "created_at_utc" });

            migrationBuilder.CreateIndex(
                name: "IX_dashboard_exports_dashboard_id",
                table: "dashboard_exports",
                column: "dashboard_id");

            migrationBuilder.CreateIndex(
                name: "IX_dashboard_exports_delivery_schedule_id",
                table: "dashboard_exports",
                column: "delivery_schedule_id");

            migrationBuilder.CreateIndex(
                name: "IX_dashboard_exports_requested_by_user_id",
                table: "dashboard_exports",
                column: "requested_by_user_id");

            migrationBuilder.CreateIndex(
                name: "IX_dashboard_exports_share_link_id",
                table: "dashboard_exports",
                column: "share_link_id");

            migrationBuilder.CreateIndex(
                name: "IX_dashboard_exports_status_created_at_utc",
                table: "dashboard_exports",
                columns: new[] { "status", "created_at_utc" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "dashboard_exports");

            migrationBuilder.DropTable(
                name: "dashboard_delivery_schedules");
        }
    }
}
