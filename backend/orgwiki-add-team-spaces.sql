START TRANSACTION;

CREATE TABLE team_spaces (
    "Id" uuid NOT NULL,
    "Name" character varying(128) NOT NULL,
    "Slug" character varying(128) NOT NULL,
    "Description" character varying(1024) NOT NULL,
    "CreatedAtUtc" timestamp with time zone NOT NULL,
    CONSTRAINT "PK_team_spaces" PRIMARY KEY ("Id")
);

CREATE TABLE team_space_articles (
    "TeamSpaceId" uuid NOT NULL,
    "GeneratedArticleId" uuid NOT NULL,
    CONSTRAINT "PK_team_space_articles" PRIMARY KEY ("TeamSpaceId", "GeneratedArticleId"),
    CONSTRAINT "FK_team_space_articles_generated_articles_GeneratedArticleId" FOREIGN KEY ("GeneratedArticleId") REFERENCES generated_articles ("Id") ON DELETE CASCADE,
    CONSTRAINT "FK_team_space_articles_team_spaces_TeamSpaceId" FOREIGN KEY ("TeamSpaceId") REFERENCES team_spaces ("Id") ON DELETE CASCADE
);

CREATE INDEX "IX_team_space_articles_GeneratedArticleId" ON team_space_articles ("GeneratedArticleId");

CREATE UNIQUE INDEX "IX_team_spaces_Slug" ON team_spaces ("Slug");

INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
VALUES ('20260720125850_AddTeamSpaces', '8.0.11');

COMMIT;