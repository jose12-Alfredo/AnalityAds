using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AnaliticAsd.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddClientAccess : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddUniqueConstraint(
                name: "AK_clients_id_agency_id",
                table: "clients",
                columns: new[] { "id", "agency_id" });

            migrationBuilder.CreateTable(
                name: "client_accesses",
                columns: table => new
                {
                    agency_id = table.Column<Guid>(type: "uuid", nullable: false),
                    client_id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    granted_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_client_accesses", x => new { x.agency_id, x.client_id, x.user_id });
                    table.ForeignKey(
                        name: "FK_client_accesses_clients_client_id_agency_id",
                        columns: x => new { x.client_id, x.agency_id },
                        principalTable: "clients",
                        principalColumns: new[] { "id", "agency_id" },
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_client_accesses_memberships_agency_id_user_id",
                        columns: x => new { x.agency_id, x.user_id },
                        principalTable: "memberships",
                        principalColumns: new[] { "agency_id", "user_id" },
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "client_invitations",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    agency_id = table.Column<Guid>(type: "uuid", nullable: false),
                    client_id = table.Column<Guid>(type: "uuid", nullable: false),
                    created_by_user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    email = table.Column<string>(type: "character varying(320)", maxLength: 320, nullable: false),
                    normalized_email = table.Column<string>(type: "character varying(320)", maxLength: 320, nullable: false),
                    token_hash = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    created_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    expires_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    accepted_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    revoked_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    version = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_client_invitations", x => x.id);
                    table.ForeignKey(
                        name: "FK_client_invitations_clients_client_id_agency_id",
                        columns: x => new { x.client_id, x.agency_id },
                        principalTable: "clients",
                        principalColumns: new[] { "id", "agency_id" },
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_client_accesses_agency_id_user_id",
                table: "client_accesses",
                columns: new[] { "agency_id", "user_id" });

            migrationBuilder.CreateIndex(
                name: "IX_client_accesses_client_id_agency_id",
                table: "client_accesses",
                columns: new[] { "client_id", "agency_id" });

            migrationBuilder.CreateIndex(
                name: "IX_client_invitations_agency_id_client_id",
                table: "client_invitations",
                columns: new[] { "agency_id", "client_id" });

            migrationBuilder.CreateIndex(
                name: "IX_client_invitations_client_id_agency_id",
                table: "client_invitations",
                columns: new[] { "client_id", "agency_id" });

            migrationBuilder.CreateIndex(
                name: "IX_client_invitations_token_hash",
                table: "client_invitations",
                column: "token_hash",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "client_accesses");

            migrationBuilder.DropTable(
                name: "client_invitations");

            migrationBuilder.DropUniqueConstraint(
                name: "AK_clients_id_agency_id",
                table: "clients");
        }
    }
}
