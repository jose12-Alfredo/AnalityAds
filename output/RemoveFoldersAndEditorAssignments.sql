START TRANSACTION;
DROP TABLE client_editor_assignments;

DROP TABLE folders;

DELETE FROM "__EFMigrationsHistory"
WHERE "MigrationId" = '20260917202057_AddFoldersAndEditorAssignments';

COMMIT;

