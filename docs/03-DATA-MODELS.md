# Data Models - Database Schemas and Entities

Status: Draft
Version: 1.0.0
Last Updated: October 24, 2025

---

## Purpose

Define and document per-service data ownership, schemas, and key entities. Adopt PostgreSQL with separate schemas per service.

## Conventions

- PostgreSQL 16
- snake_case table and column names
- UUID primary keys (v4)
- created_at, updated_at timestamps (timestamptz)
- Foreign keys with ON DELETE CASCADE where appropriate

## Schemas

- auth: users, sessions, api_keys (shared)
- chat: conversations, messages, attachments
- orchestration: tasks, executions, results
- ml: training_samples, ml_models, model_feedback
- github: repositories, pull_requests, issues
- cicd: builds, failure_reports, fixes

## ER Diagrams

TBD. Use dbdiagram.io or PlantUML.

## Migration Strategy

- Migrations per service (EF Core for .NET, Alembic for Python if needed)
- Idempotent seed scripts for dev
- One-way migrations only in prod

## Next Steps

- [ ] Draft initial tables for chat schema
- [ ] Draft initial tables for orchestration schema
- [ ] Define training_samples and ml_models schema
- [ ] Add seed scripts for dev environments
