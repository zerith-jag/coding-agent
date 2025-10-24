# Implementation Roadmap - Microservices Rewrite

**Project Duration**: 6 months (24 weeks)
**Start Date**: October 2025
**Target Completion**: April 2026
**Team Size**: 1 developer + AI assistance (GitHub Copilot)
**Current Phase**: Phase 1 - Week 4 (Infrastructure & Gateway)
**Last Updated**: October 25, 2025

---

## Project Phases Overview

| Phase | Duration | Focus | Deliverable |
|-------|----------|-------|-------------|
| **Phase 0** | 2 weeks | Architecture & Planning | Complete specifications, ADRs, POC |
| **Phase 1** | 4 weeks | Infrastructure & Gateway | Gateway + Auth + Observability |
| **Phase 2** | 6 weeks | Core Services | Chat + Orchestration + ML Classifier |
| **Phase 3** | 4 weeks | Integration Services | GitHub + Browser + CI/CD Monitor |
| **Phase 4** | 4 weeks | Frontend & Dashboard | Angular dashboard + E2E integration |
| **Phase 5** | 2 weeks | Migration & Cutover | Data migration, traffic routing |
| **Phase 6** | 2 weeks | Stabilization & Docs | Bug fixes, documentation, handoff |

---

## Phase 0: Architecture & Planning (Weeks 1-2)

### Goal
Complete architectural specifications and validate technical approach.

### Week 1: Documentation & Design

**Days 1-2: Current System Analysis**

- ✅ Review existing codebase (CodingAgent.Core, Application, Infrastructure)
- ✅ Document feature inventory (what must be migrated)
- ✅ Identify pain points (tight coupling, deployment issues, scalability)
- ✅ Extract reusable patterns (orchestration logic, ML classification)

- **Deliverable**: `SYSTEM-ANALYSIS.md` with feature map and technical debt log

**Days 3-4: Microservice Boundaries**

- ✅ Define 8 microservices with DDD bounded contexts
- ✅ Map current features to new services
- ✅ Design service APIs (OpenAPI specs)
- ✅ Define data ownership per service

- **Deliverable**: `01-SERVICE-CATALOG.md` with detailed specifications

**Day 5: SharedKernel Design**

- ✅ Identify common domain primitives (Task, User, Result)
- ✅ Design shared contracts (DTOs, events, interfaces)
- ✅ Define versioning strategy (semantic versioning)

- **Deliverable**: `CodingAgent.SharedKernel` project structure

### Week 2: Service Scaffolding

**Days 1-5: Initial Service Setup**

- ✅ Create solution structure for all services
- ✅ Setup project templates and conventions
- ✅ Implement SharedKernel base types
- ✅ Setup initial CI/CD workflows

- **Deliverable**: All service projects scaffolded and building

---

## Phase 1: Infrastructure & Gateway (Weeks 3-6)

### Goal
Production-ready infrastructure: Gateway, Auth, Databases, Message Bus, Observability.

### Week 3: Infrastructure Setup

**Days 1-2: Docker Compose Stack**
- [ ] Create `docker-compose.microservices.yml`
- [ ] PostgreSQL with 8 schemas (one per service)
- [ ] Redis cluster (3 nodes for HA)
- [ ] RabbitMQ with management console
- [ ] Prometheus + Grafana + Jaeger
- **Deliverable**: `docker compose up` starts full stack

**Days 3-4: Database Migrations**
- [ ] Setup EF Core migrations per service
- [ ] Create `auth` schema (users, sessions, api_keys)
- [ ] Seed test data (admin user, API keys)
- [ ] Verify cross-service queries work
- **Deliverable**: Database migration scripts in each service

**Day 5: CI/CD Pipeline**
- [ ] GitHub Actions workflow per service
- [ ] Build, test, docker build, push to registry
- [ ] Separate pipelines allow parallel deployment
- **Deliverable**: `.github/workflows/gateway.yml` and similar

### Week 4: API Gateway (Production)

**Days 1-2: Core Gateway**

- ✅ Implement all YARP routes (8 services)
- ✅ Add health check aggregation
- ✅ Configure load balancing (round-robin)
- ✅ Setup circuit breakers and resilience policies (Polly)

- **Deliverable**: Gateway routes all traffic correctly

**Days 3-4: Authentication & Authorization**

- ✅ Implement JWT token generation (`/auth/login`)
- ✅ Add CORS configuration for frontend connectivity
- [ ] Add refresh token support
- [ ] Create API key validation for service-to-service
- [ ] Implement RBAC (Admin, Developer, Viewer roles)

- **Deliverable**: Secured endpoints, 401 on invalid tokens

**Day 5: Rate Limiting & Throttling**

- ✅ Per-user limits (1000 req/hour)
- ✅ Per-IP limits (100 req/min)
- [ ] API key tier-based limits
- ✅ Return `429 Too Many Requests` with `Retry-After`
- ✅ Add rate limit headers to responses (X-RateLimit-*)

- **Deliverable**: Load tests show rate limits enforced

### Week 5-6: Observability

**Days 1-3: OpenTelemetry Integration**

- ✅ Add OTLP exporters to all services
- ✅ Implement correlation ID propagation
- [ ] Configure trace sampling (10% in prod, 100% in dev)
- [ ] Setup Jaeger for distributed tracing

- **Deliverable**: End-to-end traces visible in Jaeger UI

**Days 4-5: Metrics & Dashboards**

- ✅ Instrument custom metrics (task duration, queue depth)
- ✅ Create Grafana dashboards (5 dashboards: system, API, services, database, cache)
- ✅ Configure Prometheus scraping
- [ ] Setup alerting rules (high error rate, high latency)

- **Deliverable**: Real-time metrics visible in Grafana

---

## Phase 2: Core Services (Weeks 7-12)

### Goal
Implement the three most critical services: Chat, Orchestration, ML Classifier.

### Week 7-8: Chat Service

**Days 1-2: Domain Model & Repository**
- [ ] Implement entities (Conversation, Message, Attachment)
- [ ] Create repository pattern with EF Core
- [ ] Add validation (FluentValidation)
- [ ] Write unit tests (85%+ coverage)
- **Deliverable**: `CodingAgent.Services.Chat.Domain` complete

**Days 3-5: REST API**
- [ ] Implement all endpoints (POST /conversations, GET /messages, etc.)
- [ ] Add pagination (page size: 50)
- [ ] Implement search (full-text via PostgreSQL)
- [ ] Write integration tests (Testcontainers)
- **Deliverable**: Chat REST API passing all tests

**Days 6-8: SignalR WebSocket**
- [ ] Implement `/hubs/chat` SignalR hub
- [ ] Add connection authentication (JWT in query string)
- [ ] Implement typing indicators
- [ ] Add presence tracking (online/offline)
- [ ] Write SignalR integration tests
- **Deliverable**: Real-time chat working end-to-end

**Days 9-10: File Upload & Cache**
- [ ] Implement multipart file upload
- [ ] Store files in Azure Blob / S3
- [ ] Cache last 100 messages in Redis
- [ ] Add cache invalidation on new messages
- **Deliverable**: File attachments working, cache hit rate > 80%

### Week 9-10: Orchestration Service

**Days 1-3: Task Domain Model**
- [ ] Implement entities (CodingTask, TaskExecution, ExecutionResult)
- [ ] Create repository pattern
- [ ] Add state machine for TaskStatus transitions
- [ ] Write unit tests for state transitions
- **Deliverable**: Task domain logic complete

**Days 4-6: Execution Strategies**
- [ ] Implement `SingleShotStrategy` (simple tasks)
- [ ] Implement `IterativeStrategy` (medium tasks)
- [ ] Implement `MultiAgentStrategy` (complex tasks)
- [ ] Add strategy selector (based on complexity)
- **Deliverable**: All 3 strategies implemented and tested

**Days 7-9: REST API & Integration**
- [ ] Implement task CRUD endpoints
- [ ] Add SSE endpoint for streaming logs (`GET /tasks/{id}/logs`)
- [ ] Integrate with ML Classifier (REST call)
- [ ] Integrate with GitHub Service (create PR)
- **Deliverable**: Full task lifecycle working

**Day 10: Event Publishing**
- [ ] Publish `TaskCreatedEvent`, `TaskCompletedEvent`, `TaskFailedEvent`
- [ ] Configure MassTransit message bus
- [ ] Add retry logic (3 retries with exponential backoff)
- **Deliverable**: Events published to RabbitMQ

### Week 11-12: ML Classifier Service

**Days 1-2: Python Project Setup**
- [ ] Create FastAPI project structure
- [ ] Setup virtual environment (Poetry)
- [ ] Configure PostgreSQL connection (asyncpg)
- [ ] Add pytest test framework
- **Deliverable**: `ml-classifier-service/` Python project

**Days 3-5: Classification Logic**
- [ ] Implement heuristic classifier (keyword matching)
- [ ] Implement ML classifier (XGBoost model)
- [ ] Add hybrid approach (heuristic → ML fallback)
- [ ] Write unit tests for both classifiers
- **Deliverable**: Classification API returns predictions

**Days 6-7: Model Training**
- [ ] Create training data loader (from PostgreSQL)
- [ ] Implement feature extraction (TF-IDF, code metrics)
- [ ] Train XGBoost model (scikit-learn pipeline)
- [ ] Export model to ONNX format
- **Deliverable**: Trained model with 85%+ accuracy

**Days 8-10: REST API & Integration**
- [ ] Implement `/classify` endpoint
- [ ] Add `/train` endpoint (trigger retraining)
- [ ] Implement event listener for `TaskCompletedEvent` (training data collection)
- [ ] Add model versioning (save models with timestamps)
- **Deliverable**: ML service integrated with Orchestration

---

## Phase 3: Integration Services (Weeks 13-16)

### Goal
Build GitHub, Browser, and CI/CD Monitor services.

### Week 13-14: GitHub Service

**Days 1-3: Octokit Integration**
- [ ] Implement repository connection (OAuth flow)
- [ ] Add repository CRUD operations
- [ ] Implement branch management
- [ ] Write unit tests with mocked Octokit
- **Deliverable**: GitHub repository operations working

**Days 4-6: Pull Request Management**
- [ ] Implement PR creation endpoint
- [ ] Add PR merge/close operations
- [ ] Create PR templates (Markdown)
- [ ] Add automated code review comments
- **Deliverable**: PR lifecycle complete

**Days 7-10: Webhook Handling**
- [ ] Implement `/webhooks/github` endpoint
- [ ] Validate webhook signatures (HMAC)
- [ ] Handle push, PR, issue events
- [ ] Publish domain events to RabbitMQ
- **Deliverable**: Webhooks triggering downstream actions

### Week 15: Browser Service

**Days 1-2: Playwright Setup**
- [ ] Install Playwright browsers (Chromium, Firefox)
- [ ] Implement browser pool (max 5 concurrent)
- [ ] Add navigation endpoint (`POST /browse`)
- **Deliverable**: Basic browsing working

**Days 3-5: Advanced Features**
- [ ] Implement screenshot capture (full page + element)
- [ ] Add content extraction (text, links, images)
- [ ] Implement form interaction (fill, submit)
- [ ] Add PDF generation
- **Deliverable**: All browser features operational

### Week 16: CI/CD Monitor Service

**Days 1-3: GitHub Actions Integration**
- [ ] Poll GitHub Actions API for build status
- [ ] Detect build failures
- [ ] Parse build logs for error messages
- **Deliverable**: Build monitoring working

**Days 4-5: Automated Fix Generation**
- [ ] Integrate with Orchestration service
- [ ] Generate fix task from build error
- [ ] Create PR with fix
- [ ] Track fix success rate
- **Deliverable**: End-to-end automated fix flow

---

## Phase 4: Frontend & Dashboard (Weeks 17-20)

### Goal
Rebuild Angular dashboard with microservices integration.

### Week 17-18: Dashboard Service (BFF)

**Days 1-3: Data Aggregation**
- [ ] Implement `/dashboard/stats` (aggregate from all services)
- [ ] Add `/dashboard/tasks` (enrich with GitHub data)
- [ ] Create `/dashboard/activity` (recent events)
- **Deliverable**: Dashboard API returning aggregated data

**Days 4-5: Caching Strategy**
- [ ] Add Redis caching (5 min TTL)
- [ ] Implement cache invalidation on events
- [ ] Add cache warming on startup
- **Deliverable**: Dashboard API response time < 100ms

### Week 19-20: Angular Dashboard

**Days 1-5: Component Rewrite**
- [ ] Rebuild task list component (calls Dashboard Service)
- [ ] Rebuild chat component (SignalR integration)
- [ ] Add real-time notifications (via SignalR)
- [ ] Create system health dashboard (metrics from Gateway)
- **Deliverable**: Functional Angular dashboard

**Days 6-10: E2E Testing**
- [ ] Write Cypress E2E tests (full user flows)
- [ ] Test chat conversation flow
- [ ] Test task creation → execution → PR flow
- [ ] Test error handling (network failures, 500 errors)
- **Deliverable**: E2E test suite passing

---

## Phase 5: Migration & Cutover (Weeks 21-22)

### Goal
Migrate data from old system and route production traffic to new system.

### Week 21: Data Migration

**Days 1-2: Migration Scripts**
- [ ] Write PostgreSQL migration (old DB → new schemas)
- [ ] Migrate users (1:1 mapping)
- [ ] Migrate conversations (Chat service schema)
- [ ] Migrate tasks (Orchestration service schema)
- **Deliverable**: Migration scripts tested on staging

**Days 3-5: Dual-Write Period**
- [ ] Enable dual-writes (write to both old and new DBs)
- [ ] Verify data consistency
- [ ] Monitor for write errors
- **Deliverable**: Data consistency validated

### Week 22: Traffic Cutover

**Days 1-2: Feature Flags**
- [ ] Add feature flags in Gateway (`UseLegacyChat`, `UseLegacyOrchestration`)
- [ ] Route 10% of traffic to new services
- [ ] Monitor error rates and latency
- **Deliverable**: Partial traffic routing working

**Days 3-4: Full Cutover**
- [ ] Route 100% traffic to new services
- [ ] Disable writes to old DB
- [ ] Monitor for 24 hours
- **Deliverable**: Old system decommissioned

**Day 5: Cleanup**
- [ ] Remove old monolith code
- [ ] Archive old repositories
- [ ] Update documentation
- **Deliverable**: Clean codebase

---

## Phase 6: Stabilization & Documentation (Weeks 23-24)

### Goal
Fix bugs, optimize performance, complete documentation.

### Week 23: Bug Fixes & Optimization

**Days 1-3: Bug Triage**
- [ ] Review production errors (last 7 days)
- [ ] Fix P0 bugs (crashes, data loss)
- [ ] Fix P1 bugs (functional issues)
- **Deliverable**: Zero P0/P1 bugs

**Days 4-5: Performance Optimization**
- [ ] Identify slow endpoints (p95 > 500ms)
- [ ] Add database indexes
- [ ] Optimize N+1 queries
- [ ] Tune cache TTLs
- **Deliverable**: All endpoints < 500ms p95

### Week 24: Documentation & Handoff

**Days 1-2: Architecture Documentation**
- [ ] Finalize all ADRs (Architecture Decision Records)
- [ ] Complete OpenAPI specs (Swagger UI)
- [ ] Write deployment guide (Docker + K8s)
- **Deliverable**: Complete documentation set

**Days 3-4: Runbooks**
- [ ] Write incident response runbooks
- [ ] Document common issues and resolutions
- [ ] Create operational dashboard (Grafana)
- **Deliverable**: Operations manual

**Day 5: Handoff & Retrospective**
- [ ] Conduct project retrospective
- [ ] Document lessons learned
- [ ] Plan next enhancements (Phase 7+)
- **Deliverable**: Project closure report

---

## Risk Management

### High Risks

| Risk | Probability | Impact | Mitigation |
|------|------------|--------|------------|
| **Underestimated Complexity** | Medium | High | Add 20% buffer to each phase |
| **Integration Issues** | High | Medium | POC in Phase 0 validates approach |
| **Data Migration Errors** | Medium | Critical | Dual-write period + rollback plan |
| **Performance Degradation** | Low | High | Load testing in Phase 4 |
| **Scope Creep** | High | Medium | Strict scope definition, Phase 7 for extras |

### Mitigation Strategies

1. **Weekly Progress Reviews**: Adjust timeline if falling behind
2. **Automated Testing**: 85%+ coverage prevents regressions
3. **Feature Flags**: Enable gradual rollout and rollback
4. **Rollback Plan**: Keep old system operational until cutover validated

---

## Success Criteria

### Technical Metrics

- ✅ **Zero-downtime deployment**: Rolling updates without service interruption
- ✅ **API latency**: p95 < 500ms for all endpoints
- ✅ **Test coverage**: 85%+ for all services
- ✅ **Build time**: < 5 minutes per service
- ✅ **Availability**: 99.5%+ uptime

### Business Metrics

- ✅ **Feature velocity**: 2x faster (parallel development)
- ✅ **Deployment frequency**: Daily deployments per service
- ✅ **MTTR**: < 5 minutes (auto-recovery)
- ✅ **Cost reduction**: 30% (independent scaling)

---

## Resource Requirements

### Development Tools

- **IDE**: Visual Studio Code + GitHub Copilot
- **Database**: PostgreSQL 16, Redis 7
- **Message Queue**: RabbitMQ 3.12
- **Monitoring**: Prometheus, Grafana, Jaeger, Seq
- **Testing**: xUnit, pytest, Cypress, k6 (load testing)

### Infrastructure

- **Development**: Local Docker Compose
- **Staging**: Cloud VMs (2 vCPU, 8GB RAM) × 3
- **Production**: Kubernetes cluster (autoscaling)

### Estimated Costs

- **Development**: $0 (local Docker)
- **Staging**: ~$150/month (cloud VMs)
- **Production**: ~$500/month (K8s + managed services)

---

## Next Steps

1. **Review & Approve Roadmap** (End of Week 1)
2. **Start Phase 0** (Week 2)
3. **Weekly Standup**: Monday 9am (progress review)
4. **Monthly Checkpoint**: Review metrics, adjust timeline

---

**Document Owner**: Technical Lead
**Last Updated**: October 25, 2025
**Next Review**: November 1, 2025
