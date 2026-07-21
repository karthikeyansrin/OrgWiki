# OrgWiki

> Transform fragmented organizational documents into trusted, searchable knowledge.

OrgWiki is an AI-assisted organizational knowledge platform. It turns a bounded ZIP archive of PDFs, DOCX files, Markdown, and text documents into a structured knowledge map, citation-backed draft articles, and a human-reviewed knowledge base.

It is not a chat-with-your-files application. OrgWiki refactors the organization’s knowledge before it is published: it discovers domains, identifies duplicate and conflicting guidance, generates grounded articles, and keeps people in control of publication.

## The product story

```text
Source documents
  -> Document ingestion
  -> AI knowledge discovery
  -> Validated knowledge map
  -> AI knowledge generation
  -> Human review
  -> Explicit publishing
  -> Searchable knowledge base and public Team Spaces
```

Published articles may also be curated into **Team Spaces**: public, read-only collections that employees can browse without signing in.

## Key capabilities

- Safe ZIP ingestion for PDF, DOCX, Markdown, and text documents.
- Corpus-level discovery of domains, topics, relationships, duplicates, conflicts, and potentially outdated knowledge.
- Citation-backed Markdown article generation.
- Exact evidence validation against normalized source content.
- Human edit, approve, reject, publish, and republish workflow.
- Private, user-owned workspaces with JWT authentication.
- Published knowledge browsing, keyword filtering, and source evidence.
- Public Team Spaces with scoped search and Markdown downloads.

## Architecture

```text
React + TypeScript (Vite)
  -> ASP.NET Core .NET 8 API
  -> PostgreSQL

API AI provider selection
  -> Replay providers for offline development
  -> OpenAI provider for deliberate Live execution
```

The backend uses Entity Framework Core with PostgreSQL/Npgsql. The frontend uses React, React Router, React Query, Tailwind CSS, and Motion.

## AI design and key technical decisions

### Two controlled GPT-5.6 stages

OrgWiki deliberately avoids per-document, per-topic, or agent-loop AI usage.

1. **Knowledge Discovery** makes exactly one bounded provider call for an eligible upload. It analyzes the complete deterministic corpus and produces a knowledge map.
2. **Knowledge Generation** makes exactly one bounded provider call for an analysis. It generates all proposed articles in a single response.

Review, publishing, search, Team Spaces, and citation display are deterministic application features and make no AI calls.

The configured model is read from `OPENAI_MODEL`; the checked-in default is `gpt-5.6`. Live execution is an explicit opt-in through `OPENAI_MODE=Live` and requires `OPENAI_API_KEY`.

### Why this approach

- **Cost control:** the MVP bounds archive size, document count, normalized corpus size, and output tokens before any Live request.
- **Trust:** discovery and generation responses are validated before persistence. Source document references, cross-references, confidence values, and conflict evidence are checked by the application.
- **Evidence:** conflict and article citations must be exact substrings of normalized source content; fabricated or paraphrased evidence is rejected.
- **Human governance:** generated articles remain Pending Review until a person explicitly approves and publishes them.
- **Safe development:** Replay is the default mode and makes zero OpenAI requests, enabling full workflow and UI development without API spend.

### How Codex accelerated implementation

Codex was used as an implementation partner across the MVP: designing and implementing the phased workflow, focused test coverage, EF Core migration and SQL-script workflow, deterministic Replay support, security hardening, frontend UX polish, and deployment configuration.

Key decisions were made explicitly in code rather than delegated to the model: one provider call per AI stage, no automatic AI retries, strict structured output, exact evidence verification, transactional persistence, manual database migration application, and human approval before publication.

## Prerequisites

- .NET 8 SDK
- Node.js 20 or newer
- PostgreSQL 15+ or a Supabase PostgreSQL database
- A modern browser

Docker is optional for backend deployment. See [backend/Dockerfile](backend/Dockerfile) for the Render-oriented backend image.

## Quick start

### 1. Create a PostgreSQL database and apply the schema

OrgWiki does **not** apply EF migrations at application startup and does **not** use `dotnet ef database update` as its deployment workflow.

For a fresh database, manually review and execute these SQL files in this exact order:

1. [backend/orgwiki-initial-schema.sql](backend/orgwiki-initial-schema.sql)
2. [backend/orgwiki-add-authentication-foundation.sql](backend/orgwiki-add-authentication-foundation.sql)
3. [backend/orgwiki-add-user-owned-workspaces.sql](backend/orgwiki-add-user-owned-workspaces.sql)
4. [backend/orgwiki-add-team-spaces.sql](backend/orgwiki-add-team-spaces.sql)

For Supabase, create the project, open the SQL Editor, confirm the database is empty, review each script, and execute each one once. Verify the matching entries in `__EFMigrationsHistory` before configuring the backend.

### 2. Configure the backend

Set environment variables in your shell, IDE launch profile, or .NET User Secrets. The sample `backend/.env.example` is a reference file; it is not automatically loaded by ASP.NET Core.

PowerShell example:

```powershell
$env:DATABASE_URL = "Host=localhost;Port=5432;Database=orgwiki;Username=postgres;Password=YOUR_PASSWORD"
$env:JWT_SIGNING_KEY = "replace-with-a-random-secret-at-least-32-characters-long"
$env:OPENAI_MODE = "Replay"
$env:OPENAI_MODEL = "gpt-5.6"
$env:CORS__ALLOWEDORIGINS__0 = "http://localhost:5173"
```

Required settings:

| Variable | Purpose |
|---|---|
| `DATABASE_URL` | PostgreSQL/Npgsql connection string. |
| `JWT_SIGNING_KEY` | Random signing secret, at least 32 characters. Never commit it. |
| `OPENAI_MODE` | `Replay` for safe local development, or `Live` for a deliberate model request. |
| `OPENAI_MODEL` | Configured Live model identifier; default configuration is `gpt-5.6`. |
| `OPENAI_API_KEY` | Required only when `OPENAI_MODE=Live`. |
| `CORS__ALLOWEDORIGINS__0` | Frontend origin allowed to call the API. |
| `OPENAI_VERBOSE_LOGGING` | Optional `true` for request metadata and usage diagnostics; defaults to `false`. |

`SUPABASE_URL`, `SUPABASE_KEY`, and `SUPABASE_STORAGE_BUCKET` are reserved configuration placeholders. The current MVP uses direct PostgreSQL through Npgsql and local archive storage; it does not use a Supabase SDK or Supabase Storage client.

Start the API:

```bash
cd backend
dotnet restore
dotnet run --project src/OrgWiki.API
```

The local API health endpoint is available at `http://localhost:5051/health` when using the default launch profile. Swagger is enabled only in Development.

### 3. Configure and start the frontend

```bash
cd frontend
copy .env.example .env.local
npm install
npm run dev
```

On macOS or Linux, use `cp .env.example .env.local` instead of `copy`.

Set `VITE_API_BASE_URL` in `frontend/.env.local` to the backend URL. For local development, the sample value is:

```text
VITE_API_BASE_URL=http://localhost:5051
```

Open the Vite URL shown in the terminal, register an account, and begin importing documents.

## Replay demo data

No ZIP fixture is committed to the repository. Replay works with any small supported archive and never calls OpenAI.

To make a simple local demo archive, create two small files such as:

```text
demo-corpus/
  Engineering/Authentication.txt
  HR/LeavePolicy.md
```

Then create `DemoDocs.zip` from the files. In PowerShell:

```powershell
Compress-Archive -Path .\demo-corpus\* -DestinationPath .\DemoDocs.zip
```

Upload the archive while `OPENAI_MODE=Replay`. The Replay discovery provider maps the first one or two parsed documents into a deterministic fixture-compatible knowledge map, and the Replay generation provider returns deterministic cited drafts. This supports the full flow:

```text
Register -> Upload -> Analyze -> Generate -> Review -> Approve -> Publish -> Knowledge Base -> Team Spaces
```

For Live testing, start with a deliberately small two-document corpus. Use the Retry actions only when intentionally authorizing another provider call.

## Security and ownership

- Passwords use ASP.NET Core `PasswordHasher` and are never stored in plaintext.
- JWT authentication protects upload, analysis, generation, review, publishing, and private knowledge-base routes.
- Uploads are owned by the authenticated user; downstream documents, analyses, generations, citations, and articles inherit ownership through the upload.
- ZIP extraction validates paths, entry limits, file types, archive sizes, extracted sizes, PDF page count, and normalized text limits.
- Markdown is rendered without unsafe HTML injection, and external links are restricted to HTTP(S).
- Public Team Spaces expose only curated, Published article content through read-only routes.

## Deployment notes

- **Backend on Render:** set the Root Directory to `backend`, use `Dockerfile`, internal port `10000`, and health-check path `/health`.
- **Frontend on Vercel:** set `VITE_API_BASE_URL` to the Render API URL. The included `frontend/vercel.json` provides the SPA fallback required for `/spaces` and other direct routes.
- **CORS:** set `CORS__ALLOWEDORIGINS__0` on Render to the deployed Vercel origin.
- **Database:** apply reviewed SQL manually before deploying the backend. Do not enable automatic EF migrations.
- **Archive retention:** Render filesystem storage is ephemeral. Normalized source content persists in PostgreSQL, but durable original archive retention requires future object-storage integration.

## Project structure

```text
OrgWiki/
  frontend/
    src/                  React application
    vercel.json           SPA deployment fallback
  backend/
    src/
      OrgWiki.API/        ASP.NET Core API and configuration
      OrgWiki.Application/ application contracts and use cases
      OrgWiki.Domain/     domain entities
      OrgWiki.Infrastructure/ EF Core, storage, providers, services
    tests/                focused backend tests
    *.sql                 manually reviewed PostgreSQL migration scripts
    Dockerfile            Render-oriented backend image
```

## MVP scope

Included:

- ZIP ingestion and normalization
- One-call knowledge discovery
- One-call article generation
- Exact citation validation
- Human review and explicit publishing
- Private knowledge base, keyword filtering, and source evidence
- Public Team Spaces and Markdown download
- JWT authentication and user-owned workspaces

Intentionally out of scope:

- AI chat, semantic search, embeddings, and vector databases
- Background queues, agent frameworks, or automatic AI retries
- OAuth, RBAC, organizations, and collaboration workflows
- Version history, source connectors, and durable object-storage integration

## License

OrgWiki is a hackathon MVP and portfolio project. Future work can evolve the personal-workspace foundation into organization-based knowledge management.
