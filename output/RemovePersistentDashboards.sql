START TRANSACTION;
DROP TABLE dashboard_drafts;

DROP TABLE dashboard_templates;

DROP TABLE dashboard_versions;

DROP TABLE dashboards;

DELETE FROM "__EFMigrationsHistory"
WHERE "MigrationId" = '20260917212324_AddPersistentDashboards';

COMMIT;

