START TRANSACTION;
DROP TABLE provider_metric_snapshots;

DELETE FROM "__EFMigrationsHistory"
WHERE "MigrationId" = '20260922151443_AddProviderMetricSnapshots';

COMMIT;

