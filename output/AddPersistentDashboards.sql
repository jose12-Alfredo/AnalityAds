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

