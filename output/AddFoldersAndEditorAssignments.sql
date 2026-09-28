START TRANSACTION;
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

CREATE INDEX ix_client_editor_assignments_agency_user ON client_editor_assignments (agency_id, user_id);

CREATE INDEX "IX_client_editor_assignments_client_id_agency_id" ON client_editor_assignments (client_id, agency_id);

CREATE INDEX "IX_client_editor_assignments_granted_by_user_id" ON client_editor_assignments (granted_by_user_id);

CREATE INDEX "IX_folders_client_id_agency_id" ON folders (client_id, agency_id);

CREATE INDEX "IX_folders_parent_folder_id_client_id_agency_id" ON folders (parent_folder_id, client_id, agency_id);

CREATE INDEX ix_folders_workspace_archive ON folders (agency_id, client_id, archived_at_utc);

CREATE INDEX ix_folders_workspace_parent_sort ON folders (agency_id, client_id, parent_folder_id, sort_order);

INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
VALUES ('20260917202057_AddFoldersAndEditorAssignments', '10.0.11');

COMMIT;

