START TRANSACTION;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261111132730_ChavesGeradasNaAplicacao') THEN
    INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
    VALUES ('20261111132730_ChavesGeradasNaAplicacao', '10.0.15');
    END IF;
END $EF$;
COMMIT;

