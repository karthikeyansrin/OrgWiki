# OrgWiki

> **Transform fragmented organizational documents into trusted, searchable knowledge.**

OrgWiki is an AI-powered organizational knowledge platform that converts scattered documents into a structured internal knowledge base. Instead of forcing teams to manually organize years of documentation, OrgWiki analyzes, consolidates, and generates high-quality knowledge articles backed by verifiable source citations.

---

# The Problem

Every organization accumulates documentation across multiple places:

* Shared Drives
* Git repositories
* SharePoint
* Confluence
* Notion
* Local folders
* PDFs
* Word documents
* Markdown files

Over time this knowledge becomes:

* Duplicated
* Contradictory
* Outdated
* Difficult to search
* Difficult to trust

Employees often spend more time searching for information than actually using it.

---

# The Solution

OrgWiki transforms fragmented documentation into a trusted internal knowledge base using AI.

Instead of simply indexing files, OrgWiki understands organizational knowledge, detects duplication and conflicts, generates structured articles, and keeps every generated statement traceable back to its original source.

Every AI-generated article goes through a human review workflow before publication.

---

# Demo Workflow

```text
Upload ZIP
      │
      ▼
Document Parsing
      │
      ▼
AI Knowledge Discovery
      │
      ▼
Duplicate Detection
      │
      ▼
Conflict Detection
      │
      ▼
Knowledge Article Generation
      │
      ▼
Human Review
      │
      ▼
Publish
      │
      ▼
Searchable Knowledge Base
```

---

# Key Features

## AI Knowledge Discovery

Analyze an entire document collection in a single AI request.

Automatically identifies:

* Knowledge domains
* Topics
* Relationships
* Duplicate knowledge
* Conflicting information
* Potentially outdated documentation

---

## AI Knowledge Generation

Generate structured wiki articles from discovered knowledge.

Each generated article includes:

* Title
* Summary
* Rich Markdown content
* Tags
* Reading time
* Difficulty
* Related articles
* Confidence score

---

## Evidence-backed Citations

Every generated statement is backed by source evidence.

Each citation contains:

* Original document
* Supporting text snippet

This allows reviewers to verify AI-generated content before publishing.

---

## Human Review Workflow

Generated articles are never published automatically.

Reviewers can:

* Edit
* Approve
* Reject
* Publish

AI assists.

Humans remain in control.

---

## Searchable Knowledge Base

Published articles become part of an internal knowledge base with:

* Keyword search
* Related articles
* Markdown rendering
* Source evidence

---

# Architecture

```text
                React + TypeScript
                        │
                        ▼
                .NET 9 Web API
                        │
        ┌───────────────┴───────────────┐
        │                               │
        ▼                               ▼
Replay AI Provider             OpenAI GPT-5.6
        │                               │
        └───────────────┬───────────────┘
                        ▼
                Knowledge Pipeline
                        │
                        ▼
             PostgreSQL (Supabase Hosted)
```

---

# AI Pipeline

## 1. Upload

Upload a ZIP containing:

* PDF
* DOCX
* Markdown
* TXT

---

## 2. Knowledge Discovery

AI performs a single corpus-level analysis.

It identifies:

* Domains
* Topics
* Duplicate knowledge
* Conflicts
* Relationships
* Suggested knowledge articles

---

## 3. Knowledge Generation

AI generates structured wiki articles from discovered knowledge.

Each article contains verified citations.

---

## 4. Review

Human reviewers validate AI output.

Only approved articles may be published.

---

## 5. Publish

Published articles become searchable organizational knowledge.

---

# Security

OrgWiki follows a backend-first architecture.

```
Browser
    │
    ▼
.NET API
    │
    ▼
PostgreSQL
```

Current implementation includes:

* JWT Authentication
* Password hashing
* User-owned workspaces
* Backend authorization
* Environment-based secret management

Each authenticated user can access only their own uploaded documents and generated knowledge.

---

# Technology Stack

## Frontend

* React
* TypeScript
* React Query
* Tailwind CSS

## Backend

* .NET 8
* ASP.NET Core Web API
* Entity Framework Core

## Database

* PostgreSQL
* Supabase (Database Hosting)

## AI

* OpenAI GPT-5.6
* Replay Provider (offline testing)

## Authentication

* JWT
* ASP.NET Core Password Hasher

---

# Getting Started

## Prerequisites

* .NET 8 SDK
* Node.js
* PostgreSQL (or Supabase PostgreSQL)

---

## Backend

```bash
cd backend
dotnet restore
dotnet run
```

---

## Frontend

```bash
cd frontend
npm install
npm run dev
```

---

# Required Environment Variables

```text
DATABASE_URL=

JWT_SIGNING_KEY=

OPENAI_API_KEY=

OPENAI_MODE=Replay

OPENAI_MODEL=gpt-5.6-luna
```

Replay mode is the default and allows end-to-end testing without consuming OpenAI API credits.

---

# Project Structure

```text
OrgWiki
│
├── frontend
│   ├── pages
│   ├── components
│   ├── hooks
│   └── services
│
├── backend
│   ├── API
│   ├── Application
│   ├── Domain
│   ├── Infrastructure
│   └── Persistence
│
└── docs
```

---

# Current MVP Scope

* ZIP upload
* Document parsing
* AI knowledge discovery
* Duplicate detection
* Conflict detection
* Knowledge article generation
* Citation validation
* Review workflow
* Publishing
* Searchable knowledge base
* JWT authentication
* User workspace isolation

---

# Roadmap

## Near Term

* Rich dashboard
* Better search experience
* Streaming AI progress
* Syntax highlighting
* Version history

## Enterprise

* Organization workspaces
* Role-based access control
* SharePoint connector
* Google Drive connector
* Confluence connector
* Incremental synchronization
* Knowledge graph visualization
* Local LLM support
* Air-gapped deployment

---

# Why OrgWiki?

Most documentation platforms expect users to organize knowledge manually.

Most AI search tools retrieve information from existing documents.

**OrgWiki takes a different approach.**

Instead of searching fragmented documentation, it transforms organizational knowledge into a trusted, structured, reviewable knowledge base with AI-assisted discovery and human verification.

---

# License

This project was built as a hackathon MVP and portfolio project.

Future development will focus on enterprise knowledge management, AI-assisted documentation modernization, and secure on-premise deployments.