START TRANSACTION;
CREATE TABLE brand_profiles (
    id uuid NOT NULL,
    agency_id uuid NOT NULL,
    client_id uuid,
    logo_url character varying(2048),
    primary_color character varying(7) NOT NULL,
    secondary_color character varying(7) NOT NULL,
    background_color character varying(7) NOT NULL,
    font_family character varying(100) NOT NULL,
    updated_at_utc timestamp with time zone NOT NULL,
    version uuid NOT NULL,
    CONSTRAINT "PK_brand_profiles" PRIMARY KEY (id)
);

CREATE TABLE dashboard_share_links (
    id uuid NOT NULL,
    dashboard_id uuid NOT NULL,
    agency_id uuid NOT NULL,
    client_id uuid NOT NULL,
    created_by_user_id uuid NOT NULL,
    token_hash character varying(64) NOT NULL,
    password_hash character varying(300),
    recipient_email character varying(320),
    expires_at_utc timestamp with time zone,
    allow_filters boolean NOT NULL,
    allow_export boolean NOT NULL,
    allow_embed boolean NOT NULL,
    created_at_utc timestamp with time zone NOT NULL,
    revoked_at_utc timestamp with time zone,
    last_accessed_at_utc timestamp with time zone,
    access_count bigint NOT NULL,
    version uuid NOT NULL,
    CONSTRAINT "PK_dashboard_share_links" PRIMARY KEY (id),
    CONSTRAINT "FK_dashboard_share_links_dashboards_dashboard_id" FOREIGN KEY (dashboard_id) REFERENCES dashboards (id) ON DELETE CASCADE,
    CONSTRAINT "FK_dashboard_share_links_users_created_by_user_id" FOREIGN KEY (created_by_user_id) REFERENCES users (id) ON DELETE RESTRICT
);

CREATE UNIQUE INDEX "IX_brand_profiles_agency_id_client_id" ON brand_profiles (agency_id, client_id);

CREATE INDEX "IX_dashboard_share_links_agency_id_dashboard_id" ON dashboard_share_links (agency_id, dashboard_id);

CREATE INDEX "IX_dashboard_share_links_created_by_user_id" ON dashboard_share_links (created_by_user_id);

CREATE INDEX "IX_dashboard_share_links_dashboard_id" ON dashboard_share_links (dashboard_id);

CREATE UNIQUE INDEX "IX_dashboard_share_links_token_hash" ON dashboard_share_links (token_hash);

INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
VALUES ('20260922182726_AddDashboardSharingAndBranding', '10.0.11');

COMMIT;

START TRANSACTION;
DROP INDEX "IX_brand_profiles_agency_id_client_id";

CREATE UNIQUE INDEX "IX_brand_profiles_agency_id" ON brand_profiles (agency_id) WHERE client_id IS NULL;

CREATE UNIQUE INDEX "IX_brand_profiles_agency_id_client_id" ON brand_profiles (agency_id, client_id) WHERE client_id IS NOT NULL;

INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
VALUES ('20260922182817_EnforceAgencyBrandUniqueness', '10.0.11');

COMMIT;

