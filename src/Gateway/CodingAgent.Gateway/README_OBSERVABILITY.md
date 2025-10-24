# Gateway Observability

This service exposes structured logs, distributed tracing, metrics, and health endpoints.

## Logging

- Serilog is enabled with console sink and enrichers for environment and thread id.
- Request logging middleware is enabled.
- Each request is enriched with a `CorrelationId` property.

Configuration (optional):

- Provide `Serilog` section in `appsettings.json` to customize sinks (e.g., Seq).

## Tracing (OpenTelemetry)

- Tracing instrumentation: ASP.NET Core and HttpClient.
- OTLP exporter is enabled. If `OpenTelemetry:Endpoint` is provided, it will be used; otherwise defaults apply (e.g., `http://localhost:4317`).
- Resource attributes include `service.name = CodingAgent.Gateway`.

## Metrics (Prometheus)

- Metrics instrumentation: ASP.NET Core and HttpClient.
- Prometheus scraping endpoint is exposed at `/metrics`.

## Correlation ID

- Header `X-Correlation-Id` is read from the incoming request or generated if missing.
- The same value is returned in the response headers and added to the current Activity and Serilog logging scope as `CorrelationId`.

## Health Endpoints

- `/health`       → overall health in UI client JSON format
- `/health/live`  → liveness (self-check)
- `/health/ready` → readiness (currently same as overall; extend with external dependencies as they are added)

## Notes

- Downstream dependency checks should be added to readiness as the Gateway integrates with more systems.
- Ensure your OpenTelemetry collector is running if you expect traces to be exported via OTLP.
