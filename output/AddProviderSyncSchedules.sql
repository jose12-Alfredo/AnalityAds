START TRANSACTION;
CREATE TABLE provider_sync_schedules (
    id uuid NOT NULL,
    agency_id uuid NOT NULL,
    data_source_id uuid NOT NULL,
    is_enabled boolean NOT NULL,
    interval_minutes integer NOT NULL,
    lookback_days integer NOT NULL,
    next_run_at_utc timestamp with time zone NOT NULL,
    locked_until_utc timestamp with time zone,
    consecutive_failures integer NOT NULL,
    last_started_at_utc timestamp with time zone,
    last_succeeded_at_utc timestamp with time zone,
    last_error_code character varying(100),
    CONSTRAINT "PK_provider_sync_schedules" PRIMARY KEY (id),
    CONSTRAINT "FK_provider_sync_schedules_data_sources_data_source_id" FOREIGN KEY (data_source_id) REFERENCES data_sources (id) ON DELETE CASCADE
);

CREATE UNIQUE INDEX "IX_provider_sync_schedules_data_source_id" ON provider_sync_schedules (data_source_id);

CREATE INDEX "IX_provider_sync_schedules_is_enabled_next_run_at_utc" ON provider_sync_schedules (is_enabled, next_run_at_utc);

INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
VALUES ('20260922162515_AddProviderSyncSchedules', '10.0.11');

COMMIT;

