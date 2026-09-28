START TRANSACTION;
ALTER TABLE ad_accounts DROP CONSTRAINT "FK_ad_accounts_data_sources_data_source_id_client_id";

DROP TABLE data_sources;

DROP TABLE provider_connections;

DROP INDEX "IX_ad_accounts_data_source_id_client_id";

DROP INDEX ux_ad_accounts_data_source_id;

ALTER TABLE ad_accounts DROP COLUMN data_source_id;

DELETE FROM "__EFMigrationsHistory"
WHERE "MigrationId" = '20260917195257_AddGenericDataSources';

COMMIT;

