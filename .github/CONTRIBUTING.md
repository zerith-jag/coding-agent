# Contributing Guide

Thanks for your interest in contributing! This project is a microservices-based rewrite of the Coding Agent and welcomes contributions across docs, services, CI/CD, and tooling.

By participating in this project, you agree to follow the Code of Conduct (see CODE_OF_CONDUCT.md).

## Before You Start

1. Read the docs in `docs/`:
   - `README.md` → Architecture summary
   - `00-OVERVIEW.md` → High-level design
   - `01-SERVICE-CATALOG.md` → Service specs
   - `02-IMPLEMENTATION-ROADMAP.md` → Timeline and milestones
   - `03-SOLUTION-STRUCTURE.md` → Monorepo layout & CI/CD
2. For documentation changes, follow `docs/STYLEGUIDE.md`.
3. For security issues, do NOT open a public issue; see `SECURITY.md`.

## Development Workflow

- Fork the repo and create a feature branch from `main`.
- Prefer small, focused PRs (keep < 500 lines changed when possible).
- Add/Update tests with code changes (unit/integration/contract/E2E as applicable).
- Update documentation (README, ADRs, API specs, runbooks) if behavior changes.
- Ensure CI passes (lint, tests, link checks).

### Branch Naming

Use descriptive, kebab-case prefixes:

- `feat/<area>-<short-description>`
- `fix/<area>-<short-description>`
- `docs/<topic>-<short-description>`
- `chore/<area>-<short-description>`

Examples:

- `feat/chat-typing-indicator`
- `fix/gateway-rate-limit-config`
- `docs/add-adr-for-ml-pipeline`

### Commit Messages (Conventional Commits)

Follow Conventional Commits for clarity and automated changelogs:

```text
<type>(<scope>): <subject>

<body>  # optional

<footer> # optional (breaking changes, closes #123)
```

Types: `feat`, `fix`, `docs`, `refactor`, `test`, `chore`, `build`, `ci`.

Examples:

- `feat(chat): add typing indicator`
- `fix(gateway): correct JWT authority URL`
- `docs(style): define headings and link rules`

### Pull Requests

- Fill out the PR template completely.
- Link related issues (e.g., `Fixes #123`).
- Include screenshots/logs for UI/observability changes when helpful.
- Request review from a maintainer when ready.

## Code Style & Quality

- .NET: C# 13, nullable enabled, async/await best practices, DI-centric.
- Python: Pydantic models, type hints, black/ruff (PEP8), FastAPI for APIs.
- Angular: Standalone components, strict mode, ESLint configured, Nx-friendly structure.
- Testing: Aim for 85%+ coverage, Testcontainers for integration tests.
- Observability: Add OpenTelemetry spans/metrics for significant operations.

## Documentation Contributions

- Follow `docs/STYLEGUIDE.md`.
- Keep headings, TOC, and front-matter (Status/Version/Last Updated) consistent.
- Prefer relative links within the repo.
- Use diagrams where valuable (PlantUML/Mermaid encouraged).

## Issue Triage

- `bug`: Reproducible defect; include steps and environment.
- `enhancement`: New feature or improvement.
- `question`: Non-actionable inquiry; may be converted to discussion.
- `security`: Do not create public issues; see `SECURITY.md`.

## License & CLA

By contributing, you agree your contributions will be licensed under the repository’s license.
We do not currently require a CLA.

## Contact

For questions, open a GitHub issue with the `question` label. For security matters, follow `SECURITY.md`.
