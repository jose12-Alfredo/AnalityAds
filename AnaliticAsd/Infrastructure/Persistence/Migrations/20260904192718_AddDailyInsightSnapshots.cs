using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AnaliticAsd.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddDailyInsightSnapshots : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "insight_snapshots",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    ad_account_id = table.Column<Guid>(type: "uuid", nullable: false),
                    campaign_id = table.Column<Guid>(type: "uuid", nullable: true),
                    ad_set_id = table.Column<Guid>(type: "uuid", nullable: true),
                    ad_id = table.Column<Guid>(type: "uuid", nullable: true),
                    level = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    snapshot_date = table.Column<DateOnly>(type: "date", nullable: false),
                    currency = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: false),
                    spend = table.Column<decimal>(type: "numeric(20,6)", precision: 20, scale: 6, nullable: false),
                    impressions = table.Column<long>(type: "bigint", nullable: false),
                    reach = table.Column<long>(type: "bigint", nullable: false),
                    link_clicks = table.Column<long>(type: "bigint", nullable: false),
                    leads = table.Column<decimal>(type: "numeric(28,10)", precision: 28, scale: 10, nullable: false),
                    purchases = table.Column<decimal>(type: "numeric(28,10)", precision: 28, scale: 10, nullable: false),
                    purchase_value = table.Column<decimal>(type: "numeric(20,6)", precision: 20, scale: 6, nullable: false),
                    frequency = table.Column<decimal>(type: "numeric(28,10)", precision: 28, scale: 10, nullable: true),
                    cpm = table.Column<decimal>(type: "numeric(28,10)", precision: 28, scale: 10, nullable: true),
                    ctr = table.Column<decimal>(type: "numeric(28,10)", precision: 28, scale: 10, nullable: true),
                    cpc = table.Column<decimal>(type: "numeric(28,10)", precision: 28, scale: 10, nullable: true),
                    cpl = table.Column<decimal>(type: "numeric(28,10)", precision: 28, scale: 10, nullable: true),
                    cpa = table.Column<decimal>(type: "numeric(28,10)", precision: 28, scale: 10, nullable: true),
                    roas = table.Column<decimal>(type: "numeric(28,10)", precision: 28, scale: 10, nullable: true),
                    observed_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_insight_snapshots", x => x.id);
                    table.ForeignKey(
                        name: "FK_insight_snapshots_ad_accounts_ad_account_id",
                        column: x => x.ad_account_id,
                        principalTable: "ad_accounts",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_insight_snapshots_ad_sets_ad_set_id",
                        column: x => x.ad_set_id,
                        principalTable: "ad_sets",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_insight_snapshots_ads_ad_id",
                        column: x => x.ad_id,
                        principalTable: "ads",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_insight_snapshots_campaigns_campaign_id",
                        column: x => x.campaign_id,
                        principalTable: "campaigns",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ux_insight_snapshots_account_date",
                table: "insight_snapshots",
                columns: new[] { "ad_account_id", "snapshot_date" },
                unique: true,
                filter: "level = 'Account'");

            migrationBuilder.CreateIndex(
                name: "ux_insight_snapshots_ad_date",
                table: "insight_snapshots",
                columns: new[] { "ad_id", "snapshot_date" },
                unique: true,
                filter: "level = 'Ad'");

            migrationBuilder.CreateIndex(
                name: "ux_insight_snapshots_adset_date",
                table: "insight_snapshots",
                columns: new[] { "ad_set_id", "snapshot_date" },
                unique: true,
                filter: "level = 'AdSet'");

            migrationBuilder.CreateIndex(
                name: "ux_insight_snapshots_campaign_date",
                table: "insight_snapshots",
                columns: new[] { "campaign_id", "snapshot_date" },
                unique: true,
                filter: "level = 'Campaign'");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "insight_snapshots");
        }
    }
}
