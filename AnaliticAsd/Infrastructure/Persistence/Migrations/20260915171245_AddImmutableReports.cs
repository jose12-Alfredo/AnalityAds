using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AnaliticAsd.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddImmutableReports : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "reports",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    agency_id = table.Column<Guid>(type: "uuid", nullable: false),
                    client_id = table.Column<Guid>(type: "uuid", nullable: false),
                    ad_account_id = table.Column<Guid>(type: "uuid", nullable: false),
                    created_by_user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    title = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    since = table.Column<DateOnly>(type: "date", nullable: false),
                    until = table.Column<DateOnly>(type: "date", nullable: false),
                    created_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    schema_version = table.Column<int>(type: "integer", nullable: false),
                    snapshot_json = table.Column<string>(type: "jsonb", nullable: false),
                    snapshot_hash = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_reports", x => x.id);
                    table.ForeignKey(
                        name: "FK_reports_ad_accounts_ad_account_id",
                        column: x => x.ad_account_id,
                        principalTable: "ad_accounts",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_reports_clients_client_id_agency_id",
                        columns: x => new { x.client_id, x.agency_id },
                        principalTable: "clients",
                        principalColumns: new[] { "id", "agency_id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_reports_users_created_by_user_id",
                        column: x => x.created_by_user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_reports_ad_account_id",
                table: "reports",
                column: "ad_account_id");

            migrationBuilder.CreateIndex(
                name: "IX_reports_agency_id_client_id_created_at_utc",
                table: "reports",
                columns: new[] { "agency_id", "client_id", "created_at_utc" });

            migrationBuilder.CreateIndex(
                name: "IX_reports_client_id_agency_id",
                table: "reports",
                columns: new[] { "client_id", "agency_id" });

            migrationBuilder.CreateIndex(
                name: "IX_reports_created_by_user_id",
                table: "reports",
                column: "created_by_user_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "reports");
        }
    }
}
