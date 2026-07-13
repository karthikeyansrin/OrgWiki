# OrgWiki

OrgWiki transforms fragmented organizational documents into structured, reviewed knowledge.

## Prerequisites

- .NET SDK 8.0
- Node.js 24 or later
- PostgreSQL or a Supabase PostgreSQL connection for future persistence work

## Local startup

1. Copy `backend/.env.example` to `backend/.env` and add the service values available to you. The backend also reads these variables from your shell or deployment environment.
2. Run the API from `backend/src/OrgWiki.API` with `dotnet run`. Swagger is available at `http://localhost:5051/swagger` and health at `http://localhost:5051/health`.
3. Run the frontend from `frontend` with `npm install` followed by `npm run dev`. It is served at `http://localhost:5173`.

The frontend defaults to `http://localhost:5051` for its API. Override this using `VITE_API_BASE_URL` in `frontend/.env.local` when required.

## Configuration

- `OPENAI_API_KEY`
- `DATABASE_URL`
- `SUPABASE_URL`
- `SUPABASE_KEY`
- `SUPABASE_STORAGE_BUCKET`
