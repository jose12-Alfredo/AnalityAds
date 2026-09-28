START TRANSACTION;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260917195257_AddGenericDataSources') THEN
    ALTER TABLE ad_accounts ADD data_source_id uuid;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260917195257_AddGenericDataSources') THEN
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
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260917195257_AddGenericDataSources') THEN
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
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260917195257_AddGenericDataSources') THEN
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
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260917195257_AddGenericDataSources') THEN
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
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260917195257_AddGenericDataSources') THEN
    ALTER TABLE ad_accounts ALTER COLUMN data_source_id SET NOT NULL;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260917195257_AddGenericDataSources') THEN
    CREATE UNIQUE INDEX "IX_ad_accounts_data_source_id_client_id" ON ad_accounts (data_source_id, client_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260917195257_AddGenericDataSources') THEN
    CREATE UNIQUE INDEX ux_ad_accounts_data_source_id ON ad_accounts (data_source_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260917195257_AddGenericDataSources') THEN
    CREATE INDEX "IX_data_sources_agency_id_client_id" ON data_sources (agency_id, client_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260917195257_AddGenericDataSources') THEN
    CREATE UNIQUE INDEX "IX_data_sources_agency_id_provider_source_type_external_id" ON data_sources (agency_id, provider, source_type, external_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260917195257_AddGenericDataSources') THEN
    CREATE INDEX "IX_data_sources_client_id_agency_id" ON data_sources (client_id, agency_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260917195257_AddGenericDataSources') THEN
    CREATE INDEX "IX_data_sources_provider_connection_id_agency_id" ON data_sources (provider_connection_id, agency_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260917195257_AddGenericDataSources') THEN
    CREATE INDEX "IX_provider_connections_agency_id_provider" ON provider_connections (agency_id, provider);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260917195257_AddGenericDataSources') THEN
    ALTER TABLE ad_accounts ADD CONSTRAINT "FK_ad_accounts_data_sources_data_source_id_client_id" FOREIGN KEY (data_source_id, client_id) REFERENCES data_sources (id, client_id) ON DELETE CASCADE;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260917195257_AddGenericDataSources') THEN
    INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
    VALUES ('20260917195257_AddGenericDataSources', '10.0.11');
    END IF;
END $EF$;
COMMIT;

START TRANSACTION;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260917202057_AddFoldersAndEditorAssignments') THEN
    CREATE TABLE client_editor_assignments (
        agency_id uuid NOT NULL,
        client_id uuid NOT NULL,
        user_id uuid NOT NULL,
        granted_by_user_id uuid NOT NULL,
        granted_at_utc timestamp with time zone NOT NULL,
        version uuid NOT NULL,
        CONSTRAINT "PK_client_editor_assignments" PRIMARY KEY (agency_id, client_id, user_id),
        CONSTRAINT "FK_client_editor_assignments_clients_client_id_agency_id" FOREIGN KEY (client_id, agency_id) REFERENCES clients (id, agency_id) ON DELETE CASCADE,
        CONSTRAINT "FK_client_editor_assignments_memberships_agency_id_user_id" FOREIGN KEY (agency_id, user_id) REFERENCES memberships (agency_id, user_id) ON DELETE CASCADE,
        CONSTRAINT "FK_client_editor_assignments_users_granted_by_user_id" FOREIGN KEY (granted_by_user_id) REFERENCES users (id) ON DELETE RESTRICT
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260917202057_AddFoldersAndEditorAssignments') THEN
    CREATE TABLE folders (
        id uuid NOT NULL,
        agency_id uuid NOT NULL,
        client_id uuid NOT NULL,
        parent_folder_id uuid,
        name character varying(200) NOT NULL,
        sort_order integer NOT NULL,
        archived_at_utc timestamp with time zone,
        created_at_utc timestamp with time zone NOT NULL,
        updated_at_utc timestamp with time zone NOT NULL,
        version uuid NOT NULL,
        CONSTRAINT "PK_folders" PRIMARY KEY (id),
        CONSTRAINT "AK_folders_id_client_id_agency_id" UNIQUE (id, client_id, agency_id),
        CONSTRAINT "FK_folders_clients_client_id_agency_id" FOREIGN KEY (client_id, agency_id) REFERENCES clients (id, agency_id) ON DELETE CASCADE,
        CONSTRAINT "FK_folders_folders_parent_folder_id_client_id_agency_id" FOREIGN KEY (parent_folder_id, client_id, agency_id) REFERENCES folders (id, client_id, agency_id) ON DELETE RESTRICT
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260917202057_AddFoldersAndEditorAssignments') THEN
    INSERT INTO client_editor_assignments
        (agency_id, client_id, user_id, granted_by_user_id, granted_at_utc, version)
    SELECT
        membership.agency_id,
        client.id,
        membership.user_id,
        COALESCE((
            SELECT grantor.user_id
            FROM memberships AS grantor
            WHERE grantor.agency_id = membership.agency_id
              AND grantor.role IN ('Owner', 'Admin')
            ORDER BY CASE grantor.role WHEN 'Owner' THEN 0 ELSE 1 END,
                     grantor.created_at_utc,
                     grantor.user_id
            LIMIT 1
        ), membership.user_id),
        GREATEST(membership.created_at_utc, client.created_at_utc),
        client.id
    FROM memberships AS membership
    INNER JOIN clients AS client ON client.agency_id = membership.agency_id
    WHERE membership.role = 'Analyst';
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260917202057_AddFoldersAndEditorAssignments') THEN
    CREATE INDEX ix_client_editor_assignments_agency_user ON client_editor_assignments (agency_id, user_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260917202057_AddFoldersAndEditorAssignments') THEN
    CREATE INDEX "IX_client_editor_assignments_client_id_agency_id" ON client_editor_assignments (client_id, agency_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260917202057_AddFoldersAndEditorAssignments') THEN
    CREATE INDEX "IX_client_editor_assignments_granted_by_user_id" ON client_editor_assignments (granted_by_user_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260917202057_AddFoldersAndEditorAssignments') THEN
    CREATE INDEX "IX_folders_client_id_agency_id" ON folders (client_id, agency_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260917202057_AddFoldersAndEditorAssignments') THEN
    CREATE INDEX "IX_folders_parent_folder_id_client_id_agency_id" ON folders (parent_folder_id, client_id, agency_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260917202057_AddFoldersAndEditorAssignments') THEN
    CREATE INDEX ix_folders_workspace_archive ON folders (agency_id, client_id, archived_at_utc);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260917202057_AddFoldersAndEditorAssignments') THEN
    CREATE INDEX ix_folders_workspace_parent_sort ON folders (agency_id, client_id, parent_folder_id, sort_order);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260917202057_AddFoldersAndEditorAssignments') THEN
    INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
    VALUES ('20260917202057_AddFoldersAndEditorAssignments', '10.0.11');
    END IF;
END $EF$;
COMMIT;

START TRANSACTION;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260917212324_AddPersistentDashboards') THEN
    CREATE TABLE dashboard_templates (
        id uuid NOT NULL,
        agency_id uuid NOT NULL,
        client_id uuid,
        name character varying(200) NOT NULL,
        description character varying(1000) NOT NULL,
        schema_version integer NOT NULL,
        definition_json jsonb NOT NULL,
        created_by_user_id uuid NOT NULL,
        created_at_utc timestamp with time zone NOT NULL,
        version uuid NOT NULL,
        CONSTRAINT "PK_dashboard_templates" PRIMARY KEY (id),
        CONSTRAINT "FK_dashboard_templates_agencies_agency_id" FOREIGN KEY (agency_id) REFERENCES agencies (id) ON DELETE RESTRICT,
        CONSTRAINT "FK_dashboard_templates_clients_client_id_agency_id" FOREIGN KEY (client_id, agency_id) REFERENCES clients (id, agency_id) ON DELETE CASCADE,
        CONSTRAINT "FK_dashboard_templates_users_created_by_user_id" FOREIGN KEY (created_by_user_id) REFERENCES users (id) ON DELETE RESTRICT
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260917212324_AddPersistentDashboards') THEN
    CREATE TABLE dashboards (
        id uuid NOT NULL,
        agency_id uuid NOT NULL,
        client_id uuid NOT NULL,
        folder_id uuid,
        title character varying(200) NOT NULL,
        description character varying(2000) NOT NULL,
        created_by_user_id uuid NOT NULL,
        current_publication_number integer,
        archived_at_utc timestamp with time zone,
        created_at_utc timestamp with time zone NOT NULL,
        updated_at_utc timestamp with time zone NOT NULL,
        version uuid NOT NULL,
        CONSTRAINT "PK_dashboards" PRIMARY KEY (id),
        CONSTRAINT "AK_dashboards_id_client_id_agency_id" UNIQUE (id, client_id, agency_id),
        CONSTRAINT "FK_dashboards_clients_client_id_agency_id" FOREIGN KEY (client_id, agency_id) REFERENCES clients (id, agency_id) ON DELETE CASCADE,
        CONSTRAINT "FK_dashboards_folders_folder_id_client_id_agency_id" FOREIGN KEY (folder_id, client_id, agency_id) REFERENCES folders (id, client_id, agency_id) ON DELETE RESTRICT,
        CONSTRAINT "FK_dashboards_users_created_by_user_id" FOREIGN KEY (created_by_user_id) REFERENCES users (id) ON DELETE RESTRICT
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260917212324_AddPersistentDashboards') THEN
    CREATE TABLE dashboard_drafts (
        dashboard_id uuid NOT NULL,
        agency_id uuid NOT NULL,
        client_id uuid NOT NULL,
        schema_version integer NOT NULL,
        revision integer NOT NULL,
        definition_json jsonb NOT NULL,
        updated_by_user_id uuid NOT NULL,
        updated_at_utc timestamp with time zone NOT NULL,
        version uuid NOT NULL,
        CONSTRAINT "PK_dashboard_drafts" PRIMARY KEY (dashboard_id),
        CONSTRAINT "FK_dashboard_drafts_dashboards_dashboard_id_client_id_agency_id" FOREIGN KEY (dashboard_id, client_id, agency_id) REFERENCES dashboards (id, client_id, agency_id) ON DELETE CASCADE,
        CONSTRAINT "FK_dashboard_drafts_users_updated_by_user_id" FOREIGN KEY (updated_by_user_id) REFERENCES users (id) ON DELETE RESTRICT
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260917212324_AddPersistentDashboards') THEN
    CREATE TABLE dashboard_versions (
        id uuid NOT NULL,
        dashboard_id uuid NOT NULL,
        agency_id uuid NOT NULL,
        client_id uuid NOT NULL,
        publication_number integer NOT NULL,
        draft_revision integer NOT NULL,
        schema_version integer NOT NULL,
        definition_json jsonb NOT NULL,
        definition_hash character varying(64) NOT NULL,
        published_by_user_id uuid NOT NULL,
        published_at_utc timestamp with time zone NOT NULL,
        CONSTRAINT "PK_dashboard_versions" PRIMARY KEY (id),
        CONSTRAINT "FK_dashboard_versions_dashboards_dashboard_id_client_id_agency~" FOREIGN KEY (dashboard_id, client_id, agency_id) REFERENCES dashboards (id, client_id, agency_id) ON DELETE CASCADE,
        CONSTRAINT "FK_dashboard_versions_users_published_by_user_id" FOREIGN KEY (published_by_user_id) REFERENCES users (id) ON DELETE RESTRICT
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260917212324_AddPersistentDashboards') THEN
    CREATE UNIQUE INDEX "IX_dashboard_drafts_dashboard_id_client_id_agency_id" ON dashboard_drafts (dashboard_id, client_id, agency_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260917212324_AddPersistentDashboards') THEN
    CREATE INDEX "IX_dashboard_drafts_updated_by_user_id" ON dashboard_drafts (updated_by_user_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260917212324_AddPersistentDashboards') THEN
    CREATE INDEX "IX_dashboard_templates_client_id_agency_id" ON dashboard_templates (client_id, agency_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260917212324_AddPersistentDashboards') THEN
    CREATE INDEX "IX_dashboard_templates_created_by_user_id" ON dashboard_templates (created_by_user_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260917212324_AddPersistentDashboards') THEN
    CREATE INDEX ix_dashboard_templates_scope_name ON dashboard_templates (agency_id, client_id, name);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260917212324_AddPersistentDashboards') THEN
    CREATE INDEX "IX_dashboard_versions_dashboard_id_client_id_agency_id" ON dashboard_versions (dashboard_id, client_id, agency_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260917212324_AddPersistentDashboards') THEN
    CREATE INDEX "IX_dashboard_versions_published_by_user_id" ON dashboard_versions (published_by_user_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260917212324_AddPersistentDashboards') THEN
    CREATE UNIQUE INDEX ux_dashboard_versions_publication ON dashboard_versions (dashboard_id, publication_number);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260917212324_AddPersistentDashboards') THEN
    CREATE INDEX "IX_dashboards_client_id_agency_id" ON dashboards (client_id, agency_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260917212324_AddPersistentDashboards') THEN
    CREATE INDEX "IX_dashboards_created_by_user_id" ON dashboards (created_by_user_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260917212324_AddPersistentDashboards') THEN
    CREATE INDEX "IX_dashboards_folder_id_client_id_agency_id" ON dashboards (folder_id, client_id, agency_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260917212324_AddPersistentDashboards') THEN
    CREATE INDEX ix_dashboards_workspace_folder_archive ON dashboards (agency_id, client_id, folder_id, archived_at_utc);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260917212324_AddPersistentDashboards') THEN
    INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
    VALUES ('20260917212324_AddPersistentDashboards', '10.0.11');
    END IF;
END $EF$;
COMMIT;

START TRANSACTION;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260922151443_AddProviderMetricSnapshots') THEN
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
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260922151443_AddProviderMetricSnapshots') THEN
    CREATE UNIQUE INDEX "IX_provider_metric_snapshots_data_source_id_date_dimension_key" ON provider_metric_snapshots (data_source_id, date, dimension_key);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260922151443_AddProviderMetricSnapshots') THEN
    INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
    VALUES ('20260922151443_AddProviderMetricSnapshots', '10.0.11');
    END IF;
END $EF$;
COMMIT;

START TRANSACTION;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260922162515_AddProviderSyncSchedules') THEN
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
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260922162515_AddProviderSyncSchedules') THEN
    CREATE UNIQUE INDEX "IX_provider_sync_schedules_data_source_id" ON provider_sync_schedules (data_source_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260922162515_AddProviderSyncSchedules') THEN
    CREATE INDEX "IX_provider_sync_schedules_is_enabled_next_run_at_utc" ON provider_sync_schedules (is_enabled, next_run_at_utc);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260922162515_AddProviderSyncSchedules') THEN
    INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
    VALUES ('20260922162515_AddProviderSyncSchedules', '10.0.11');
    END IF;
END $EF$;
COMMIT;

START TRANSACTION;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260922182726_AddDashboardSharingAndBranding') THEN
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
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260922182726_AddDashboardSharingAndBranding') THEN
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
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260922182726_AddDashboardSharingAndBranding') THEN
    CREATE UNIQUE INDEX "IX_brand_profiles_agency_id_client_id" ON brand_profiles (agency_id, client_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260922182726_AddDashboardSharingAndBranding') THEN
    CREATE INDEX "IX_dashboard_share_links_agency_id_dashboard_id" ON dashboard_share_links (agency_id, dashboard_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260922182726_AddDashboardSharingAndBranding') THEN
    CREATE INDEX "IX_dashboard_share_links_created_by_user_id" ON dashboard_share_links (created_by_user_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260922182726_AddDashboardSharingAndBranding') THEN
    CREATE INDEX "IX_dashboard_share_links_dashboard_id" ON dashboard_share_links (dashboard_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260922182726_AddDashboardSharingAndBranding') THEN
    CREATE UNIQUE INDEX "IX_dashboard_share_links_token_hash" ON dashboard_share_links (token_hash);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260922182726_AddDashboardSharingAndBranding') THEN
    INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
    VALUES ('20260922182726_AddDashboardSharingAndBranding', '10.0.11');
    END IF;
END $EF$;
COMMIT;

START TRANSACTION;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260922182817_EnforceAgencyBrandUniqueness') THEN
    DROP INDEX "IX_brand_profiles_agency_id_client_id";
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260922182817_EnforceAgencyBrandUniqueness') THEN
    CREATE UNIQUE INDEX "IX_brand_profiles_agency_id" ON brand_profiles (agency_id) WHERE client_id IS NULL;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260922182817_EnforceAgencyBrandUniqueness') THEN
    CREATE UNIQUE INDEX "IX_brand_profiles_agency_id_client_id" ON brand_profiles (agency_id, client_id) WHERE client_id IS NOT NULL;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260922182817_EnforceAgencyBrandUniqueness') THEN
    INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
    VALUES ('20260922182817_EnforceAgencyBrandUniqueness', '10.0.11');
    END IF;
END $EF$;
COMMIT;

