START TRANSACTION;
ALTER TABLE ad_accounts ADD data_source_id uuid;

CREATE TABLE provider_connections (
    id uuid NOT NULL,
    agency_id uuid NOT NULL,
    provider character varying(40) NOT NULL,
    display_name character varying(200) NOT NULL,
    external_subject_id character varying(300),
    protected_credential_payload character varying(8192) NOT NULL,
    expires_at_utc timestamp with time zone,
    status character varying(40) NOT NULL,
    authorized_at_utc timestamp with time zone NOT NULL,
    last_succeeded_at_utc timestamp with time zone,
    last_error_code character varying(100),
    created_at_utc timestamp with time zone NOT NULL,
    updated_at_utc timestamp with time zone NOT NULL,
    version uuid NOT NULL,
    CONSTRAINT "PK_provider_connections" PRIMARY KEY (id),
    CONSTRAINT "AK_provider_connections_id_agency_id" UNIQUE (id, agency_id),
    CONSTRAINT "FK_provider_connections_agencies_agency_id" FOREIGN KEY (agency_id) REFERENCES agencies (id) ON DELETE CASCADE
);

CREATE TABLE data_sources (
    id uuid NOT NULL,
    agency_id uuid NOT NULL,
    client_id uuid NOT NULL,
    provider_connection_id uuid,
    provider character varying(40) NOT NULL,
    source_type character varying(40) NOT NULL,
    external_id character varying(300) NOT NULL,
    name character varying(200) NOT NULL,
    currency character(3),
    time_zone character varying(100) NOT NULL,
    is_active boolean NOT NULL,
    available_since date,
    available_until date,
    last_synced_at_utc timestamp with time zone,
    archived_at_utc timestamp with time zone,
    created_at_utc timestamp with time zone NOT NULL,
    updated_at_utc timestamp with time zone NOT NULL,
    version uuid NOT NULL,
    CONSTRAINT "PK_data_sources" PRIMARY KEY (id),
    CONSTRAINT "AK_data_sources_id_client_id" UNIQUE (id, client_id),
    CONSTRAINT "FK_data_sources_agencies_agency_id" FOREIGN KEY (agency_id) REFERENCES agencies (id) ON DELETE RESTRICT,
    CONSTRAINT "FK_data_sources_clients_client_id_agency_id" FOREIGN KEY (client_id, agency_id) REFERENCES clients (id, agency_id) ON DELETE RESTRICT,
    CONSTRAINT "FK_data_sources_provider_connections_provider_connection_id_ag~" FOREIGN KEY (provider_connection_id, agency_id) REFERENCES provider_connections (id, agency_id) ON DELETE RESTRICT
);

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

ALTER TABLE ad_accounts ALTER COLUMN data_source_id SET NOT NULL;

CREATE UNIQUE INDEX "IX_ad_accounts_data_source_id_client_id" ON ad_accounts (data_source_id, client_id);

CREATE UNIQUE INDEX ux_ad_accounts_data_source_id ON ad_accounts (data_source_id);

CREATE INDEX "IX_data_sources_agency_id_client_id" ON data_sources (agency_id, client_id);

CREATE UNIQUE INDEX "IX_data_sources_agency_id_provider_source_type_external_id" ON data_sources (agency_id, provider, source_type, external_id);

CREATE INDEX "IX_data_sources_client_id_agency_id" ON data_sources (client_id, agency_id);

CREATE INDEX "IX_data_sources_provider_connection_id_agency_id" ON data_sources (provider_connection_id, agency_id);

CREATE INDEX "IX_provider_connections_agency_id_provider" ON provider_connections (agency_id, provider);

ALTER TABLE ad_accounts ADD CONSTRAINT "FK_ad_accounts_data_sources_data_source_id_client_id" FOREIGN KEY (data_source_id, client_id) REFERENCES data_sources (id, client_id) ON DELETE CASCADE;

INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
VALUES ('20260917195257_AddGenericDataSources', '10.0.11');

COMMIT;

