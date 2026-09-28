using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AnaliticAsd.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddAdvertisingHierarchy : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ad_account_syncs",
                columns: table => new
                {
                    ad_account_id = table.Column<Guid>(type: "uuid", nullable: false),
                    status = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    started_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    completed_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    campaigns_synced = table.Column<int>(type: "integer", nullable: false),
                    ad_sets_synced = table.Column<int>(type: "integer", nullable: false),
                    ads_synced = table.Column<int>(type: "integer", nullable: false),
                    error_code = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ad_account_syncs", x => x.ad_account_id);
                    table.ForeignKey(
                        name: "FK_ad_account_syncs_ad_accounts_ad_account_id",
                        column: x => x.ad_account_id,
                        principalTable: "ad_accounts",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "campaigns",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    ad_account_id = table.Column<Guid>(type: "uuid", nullable: false),
                    meta_campaign_id = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    objective = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    configured_status = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    effective_status = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    starts_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    stops_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    meta_created_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    meta_updated_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    last_synced_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    is_present_on_meta = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_campaigns", x => x.id);
                    table.ForeignKey(
                        name: "FK_campaigns_ad_accounts_ad_account_id",
                        column: x => x.ad_account_id,
                        principalTable: "ad_accounts",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ad_sets",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    campaign_id = table.Column<Guid>(type: "uuid", nullable: false),
                    meta_ad_set_id = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    optimization_goal = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    billing_event = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    configured_status = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    effective_status = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    starts_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    ends_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    meta_created_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    meta_updated_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    last_synced_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    is_present_on_meta = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ad_sets", x => x.id);
                    table.ForeignKey(
                        name: "FK_ad_sets_campaigns_campaign_id",
                        column: x => x.campaign_id,
                        principalTable: "campaigns",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ads",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    ad_set_id = table.Column<Guid>(type: "uuid", nullable: false),
                    meta_ad_id = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    configured_status = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    effective_status = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    meta_created_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    meta_updated_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    last_synced_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    is_present_on_meta = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ads", x => x.id);
                    table.ForeignKey(
                        name: "FK_ads_ad_sets_ad_set_id",
                        column: x => x.ad_set_id,
                        principalTable: "ad_sets",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ux_ad_sets_campaign_meta_id",
                table: "ad_sets",
                columns: new[] { "campaign_id", "meta_ad_set_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ux_ads_ad_set_meta_id",
                table: "ads",
                columns: new[] { "ad_set_id", "meta_ad_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ux_campaigns_account_meta_id",
                table: "campaigns",
                columns: new[] { "ad_account_id", "meta_campaign_id" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ad_account_syncs");

            migrationBuilder.DropTable(
                name: "ads");

            migrationBuilder.DropTable(
                name: "ad_sets");

            migrationBuilder.DropTable(
                name: "campaigns");
        }
    }
}
