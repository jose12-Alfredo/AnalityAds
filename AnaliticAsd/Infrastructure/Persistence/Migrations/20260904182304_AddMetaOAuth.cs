using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AnaliticAsd.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddMetaOAuth : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "meta_connections",
                columns: table => new
                {
                    agency_id = table.Column<Guid>(type: "uuid", nullable: false),
                    protected_access_token = table.Column<string>(type: "character varying(4096)", maxLength: 4096, nullable: false),
                    expires_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    created_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_meta_connections", x => x.agency_id);
                    table.ForeignKey(
                        name: "FK_meta_connections_agencies_agency_id",
                        column: x => x.agency_id,
                        principalTable: "agencies",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "meta_connections");
        }
    }
}
