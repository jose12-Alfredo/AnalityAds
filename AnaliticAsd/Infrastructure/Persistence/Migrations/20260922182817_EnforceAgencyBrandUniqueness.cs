using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AnaliticAsd.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class EnforceAgencyBrandUniqueness : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_brand_profiles_agency_id_client_id",
                table: "brand_profiles");

            migrationBuilder.CreateIndex(
                name: "IX_brand_profiles_agency_id",
                table: "brand_profiles",
                column: "agency_id",
                unique: true,
                filter: "client_id IS NULL");

            migrationBuilder.CreateIndex(
                name: "IX_brand_profiles_agency_id_client_id",
                table: "brand_profiles",
                columns: new[] { "agency_id", "client_id" },
                unique: true,
                filter: "client_id IS NOT NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_brand_profiles_agency_id",
                table: "brand_profiles");

            migrationBuilder.DropIndex(
                name: "IX_brand_profiles_agency_id_client_id",
                table: "brand_profiles");

            migrationBuilder.CreateIndex(
                name: "IX_brand_profiles_agency_id_client_id",
                table: "brand_profiles",
                columns: new[] { "agency_id", "client_id" },
                unique: true);
        }
    }
}
