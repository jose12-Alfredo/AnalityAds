START TRANSACTION;
DROP INDEX "IX_brand_profiles_agency_id";

DROP INDEX "IX_brand_profiles_agency_id_client_id";

CREATE UNIQUE INDEX "IX_brand_profiles_agency_id_client_id" ON brand_profiles (agency_id, client_id);

DELETE FROM "__EFMigrationsHistory"
WHERE "MigrationId" = '20260922182817_EnforceAgencyBrandUniqueness';

COMMIT;

START TRANSACTION;
DROP TABLE brand_profiles;

DROP TABLE dashboard_share_links;

DELETE FROM "__EFMigrationsHistory"
WHERE "MigrationId" = '20260922182726_AddDashboardSharingAndBranding';

COMMIT;

