START TRANSACTION;

DROP INDEX "IX_generated_articles_Key";

ALTER TABLE uploads ADD "UserId" uuid;

CREATE INDEX "IX_uploads_UserId" ON uploads ("UserId");

ALTER TABLE uploads ADD CONSTRAINT "FK_uploads_users_UserId" FOREIGN KEY ("UserId") REFERENCES users ("Id") ON DELETE CASCADE;

INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
VALUES ('20260716141153_AddUserOwnedWorkspaces', '8.0.11');

COMMIT;