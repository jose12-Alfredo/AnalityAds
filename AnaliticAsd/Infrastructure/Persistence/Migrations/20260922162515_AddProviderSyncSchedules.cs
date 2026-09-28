using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AnaliticAsd.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddProviderSyncSchedules : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "provider_sync_schedules",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    agency_id = table.Column<Guid>(type: "uuid", nullable: false),
                    data_source_id = table.Column<Guid>(type: "uuid", nullable: false),
                    is_enabled = table.Column<bool>(type: "boolean", nullable: false),
                    interval_minutes = table.Column<int>(type: "integer", nullable: false),
                    lookback_days = table.Column<int>(type: "integer", nullable: false),
                    next_run_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    locked_until_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    consecutive_failures = table.Column<int>(type: "integer", nullable: false),
                    last_started_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    last_succeeded_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    last_error_code = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_provider_sync_schedules", x => x.id);
                    table.ForeignKey(
                        name: "FK_provider_sync_schedules_data_sources_data_source_id",
                        column: x => x.data_source_id,
                        principalTable: "data_sources",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_provider_sync_schedules_data_source_id",
                table: "provider_sync_schedules",
                column: "data_source_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_provider_sync_schedules_is_enabled_next_run_at_utc",
                table: "provider_sync_schedules",
                columns: new[] { "is_enabled", "next_run_at_utc" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "provider_sync_schedules");
        }
    }
}
