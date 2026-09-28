START TRANSACTION;
CREATE TABLE provider_metric_snapshots (
    id uuid NOT NULL,
    agency_id uuid NOT NULL,
    data_source_id uuid NOT NULL,
    date date NOT NULL,
    dimension_key character varying(300) NOT NULL,
    dimension_name character varying(300) NOT NULL,
    spend numeric(20,6),
    impressions bigint,
    clicks bigint,
    conversions numeric(28,10),
    conversion_value numeric(20,6),
    active_users bigint,
    sessions bigint,
    views bigint,
    observed_at_utc timestamp with time zone NOT NULL,
    CONSTRAINT "PK_provider_metric_snapshots" PRIMARY KEY (id),
    CONSTRAINT "FK_provider_metric_snapshots_data_sources_data_source_id" FOREIGN KEY (data_source_id) REFERENCES data_sources (id) ON DELETE CASCADE
);

CREATE UNIQUE INDEX "IX_provider_metric_snapshots_data_source_id_date_dimension_key" ON provider_metric_snapshots (data_source_id, date, dimension_key);

INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
VALUES ('20260922151443_AddProviderMetricSnapshots', '10.0.11');

COMMIT;

