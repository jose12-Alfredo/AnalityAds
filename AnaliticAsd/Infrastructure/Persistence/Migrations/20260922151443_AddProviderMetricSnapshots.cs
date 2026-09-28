using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AnaliticAsd.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddProviderMetricSnapshots : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "provider_metric_snapshots",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    agency_id = table.Column<Guid>(type: "uuid", nullable: false),
                    data_source_id = table.Column<Guid>(type: "uuid", nullable: false),
                    date = table.Column<DateOnly>(type: "date", nullable: false),
                    dimension_key = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: false),
                    dimension_name = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: false),
                    spend = table.Column<decimal>(type: "numeric(20,6)", precision: 20, scale: 6, nullable: true),
                    impressions = table.Column<long>(type: "bigint", nullable: true),
                    clicks = table.Column<long>(type: "bigint", nullable: true),
                    conversions = table.Column<decimal>(type: "numeric(28,10)", precision: 28, scale: 10, nullable: true),
                    conversion_value = table.Column<decimal>(type: "numeric(20,6)", precision: 20, scale: 6, nullable: true),
                    active_users = table.Column<long>(type: "bigint", nullable: true),
                    sessions = table.Column<long>(type: "bigint", nullable: true),
                    views = table.Column<long>(type: "bigint", nullable: true),
                    observed_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_provider_metric_snapshots", x => x.id);
                    table.ForeignKey(
                        name: "FK_provider_metric_snapshots_data_sources_data_source_id",
                        column: x => x.data_source_id,
                        principalTable: "data_sources",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_provider_metric_snapshots_data_source_id_date_dimension_key",
                table: "provider_metric_snapshots",
                columns: new[] { "data_source_id", "date", "dimension_key" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "provider_metric_snapshots");
        }
    }
}
