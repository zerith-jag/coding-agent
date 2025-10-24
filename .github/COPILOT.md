# GitHub Copilot Instructions for This Repository

This guide explains how to use GitHub Copilot (Chat and Autocomplete) effectively and safely in this monorepo.

## Repo context (what Copilot should assume)

- Architecture: Microservices with an API Gateway (YARP)
- Backends: .NET 9 Minimal APIs (C# 13) for core services
- ML Service: Python FastAPI + Pydantic + asyncio
- Frontend: Angular 20.3 (standalone components) + NgRx Signal Store
- Messaging: RabbitMQ + MassTransit
- Data: PostgreSQL (per service schema), Redis cache
- Observability: OpenTelemetry → Prometheus + Grafana + Jaeger; Serilog → Seq

## Guardrails (always)

- Do not include secrets, tokens, or private keys in code or docs.
- Respect licenses; avoid copying large blocks from external sources.
- Keep changes minimal and focused; prefer small PRs.
- Use Conventional Commits (e.g., `feat(chat): add typing indicator`).
- Update or add tests for any user-visible behavior change.
- Update related docs (README, ADRs, API specs, runbooks) in the same PR.

## Service-by-service patterns

### .NET services (Gateway, Chat, Orchestration, GitHub, Browser, CI/CD, Dashboard)

- Minimal APIs, DI-centric, async/await, cancellation tokens.
- Data access via EF Core; no raw SQL unless justified.
- Add Polly retry/circuit breaker for outbound HTTP.
- Add OpenTelemetry instrumentation (HTTP, EF, custom spans) and Prometheus metrics.
- Model validation with FluentValidation.
- Unit tests (xUnit + FluentAssertions), integration tests (Testcontainers), contract tests when applicable.

Example endpoint skeleton:

```csharp
app.MapPost("/tasks", async (CreateTaskRequest request, ITaskOrchestrator orchestrator, CancellationToken ct) =>
{
    var result = await orchestrator.CreateAsync(request, ct);
    return Results.Created($"/tasks/{result.Id}", result);
})
.WithName("CreateTask")
.Produces<TaskResponse>(StatusCodes.Status201Created)
.ProducesProblem(StatusCodes.Status400BadRequest);
```

### Python ML Classifier (FastAPI)

- Use Pydantic models for request/response; type hints everywhere.
- Async endpoints; httpx for outbound calls; uvicorn for app runner.
- Tests: pytest + pytest-asyncio; format with black; lint with ruff.
- Keep model files separate (e.g., `infrastructure/ml/`).

Example route:

```python
from fastapi import APIRouter
from pydantic import BaseModel

router = APIRouter()

class ClassificationRequest(BaseModel):
    task_description: str
    context: dict[str, str] | None = None

class ClassificationResult(BaseModel):
    task_type: str
    complexity: str
    confidence: float

@router.post("/classify", response_model=ClassificationResult)
async def classify(req: ClassificationRequest) -> ClassificationResult:
    # call heuristic → ml pipeline
    return ClassificationResult(task_type="feature", complexity="medium", confidence=0.92)
```

### Angular (Dashboard)

- Standalone components; strict mode; typed HTTP; SignalR client for realtime.
- State with NgRx Signal Store or signals; keep services pure and testable.
- ESLint and Prettier consistent formatting.

## File placement hints

- Gateways/proxies: `src/Gateway/`
- Service code: `src/Services/<ServiceName>/...`
- ML service: `src/Services/MLClassifier/ml_classifier_service/...`
- Frontend: `src/Frontend/coding-agent-dashboard/...`
- Docs: `docs/` (follow `docs/STYLEGUIDE.md`)

## Tests first, please

- Add unit tests for domain logic, validators, and simple endpoints.
- Add integration tests using Testcontainers for DB/Redis/RabbitMQ.
- For cross-service contracts, introduce contract tests.
- Target ≥ 85% coverage in CI (per roadmap).

## Observability requirements

- Add OpenTelemetry spans/metrics for major operations.
- Propagate `X-Correlation-Id` headers; include in logs.
- Include health and readiness endpoints.

## Prompts that work well

- "Propose a plan to add `endpoint` to `service` and list files to edit; include tests."
- "Generate a Minimal API endpoint to do X with EF Core and FluentValidation."
- "Write a Testcontainers-based integration test for Y using PostgreSQL + Redis."
- "Add OpenTelemetry spans around Z; show how to record duration and attributes."
- "Create a FastAPI route with Pydantic models and pytest for classification logic."

## PR checklist for Copilot suggestions

- Code compiles; lint passes (C#, Python, Angular).
- Tests added and passing locally.
- Docs updated where relevant.
- Follows repo structure and naming conventions.
- No secrets or hardcoded tokens; configuration via env/Secrets.

## Security & compliance

- Never print or commit secrets.
- Validate and sanitize inputs.
- Use `IOptions<>`/env vars for configuration; for prod, prefer Key Vault/Secrets Manager.

## When in doubt

- Prefer clarity over cleverness.
- Add small incremental changes.
- Leave comments with rationale for non-obvious decisions.
