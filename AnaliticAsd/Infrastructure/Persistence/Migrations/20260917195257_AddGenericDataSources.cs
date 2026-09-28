using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AnaliticAsd.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddGenericDataSources : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "data_source_id",
                table: "ad_accounts",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "provider_connections",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    agency_id = table.Column<Guid>(type: "uuid", nullable: false),
                    provider = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    display_name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    external_subject_id = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: true),
                    protected_credential_payload = table.Column<string>(type: "character varying(8192)", maxLength: 8192, nullable: false),
                    expires_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    status = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    authorized_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    last_succeeded_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    last_error_code = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    created_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    version = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_provider_connections", x => x.id);
                    table.UniqueConstraint("AK_provider_connections_id_agency_id", x => new { x.id, x.agency_id });
                    table.ForeignKey(
                        name: "FK_provider_connections_agencies_agency_id",
                        column: x => x.agency_id,
                        principalTable: "agencies",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "data_sources",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    agency_id = table.Column<Guid>(type: "uuid", nullable: false),
                    client_id = table.Column<Guid>(type: "uuid", nullable: false),
                    provider_connection_id = table.Column<Guid>(type: "uuid", nullable: true),
                    provider = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    source_type = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    external_id = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: false),
                    name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    currency = table.Column<string>(type: "character(3)", fixedLength: true, maxLength: 3, nullable: true),
                    time_zone = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    available_since = table.Column<DateOnly>(type: "date", nullable: true),
                    available_until = table.Column<DateOnly>(type: "date", nullable: true),
                    last_synced_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    archived_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    created_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    version = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_data_sources", x => x.id);
                    table.UniqueConstraint("AK_data_sources_id_client_id", x => new { x.id, x.client_id });
                    table.ForeignKey(
                        name: "FK_data_sources_agencies_agency_id",
                        column: x => x.agency_id,
                        principalTable: "agencies",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_data_sources_clients_client_id_agency_id",
                        columns: x => new { x.client_id, x.agency_id },
                        principalTable: "clients",
                        principalColumns: new[] { "id", "agency_id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_data_sources_provider_connections_provider_connection_id_ag~",
                        columns: x => new { x.provider_connection_id, x.agency_id },
                        principalTable: "provider_connections",
                        principalColumns: new[] { "id", "agency_id" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.Sql(
                """
                INSERT INTO provider_connections
                    (id, agency_id, provider, display_name, external_subject_id,
                     protected_credential_payload, expires_at_utc, status, authorized_at_utc,
                     last_succeeded_at_utc, last_error_code, created_at_utc, updated_at_utc, version)
                SELECT
                    connection.agency_id,
                    connection.agency_id,
                    'MetaAds',
                    'Meta Ads',
                    NULL,
                    connection.protected_access_token,
                    connection.expires_at_utc,
                    CASE
                        WHEN connection.expires_at_utc IS NOT NULL
                             AND connection.expires_at_utc <= CURRENT_TIMESTAMP THEN 'Expired'
                        ELSE 'Connected'
                    END,
                    connection.updated_at_utc,
                    NULL,
                    NULL,
                    connection.created_at_utc,
                    connection.updated_at_utc,
                    connection.agency_id
                FROM meta_connections AS connection;
                """);

            migrationBuilder.Sql(
                """
                INSERT INTO data_sources
                    (id, agency_id, client_id, provider_connection_id, provider, source_type,
                     external_id, name, currency, time_zone, is_active, available_since,
                     available_until, last_synced_at_utc, archived_at_utc, created_at_utc,
                     updated_at_utc, version)
                SELECT
                    account.id,
                    client.agency_id,
                    account.client_id,
                    connection.agency_id,
                    'MetaAds',
                    'AdvertisingAccount',
                    account.meta_account_id,
                    account.name,
                    account.currency,
                    account.time_zone,
                    account.is_active,
                    snapshot.available_since,
                    snapshot.available_until,
                    COALESCE(sync.completed_at_utc, snapshot.last_observed_at_utc),
                    NULL,
                    account.created_at_utc,
                    account.updated_at_utc,
                    account.id
                FROM ad_accounts AS account
                INNER JOIN clients AS client ON client.id = account.client_id
                LEFT JOIN meta_connections AS connection ON connection.agency_id = client.agency_id
                LEFT JOIN ad_account_syncs AS sync ON sync.ad_account_id = account.id
                LEFT JOIN (
                    SELECT ad_account_id,
                           MIN(snapshot_date) AS available_since,
                           MAX(snapshot_date) AS available_until,
                           MAX(observed_at_utc) AS last_observed_at_utc
                    FROM insight_snapshots
                    GROUP BY ad_account_id
                ) AS snapshot ON snapshot.ad_account_id = account.id;

                UPDATE ad_accounts
                SET data_source_id = id;
                """);

            migrationBuilder.AlterColumn<Guid>(
                name: "data_source_id",
                table: "ad_accounts",
                type: "uuid",
                nullable: false,
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldNullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_ad_accounts_data_source_id_client_id",
                table: "ad_accounts",
                columns: new[] { "data_source_id", "client_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ux_ad_accounts_data_source_id",
                table: "ad_accounts",
                column: "data_source_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_data_sources_agency_id_client_id",
                table: "data_sources",
                columns: new[] { "agency_id", "client_id" });

            migrationBuilder.CreateIndex(
                name: "IX_data_sources_agency_id_provider_source_type_external_id",
                table: "data_sources",
                columns: new[] { "agency_id", "provider", "source_type", "external_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_data_sources_client_id_agency_id",
                table: "data_sources",
                columns: new[] { "client_id", "agency_id" });

            migrationBuilder.CreateIndex(
                name: "IX_data_sources_provider_connection_id_agency_id",
                table: "data_sources",
                columns: new[] { "provider_connection_id", "agency_id" });

            migrationBuilder.CreateIndex(
                name: "IX_provider_connections_agency_id_provider",
                table: "provider_connections",
                columns: new[] { "agency_id", "provider" });

            migrationBuilder.AddForeignKey(
                name: "FK_ad_accounts_data_sources_data_source_id_client_id",
                table: "ad_accounts",
                columns: new[] { "data_source_id", "client_id" },
                principalTable: "data_sources",
                principalColumns: new[] { "id", "client_id" },
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ad_accounts_data_sources_data_source_id_client_id",
                table: "ad_accounts");

            migrationBuilder.DropTable(
                name: "data_sources");

            migrationBuilder.DropTable(
                name: "provider_connections");

            migrationBuilder.DropIndex(
                name: "IX_ad_accounts_data_source_id_client_id",
                table: "ad_accounts");

            migrationBuilder.DropIndex(
                name: "ux_ad_accounts_data_source_id",
                table: "ad_accounts");

            migrationBuilder.DropColumn(
                name: "data_source_id",
                table: "ad_accounts");
        }
    }
}
