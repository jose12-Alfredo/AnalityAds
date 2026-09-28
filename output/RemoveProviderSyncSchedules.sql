START TRANSACTION;
DROP TABLE provider_sync_schedules;

DELETE FROM "__EFMigrationsHistory"
WHERE "MigrationId" = '20260922162515_AddProviderSyncSchedules';

COMMIT;

