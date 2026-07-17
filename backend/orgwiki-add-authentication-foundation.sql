START TRANSACTION;

ALTER TABLE uploads ADD "UploadedBy" character varying(128);

CREATE TABLE users (
    "Id" uuid NOT NULL,
    "FullName" character varying(256) NOT NULL,
    "Email" character varying(320) NOT NULL,
    "PasswordHash" text NOT NULL,
    "CreatedAtUtc" timestamp with time zone NOT NULL,
    "UpdatedAtUtc" timestamp with time zone NOT NULL,
    CONSTRAINT "PK_users" PRIMARY KEY ("Id")
);

CREATE UNIQUE INDEX "IX_users_Email" ON users ("Email");

INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
VALUES ('20260716132224_AddAuthenticationFoundation', '8.0.11');

COMMIT;