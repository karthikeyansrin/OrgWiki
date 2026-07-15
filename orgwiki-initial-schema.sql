CREATE TABLE IF NOT EXISTS "__EFMigrationsHistory" (
    "MigrationId" character varying(150) NOT NULL,
    "ProductVersion" character varying(32) NOT NULL,
    CONSTRAINT "PK___EFMigrationsHistory" PRIMARY KEY ("MigrationId")
);

START TRANSACTION;

CREATE TABLE uploads (
    "Id" uuid NOT NULL,
    "OriginalFileName" character varying(255) NOT NULL,
    "StorageKey" character varying(512) NOT NULL,
    "Status" character varying(32) NOT NULL,
    "TotalFiles" integer NOT NULL,
    "SupportedFiles" integer NOT NULL,
    "FailedFiles" integer NOT NULL,
    "TotalCharacterCount" integer NOT NULL,
    "IsEligibleForAnalysis" boolean NOT NULL,
    "AnalysisEligibilityReason" text,
    "CreatedAtUtc" timestamp with time zone NOT NULL,
    "CompletedAtUtc" timestamp with time zone,
    CONSTRAINT "PK_uploads" PRIMARY KEY ("Id")
);

CREATE TABLE documents (
    "Id" uuid NOT NULL,
    "UploadId" uuid NOT NULL,
    "FileName" character varying(255) NOT NULL,
    "OriginalPath" character varying(1024) NOT NULL,
    "FileExtension" character varying(16) NOT NULL,
    "DocumentType" character varying(32) NOT NULL,
    "Content" text,
    "CharacterCount" integer NOT NULL,
    "WordCount" integer NOT NULL,
    "ProcessingStatus" character varying(32) NOT NULL,
    "ProcessingError" text,
    "CreatedAtUtc" timestamp with time zone NOT NULL,
    CONSTRAINT "PK_documents" PRIMARY KEY ("Id"),
    CONSTRAINT "FK_documents_uploads_UploadId" FOREIGN KEY ("UploadId") REFERENCES uploads ("Id") ON DELETE CASCADE
);

CREATE TABLE knowledge_analyses (
    "Id" uuid NOT NULL,
    "UploadId" uuid NOT NULL,
    "Status" character varying(32) NOT NULL,
    "AiMode" character varying(16) NOT NULL,
    "Model" character varying(128) NOT NULL,
    "StartedAtUtc" timestamp with time zone NOT NULL,
    "CompletedAtUtc" timestamp with time zone,
    "InputTokens" integer,
    "OutputTokens" integer,
    "TotalTokens" integer,
    "DurationMilliseconds" bigint,
    "ErrorMessage" text,
    "ResultJson" text,
    "IsCurrent" boolean NOT NULL,
    "CreatedAtUtc" timestamp with time zone NOT NULL,
    CONSTRAINT "PK_knowledge_analyses" PRIMARY KEY ("Id"),
    CONSTRAINT "FK_knowledge_analyses_uploads_UploadId" FOREIGN KEY ("UploadId") REFERENCES uploads ("Id") ON DELETE CASCADE
);

CREATE TABLE knowledge_generations (
    "Id" uuid NOT NULL,
    "AnalysisId" uuid NOT NULL,
    "Status" character varying(32) NOT NULL,
    "AiMode" character varying(16) NOT NULL,
    "Model" character varying(128) NOT NULL,
    "StartedAtUtc" timestamp with time zone NOT NULL,
    "CompletedAtUtc" timestamp with time zone,
    "InputTokens" integer,
    "OutputTokens" integer,
    "TotalTokens" integer,
    "DurationMilliseconds" bigint,
    "ErrorMessage" text,
    "ResultJson" text,
    "IsCurrent" boolean NOT NULL,
    "CreatedAtUtc" timestamp with time zone NOT NULL,
    CONSTRAINT "PK_knowledge_generations" PRIMARY KEY ("Id"),
    CONSTRAINT "FK_knowledge_generations_knowledge_analyses_AnalysisId" FOREIGN KEY ("AnalysisId") REFERENCES knowledge_analyses ("Id") ON DELETE CASCADE
);

CREATE TABLE generated_articles (
    "Id" uuid NOT NULL,
    "GenerationId" uuid NOT NULL,
    "Key" character varying(128) NOT NULL,
    "Title" character varying(512) NOT NULL,
    "Summary" text NOT NULL,
    "MarkdownContent" text NOT NULL,
    "Difficulty" character varying(32) NOT NULL,
    "EstimatedReadingMinutes" integer NOT NULL,
    "TagsJson" text NOT NULL,
    "RelatedArticleKeysJson" text NOT NULL,
    "Confidence" double precision NOT NULL,
    "Status" character varying(32) NOT NULL,
    "GeneratedAtUtc" timestamp with time zone NOT NULL,
    "ReviewedBy" character varying(128),
    "ReviewedAtUtc" timestamp with time zone,
    "ReviewNotes" text,
    "LastEditedAtUtc" timestamp with time zone,
    "LastEditedBy" character varying(128),
    "PublishedAtUtc" timestamp with time zone,
    "PublishedBy" character varying(128),
    CONSTRAINT "PK_generated_articles" PRIMARY KEY ("Id"),
    CONSTRAINT "FK_generated_articles_knowledge_generations_GenerationId" FOREIGN KEY ("GenerationId") REFERENCES knowledge_generations ("Id") ON DELETE CASCADE
);

CREATE TABLE generated_article_citations (
    "Id" uuid NOT NULL,
    "GeneratedArticleId" uuid NOT NULL,
    "SourceDocumentId" uuid NOT NULL,
    "EvidenceSnippet" text NOT NULL,
    CONSTRAINT "PK_generated_article_citations" PRIMARY KEY ("Id"),
    CONSTRAINT "FK_generated_article_citations_documents_SourceDocumentId" FOREIGN KEY ("SourceDocumentId") REFERENCES documents ("Id") ON DELETE RESTRICT,
    CONSTRAINT "FK_generated_article_citations_generated_articles_GeneratedArt~" FOREIGN KEY ("GeneratedArticleId") REFERENCES generated_articles ("Id") ON DELETE CASCADE
);

CREATE INDEX "IX_documents_UploadId" ON documents ("UploadId");

CREATE INDEX "IX_generated_article_citations_GeneratedArticleId" ON generated_article_citations ("GeneratedArticleId");

CREATE INDEX "IX_generated_article_citations_SourceDocumentId" ON generated_article_citations ("SourceDocumentId");

CREATE UNIQUE INDEX "IX_generated_articles_GenerationId_Key" ON generated_articles ("GenerationId", "Key");

CREATE UNIQUE INDEX "IX_generated_articles_Key" ON generated_articles ("Key") WHERE "Status" = 'Published';

CREATE UNIQUE INDEX "IX_knowledge_analyses_UploadId" ON knowledge_analyses ("UploadId") WHERE "IsCurrent" = TRUE;

CREATE INDEX "IX_knowledge_analyses_UploadId_Status" ON knowledge_analyses ("UploadId", "Status");

CREATE UNIQUE INDEX "IX_knowledge_generations_AnalysisId" ON knowledge_generations ("AnalysisId") WHERE "IsCurrent" = TRUE;

CREATE INDEX "IX_uploads_CreatedAtUtc" ON uploads ("CreatedAtUtc");

CREATE INDEX "IX_uploads_Status" ON uploads ("Status");

INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
VALUES ('20260715142812_InitialOrgWikiSchema', '8.0.11');

COMMIT;

