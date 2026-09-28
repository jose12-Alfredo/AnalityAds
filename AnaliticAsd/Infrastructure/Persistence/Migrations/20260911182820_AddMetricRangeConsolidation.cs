using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AnaliticAsd.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddMetricRangeConsolidation : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<decimal>(
                name: "spend",
                table: "insight_snapshots",
                type: "numeric(20,6)",
                precision: 20,
                scale: 6,
                nullable: true,
                oldClrType: typeof(decimal),
                oldType: "numeric(20,6)",
                oldPrecision: 20,
                oldScale: 6);

            migrationBuilder.AlterColumn<long>(
                name: "reach",
                table: "insight_snapshots",
                type: "bigint",
                nullable: true,
                oldClrType: typeof(long),
                oldType: "bigint");

            migrationBuilder.AlterColumn<decimal>(
                name: "purchases",
                table: "insight_snapshots",
                type: "numeric(28,10)",
                precision: 28,
                scale: 10,
                nullable: true,
                oldClrType: typeof(decimal),
                oldType: "numeric(28,10)",
                oldPrecision: 28,
                oldScale: 10);

            migrationBuilder.AlterColumn<decimal>(
                name: "purchase_value",
                table: "insight_snapshots",
                type: "numeric(20,6)",
                precision: 20,
                scale: 6,
                nullable: true,
                oldClrType: typeof(decimal),
                oldType: "numeric(20,6)",
                oldPrecision: 20,
                oldScale: 6);

            migrationBuilder.AlterColumn<long>(
                name: "link_clicks",
                table: "insight_snapshots",
                type: "bigint",
                nullable: true,
                oldClrType: typeof(long),
                oldType: "bigint");

            migrationBuilder.AlterColumn<decimal>(
                name: "leads",
                table: "insight_snapshots",
                type: "numeric(28,10)",
                precision: 28,
                scale: 10,
                nullable: true,
                oldClrType: typeof(decimal),
                oldType: "numeric(28,10)",
                oldPrecision: 28,
                oldScale: 10);

            migrationBuilder.AlterColumn<long>(
                name: "impressions",
                table: "insight_snapshots",
                type: "bigint",
                nullable: true,
                oldClrType: typeof(long),
                oldType: "bigint");

            migrationBuilder.AddColumn<string>(
                name: "observed_data_quality",
                table: "insight_snapshots",
                type: "character varying(32)",
                maxLength: 32,
                nullable: false,
                defaultValue: "LegacyZeroNormalized");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "observed_data_quality",
                table: "insight_snapshots");

            migrationBuilder.AlterColumn<decimal>(
                name: "spend",
                table: "insight_snapshots",
                type: "numeric(20,6)",
                precision: 20,
                scale: 6,
                nullable: false,
                defaultValue: 0m,
                oldClrType: typeof(decimal),
                oldType: "numeric(20,6)",
                oldPrecision: 20,
                oldScale: 6,
                oldNullable: true);

            migrationBuilder.AlterColumn<long>(
                name: "reach",
                table: "insight_snapshots",
                type: "bigint",
                nullable: false,
                defaultValue: 0L,
                oldClrType: typeof(long),
                oldType: "bigint",
                oldNullable: true);

            migrationBuilder.AlterColumn<decimal>(
                name: "purchases",
                table: "insight_snapshots",
                type: "numeric(28,10)",
                precision: 28,
                scale: 10,
                nullable: false,
                defaultValue: 0m,
                oldClrType: typeof(decimal),
                oldType: "numeric(28,10)",
                oldPrecision: 28,
                oldScale: 10,
                oldNullable: true);

            migrationBuilder.AlterColumn<decimal>(
                name: "purchase_value",
                table: "insight_snapshots",
                type: "numeric(20,6)",
                precision: 20,
                scale: 6,
                nullable: false,
                defaultValue: 0m,
                oldClrType: typeof(decimal),
                oldType: "numeric(20,6)",
                oldPrecision: 20,
                oldScale: 6,
                oldNullable: true);

            migrationBuilder.AlterColumn<long>(
                name: "link_clicks",
                table: "insight_snapshots",
                type: "bigint",
                nullable: false,
                defaultValue: 0L,
                oldClrType: typeof(long),
                oldType: "bigint",
                oldNullable: true);

            migrationBuilder.AlterColumn<decimal>(
                name: "leads",
                table: "insight_snapshots",
                type: "numeric(28,10)",
                precision: 28,
                scale: 10,
                nullable: false,
                defaultValue: 0m,
                oldClrType: typeof(decimal),
                oldType: "numeric(28,10)",
                oldPrecision: 28,
                oldScale: 10,
                oldNullable: true);

            migrationBuilder.AlterColumn<long>(
                name: "impressions",
                table: "insight_snapshots",
                type: "bigint",
                nullable: false,
                defaultValue: 0L,
                oldClrType: typeof(long),
                oldType: "bigint",
                oldNullable: true);
        }
    }
}
