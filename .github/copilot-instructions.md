# Copilot Chat Directives (Repository-Specific)

These concise rules help Copilot Chat generate correct, repo-aligned changes.

## Always follow

- Use the existing architecture:
  - .NET 9 Minimal APIs for services; Python FastAPI for ML; Angular 20.3 for frontend
  - RabbitMQ + MassTransit; PostgreSQL schemas per service; Redis
- Prefer small, focused changes; keep public APIs stable unless requested.
- Add/Update tests (unit/integration/contract) when changing behavior.
- Update docs in the same PR (README, ADRs, API specs, runbooks) if behavior changes.
- Use Conventional Commits for messages (e.g., `feat(orchestration): support SSE logs`).
- Respect security: never include secrets; use env/config; sanitize inputs.

## Code style & placement

- .NET: C# 13, async/await, DI, EF Core, FluentValidation, Polly for HTTP resilience.
- Python: FastAPI, Pydantic, type hints, pytest/pytest-asyncio, black/ruff.
- Angular: standalone components, strict mode, ESLint; service-backed API calls.
- Place code under:
  - `src/Gateway/` (YARP gateway)
  - `src/Services/<ServiceName>/...`
  - `src/Services/MLClassifier/ml_classifier_service/...`
  - `src/Frontend/coding-agent-dashboard/...`
  - Docs under `docs/` (follow `docs/STYLEGUIDE.md`)

## Observability & ops

- Instrument with OpenTelemetry (HTTP, EF, custom spans); expose metrics.
- Propagate `X-Correlation-Id`; structured logging to Seq.
- Provide health/readiness endpoints.

## Testing

- Aim ≥ 85% coverage; use Testcontainers for DB/Redis/RabbitMQ integration tests.
- For cross-service interactions, prefer contract tests.

## Prompts to use

- "List the files to change and a step-by-step plan before editing."
- "Add a Minimal API endpoint for X with validation and tests."
- "Create a FastAPI route and pytest for Y."
- "Add OpenTelemetry spans and Prometheus metrics around Z."

## Prompts to avoid

- Vague or multi-goal prompts; split into small tasks.
- Requests that copy external copyrighted code.

## Output format

- Provide diff-ready changes and explain choices briefly.
- Use proper fenced code blocks with languages (csharp, python, typescript, powershell, yaml).
- Keep commands in PowerShell fences for Windows (`pwsh`).

## Final checks

- Lint passes (markdownlint, ESLint/Prettier, ruff/black, .NET analyzers).
- Tests pass locally; CI jobs succeed.
- Docs updated and cross-linked.
