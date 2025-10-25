# Ollama Service Architecture Decision Record (ADR)

**Version**: 1.0.0
**Last Updated**: October 25, 2025
**Status**: Approved for v2.0 Microservices Architecture
**Decision Maker**: Technical Lead

---

## Table of Contents

1. [Executive Summary](#executive-summary)
2. [Context and Problem Statement](#context-and-problem-statement)
3. [Decision Drivers](#decision-drivers)
4. [Considered Options](#considered-options)
5. [Decision Outcome](#decision-outcome)
6. [Ollama Service Architecture](#ollama-service-architecture)
7. [Integration Patterns](#integration-patterns)
8. [Cost-Benefit Analysis](#cost-benefit-analysis)
9. [Implementation Strategy](#implementation-strategy)
10. [Risks and Mitigations](#risks-and-mitigations)
11. [Future Considerations](#future-considerations)

---

## Executive Summary

### The Decision

**We will implement a dedicated Ollama Service** as the 9th microservice in the Coding Agent v2.0 architecture to provide cost-effective, on-premise LLM inference using locally-hosted open-source models.

### Key Benefits

- **90% Cost Reduction**: $0 inference cost vs $0.50-$2.00 per 1M tokens for cloud APIs
- **Data Privacy**: All inference happens on-premise, no data leaves infrastructure
- **Low Latency**: 5-10s inference vs 10-30s for cloud APIs (network latency eliminated)
- **Offline Support**: Works without internet connectivity
- **Compliance**: Meets strict data residency requirements (GDPR, HIPAA)

### Strategic Rationale

The Ollama Service enables a **hybrid LLM strategy**: use local models for simple/medium complexity tasks (85% of traffic), reserve expensive cloud APIs (OpenAI, Anthropic) for complex/critical tasks (15% of traffic). This balances cost, quality, and privacy.

---

## Context and Problem Statement

### Current State (Without Ollama Service)

The v2.0 architecture relies exclusively on **external cloud LLM APIs**:

```csharp
// Orchestration Service - Current Implementation
public async Task<string> GenerateCodeAsync(string prompt)
{
    var response = await _openAiClient.CreateCompletionAsync(new OpenAIRequest
    {
        Model = "gpt-4o",
        Prompt = prompt,
        MaxTokens = 2000
    });

    // Cost: $2.50 per 1M tokens (~$0.005 per request)
    return response.Content;
}
```

### Problems with Cloud-Only Approach

1. **High Operational Costs**:
   - 1,000 tasks/day × $0.005/task = **$150/month**
   - Scales linearly with usage → unsustainable for high-volume scenarios

2. **Data Privacy Concerns**:
   - Proprietary codebases sent to external APIs
   - Potential compliance violations (GDPR, HIPAA, SOC 2)
   - Audit trail complexity

3. **Network Dependency**:
   - Requires stable internet connection
   - 200-500ms network latency overhead
   - Service degradation during outages

4. **Rate Limiting**:
   - Cloud providers enforce strict rate limits (60 req/min for GPT-4)
   - Throttling during peak usage

5. **Vendor Lock-in**:
   - Tight coupling to OpenAI/Anthropic APIs
   - Migration costs if switching providers

### Desired State (With Ollama Service)

**Hybrid LLM Infrastructure**:

```
┌─────────────────────────────────────────────────────┐
│          Orchestration Service Router               │
│                                                     │
│  if (task.Complexity == Simple)                     │
│      → Ollama Service (local, fast, free)          │
│  else if (task.Complexity == Medium)                │
│      → Ollama Service OR Cloud (quality threshold)  │
│  else                                               │
│      → Cloud API (OpenAI GPT-4, Anthropic Claude)  │
└─────────────────────────────────────────────────────┘
```

**Outcome**:
- 85% of requests → Ollama (local)
- 15% of requests → Cloud APIs
- **Cost Reduction**: $150/month → $22.50/month (85% savings)

---

## Decision Drivers

### Functional Requirements

1. **F1**: Support code generation for simple/medium complexity tasks (bug fixes, small features)
2. **F2**: Provide streaming inference for real-time user feedback
3. **F3**: Enable model selection based on task type (code generation, chat, review)
4. **F4**: Cache deterministic prompts to improve latency
5. **F5**: Track usage metrics for cost analysis and model performance

### Non-Functional Requirements

1. **NFR1**: Inference latency < 10s for simple tasks (p95)
2. **NFR2**: 99.5% availability (same as other services)
3. **NFR3**: Support GPU acceleration for faster inference
4. **NFR4**: Hot-swappable models without service restart
5. **NFR5**: Seamless fallback to cloud APIs on failure (only if configured with available tokens)
6. **NFR6**: Cloud API fallback must verify configuration and token availability before use

### Business Constraints

1. **BC1**: Zero additional licensing costs (open-source models only)
2. **BC2**: Infrastructure costs < $100/month (GPU instance)
3. **BC3**: Compliance with data privacy regulations
4. **BC4**: No degradation in output quality for simple tasks

### Technical Constraints

1. **TC1**: Must integrate with existing .NET 9 microservices architecture
2. **TC2**: Must use Docker for deployment (local + K8s)
3. **TC3**: Must support model versioning and A/B testing
4. **TC4**: Must expose Ollama-compatible API for external tools (VS Code, Continue)

---

## Considered Options

### Option 1: Integrate Ollama Directly into Orchestration Service

**Architecture**:
```
Orchestration Service
├── Domain/
├── Application/
├── Infrastructure/
│   └── LLM/
│       ├── OpenAIClient.cs
│       └── OllamaClient.cs ← Add here
└── Api/
```

**Pros**:
- ✅ Simpler architecture (no additional service)
- ✅ Lower operational overhead
- ✅ Direct access to Ollama backend

**Cons**:
- ❌ Tight coupling (Orchestration depends on Ollama)
- ❌ Cannot scale Ollama independently
- ❌ Difficult to reuse Ollama for other services (e.g., Chat, Dashboard)
- ❌ No isolation if Ollama crashes (takes down Orchestration)

**Decision**: **Rejected** - Violates microservices principles (single responsibility, independent scaling)

---

### Option 2: Use Third-Party LLM Gateway (LiteLLM, Portkey)

**Architecture**:
```
Orchestration Service → LiteLLM Gateway → Ollama Backend
                                        → OpenAI API
                                        → Anthropic API
```

**Pros**:
- ✅ Unified API for all LLM providers
- ✅ Built-in load balancing and fallback
- ✅ Observability out-of-the-box

**Cons**:
- ❌ External dependency (another moving part)
- ❌ Limited customization (e.g., prompt caching, model routing logic)
- ❌ Potential vendor lock-in (LiteLLM)
- ❌ Additional latency (extra network hop)

**Decision**: **Rejected** - Loss of control and flexibility; adds complexity

---

### Option 3: Dedicated Ollama Service (Recommended)

**Architecture**:
```
Orchestration Service → Ollama Service (.NET 9) → Ollama Backend (Docker)
                     ↓
                  Redis Cache (prompt caching)
                     ↓
                  PostgreSQL (usage tracking, model metadata)
```

**Pros**:
- ✅ **Single Responsibility**: Service dedicated to LLM inference
- ✅ **Independent Scaling**: Scale Ollama Service based on inference load
- ✅ **Reusability**: Chat, Dashboard, and other services can consume Ollama
- ✅ **Fault Isolation**: Ollama failures don't cascade to Orchestration
- ✅ **Custom Logic**: Implement prompt optimization, intelligent routing, caching
- ✅ **Observability**: Dedicated metrics, traces, and logs for LLM operations
- ✅ **Cost Control**: Fine-grained cost tracking per model/user/task

**Cons**:
- ❌ Additional service to maintain
- ❌ Slightly higher latency (one extra network hop: ~5ms)

**Decision**: **✅ ACCEPTED** - Aligns with microservices best practices

---

## Decision Outcome

### Chosen Option: **Option 3 - Dedicated Ollama Service**

### Rationale

1. **Microservices Alignment**: Follows single responsibility principle; clear bounded context
2. **Scalability**: Ollama Service can be scaled independently (e.g., add GPU nodes)
3. **Reusability**: Multiple services can leverage Ollama without duplicating logic
4. **Flexibility**: Enables advanced features (prompt caching, model A/B testing, cost tracking)
5. **Fault Tolerance**: Circuit breaker pattern ensures graceful degradation

### Positive Consequences

- **Cost Savings**: 85% reduction in LLM operational costs
- **Data Privacy**: Sensitive code never leaves on-premise infrastructure
- **Performance**: Lower latency for simple tasks (5s vs 10s)
- **Autonomy**: No dependency on external API rate limits or availability
- **Compliance**: Meets GDPR, HIPAA, SOC 2 requirements

### Negative Consequences

- **Infrastructure Complexity**: Requires GPU-enabled instance ($50-100/month)
- **Operational Burden**: Model management (download, update, monitor)
- **Quality Trade-off**: Open-source models (CodeLlama, DeepSeek) slightly less accurate than GPT-4 (90% vs 95%)

### Mitigation Strategies

1. **Conditional Auto-Fallback**: If Ollama quality < threshold, fallback to OpenAI (only if configured with available tokens)
2. **Model Monitoring**: Track accuracy metrics, trigger alerts on degradation
3. **Automated Model Updates**: Weekly checks for new model versions
4. **Hybrid Strategy**: Reserve cloud APIs for complex tasks requiring highest quality
5. **Token Budget Management**: Enforce monthly token limits per cloud provider to prevent unexpected costs
6. **Configuration Validation**: Verify cloud API configuration on startup and log availability status

---

## Ollama Service Architecture

### High-Level Design with ML Service Boundary

```
┌───────────────────────────────────────────────────────────┐
│                Ollama Service (.NET 9)                    │
│              NO ML TRAINING OR MODELS HERE                │
│                                                           │
│  ┌─────────────────────────────────────────────────────┐ │
│  │          API Layer (Minimal APIs)                   │ │
│  │  • /models (CRUD)                                   │ │
│  │  • /generate (streaming)                            │ │
│  │  • /inference (high-level)                          │ │
│  │  • /health                                          │ │
│  └──────────────────┬──────────────────────────────────┘ │
│                     │                                     │
│  ┌──────────────────▼──────────────────────────────────┐ │
│  │       Domain Services                               │ │
│  │  • ModelManager (download, list, delete)            │ │
│  │  • MlModelSelector (orchestration only)         ────┼─┼──┐
│  │  • ModelRegistry (dynamic model discovery)          │ │  │
│  │  • ABTestingEngine (model performance comparison)   │ │  │
│  │  • HardwareDetector (GPU/VRAM detection)            │ │  │
│  │  • PromptOptimizer (cache, compress)                │ │  │
│  │  • UsageTracker (metrics collection)                │ │  │
│  └──────────────────┬──────────────────────────────────┘ │  │
│                     │                                     │  │
│  ┌──────────────────▼──────────────────────────────────┐ │  │
│  │    Infrastructure Layer                             │ │  │
│  │  • OllamaHttpClient (→ http://ollama:11434)         │ │  │
│  │  • IMlClassifierClient (HTTP client)            ────┼─┼──┤
│  │  • PostgreSQL (model metadata, usage logs)          │ │  │
│  │  • Redis (prompt cache, 24h TTL)                    │ │  │
│  │  • RabbitMQ (publish ModelUsageEvent)               │ │  │
│  └─────────────────────────────────────────────────────┘ │  │
└─────────────────────────┬─────────────────────────────────┘  │
                          │                                    │
                          ▼                                    │ HTTP
         ┌────────────────────────────────────┐               │
         │      Ollama Backend (Docker)       │               │
         │      ollama/ollama:latest          │               │
         │                                    │               │
         │  Models (dynamically managed):     │               │
         │  • Auto-detected on startup        │               │
         │  • Hardware-aware initialization   │               │
         │  • ML-driven selection             │               │
         │  • Regular A/B testing             │               │
         │                                    │               │
         │  Example models (16GB VRAM):       │               │
         │  • codellama:13b (13GB)            │               │
         │  • deepseek-coder:6.7b (4GB)       │               │
         │  • qwen2.5-coder:7b (4.7GB)        │               │
         │  • mistral:7b (4.1GB)              │               │
         │                                    │               │
         │  GPU: Auto-detected (NVIDIA/AMD)   │               │
         │  VRAM: Detected on startup         │               │
         └────────────────────────────────────┘               │
                                                               │
                                                               ▼
┌──────────────────────────────────────────────────────────────────┐
│           ML Classifier Service (Python FastAPI)                 │
│          ALL ML TRAINING AND INFERENCE HAPPENS HERE              │
│                                                                  │
│  • XGBoost/scikit-learn models                                  │
│  • Model training pipeline (weekly retraining)                  │
│  • Feature extraction (TF-IDF, embeddings)                      │
│  • Model versioning & A/B testing                               │
│  • Training data collection from TaskCompletedEvent             │
│                                                                  │
│  API Endpoints:                                                 │
│  • POST /api/predict-model (model recommendation)               │
│  • POST /api/classify (task classification)                     │
│  • POST /api/train (trigger retraining)                         │
│  • GET /api/models/{id}/metrics                                 │
└──────────────────────────────────────────────────────────────────┘
```

### Service Responsibilities Summary

| Service | Responsibilities | Does NOT Do |
|---------|-----------------|-------------|
| **Ollama Service** | • Manage Ollama models<br>• Route inference requests<br>• Extract simple features (keywords, LOC)<br>• Call ML Classifier for predictions<br>• Manage A/B tests<br>• Track usage metrics | ❌ Train ML models<br>❌ Run scikit-learn/XGBoost<br>❌ Feature engineering (TF-IDF, embeddings)<br>❌ Store training data |
| **ML Classifier Service** | • Train XGBoost models<br>• Predict best Ollama model<br>• Classify task types<br>• Feature extraction<br>• Model versioning<br>• Collect training data | ❌ Manage Ollama models<br>❌ Execute LLM inference<br>❌ Manage A/B tests<br>❌ Cache prompts |
│  │  • PostgreSQL (model metadata, usage logs)          │ │
│  │  • Redis (prompt cache, 24h TTL)                    │ │
│  │  • RabbitMQ (publish ModelUsageEvent)               │ │
│  └─────────────────────────────────────────────────────┘ │
└─────────────────────────┬─────────────────────────────────┘
                          │
                          ▼
         ┌────────────────────────────────────┐
         │      Ollama Backend (Docker)       │
         │      ollama/ollama:latest          │
         │                                    │
         │  Models (dynamically managed):     │
         │  • Auto-detected on startup        │
         │  • Hardware-aware initialization   │
         │  • ML-driven selection             │
         │  • Regular A/B testing             │
         │                                    │
         │  Example models (16GB VRAM):       │
         │  • codellama:13b (13GB)            │
         │  • deepseek-coder:6.7b (4GB)       │
         │  • qwen2.5-coder:7b (4.7GB)        │
         │  • mistral:7b (4.1GB)              │
         │                                    │
         │  GPU: Auto-detected (NVIDIA/AMD)   │
         │  VRAM: Detected on startup         │
         └────────────────────────────────────┘
```

### ML-Driven Model Selection Architecture

**Key Principle**: No hardcoded model names. All models discovered dynamically from Ollama backend.

**Service Boundary Clarification:**
- **Ollama Service** (.NET 9): Handles model selection *orchestration* - extracts features, calls ML Classifier, manages A/B tests
- **ML Classifier Service** (Python FastAPI): Performs all ML training, predictions, and model management
- **Clear Separation**: Ollama Service has NO ML models, training logic, or scikit-learn/XGBoost dependencies

```csharp
// Domain/Services/MlModelSelector.cs (IN OLLAMA SERVICE - NO ML CODE)
public class MlModelSelector
{
    private readonly IModelRegistry _registry;
    private readonly IMlClassifierClient _mlClient;  // HTTP client to ML Classifier Service
    private readonly IABTestingEngine _abTesting;

    public async Task<string> SelectModelAsync(InferenceRequest request)
    {
        // 1. Get all available models from registry (dynamically discovered)
        var availableModels = await _registry.GetAvailableModelsAsync();

        // 2. Extract features from request (simple string parsing, NO ML)
        var features = ExtractFeatures(request);

        // 3. Check if this request is part of an A/B test
        var abTest = await _abTesting.GetActiveTestForRequestAsync(request);
        if (abTest != null)
        {
            var selectedModel = abTest.SelectVariant(request.UserId);
            _logger.LogInformation("A/B test {TestId}: routing to {Model}",
                abTest.Id, selectedModel);
            return selectedModel;
        }

        // 4. Use ML classifier to predict best model
        var prediction = await _mlClient.PredictBestModelAsync(new ModelSelectionRequest
        {
            TaskType = features.TaskType,
            Complexity = features.Complexity,
            Language = features.Language,
            ContextSize = features.ContextSize,
            AvailableModels = availableModels.Select(m => m.Name).ToList()
        });

        return prediction.RecommendedModel;
    }

    private TaskFeatures ExtractFeatures(InferenceRequest request)
    {
        // Simple feature extraction - NO ML inference here
        // Just string parsing and basic heuristics
        return new TaskFeatures
        {
            TaskType = DetectTaskType(request.Prompt),      // Keyword matching
            Complexity = EstimateComplexity(request),        // LOC estimation
            Language = DetectLanguage(request.Prompt),       // File extension detection
            ContextSize = request.Prompt.Length + (request.Context?.Length ?? 0)
        };
    }
}

// IMlClassifierClient.cs (HTTP client interface - calls ML Classifier Service)
public interface IMlClassifierClient
{
    // Calls: POST http://ml-classifier-service:5006/api/predict-model
    Task<ModelRecommendation> PredictBestModelAsync(
        ModelSelectionRequest request,
        CancellationToken ct = default);
}

### Component Responsibilities

| Component | Responsibility |
|-----------|----------------|
| **ModelManager** | Download models from Ollama registry, validate availability, track versions, detect hardware capabilities |
| **MlModelSelector** | **ML-driven model selection** based on task features, historical performance, A/B test results |
| **ModelRegistry** | Dynamic registry of all available Ollama models (discovered from Ollama backend) |
| **ABTestingEngine** | Run A/B tests comparing model performance, collect metrics, determine winners |
| **HardwareDetector** | Detect GPU type, VRAM, determine initial model set based on available resources |
| **PromptOptimizer** | Compress prompts, deduplicate, cache deterministic queries |
| **UsageTracker** | Log tokens, latency, cost, accuracy; publish events for analytics and ML training |
| **OllamaHttpClient** | HTTP client for Ollama API (generate, pull, list, show) |
| **OllamaDbContext** | EF Core context for `ollama` schema (models, requests, metrics, ab_tests) |

---

## ML-Driven Model Selection & A/B Testing

### Hardware-Aware Initialization

**Principle**: Initial model set determined by actual available hardware, not assumptions.

```csharp
// Domain/Services/HardwareDetector.cs
public class HardwareDetector
{
    private readonly IOllamaHttpClient _ollamaClient;
    private readonly ILogger<HardwareDetector> _logger;

    public async Task<HardwareProfile> DetectHardwareAsync()
    {
        // Query Ollama backend for GPU info
        var info = await _ollamaClient.GetSystemInfoAsync();

        var profile = new HardwareProfile
        {
            GpuType = info.GpuType,          // "NVIDIA T4", "AMD Radeon", "CPU-only"
            VramGB = info.VramGB,             // 16, 8, 4, or 0 for CPU
            CpuCores = info.CpuCores,
            RamGB = info.RamGB,
            DetectedAt = DateTime.UtcNow
        };

        _logger.LogInformation(
            "Detected hardware: {GpuType} with {VramGB}GB VRAM, {CpuCores} CPU cores",
            profile.GpuType, profile.VramGB, profile.CpuCores);

        return profile;
    }

    public async Task<List<string>> DetermineInitialModelsAsync(HardwareProfile hardware)
    {
        var recommendedModels = new List<string>();

        if (hardware.VramGB >= 24)
        {
            // High-end GPU: Can run 30B+ models
            recommendedModels.AddRange(new[]
            {
                "codellama:34b",
                "deepseek-coder:33b",
                "wizardcoder:34b",
                "phind-codellama:34b"
            });
        }
        else if (hardware.VramGB >= 16)
        {
            // Mid-range GPU: 13B-15B models
            recommendedModels.AddRange(new[]
            {
                "codellama:13b",
                "deepseek-coder:6.7b",
                "qwen2.5-coder:7b",
                "starcoder2:15b"
            });
        }
        else if (hardware.VramGB >= 8)
        {
            // Low-end GPU: 7B models only
            recommendedModels.AddRange(new[]
            {
                "codellama:7b",
                "deepseek-coder:6.7b",
                "qwen2.5-coder:7b",
                "mistral:7b"
            });
        }
        else
        {
            // CPU-only: Small quantized models
            recommendedModels.AddRange(new[]
            {
                "codellama:7b-q4_0",      // 4-bit quantized
                "deepseek-coder:1.3b",
                "phi-2:2.7b"
            });
        }

        _logger.LogInformation(
            "Recommended {Count} models for {VramGB}GB VRAM: {Models}",
            recommendedModels.Count, hardware.VramGB, string.Join(", ", recommendedModels));

        return recommendedModels;
    }
}
```

### A/B Testing Engine

**Principle**: Continuously compare models to find best performers for each task type.

```csharp
// Domain/Services/ABTestingEngine.cs
public class ABTestingEngine
{
    private readonly IABTestRepository _testRepo;
    private readonly ILogger<ABTestingEngine> _logger;

    // Create A/B test comparing two models
    public async Task<ABTest> CreateTestAsync(CreateABTestRequest request)
    {
        var test = new ABTest
        {
            Id = Guid.NewGuid(),
            Name = request.Name,
            ModelA = request.ModelA,
            ModelB = request.ModelB,
            TaskTypeFilter = request.TaskTypeFilter,  // e.g., "BUG_FIX", "FEATURE", null for all
            TrafficPercent = request.TrafficPercent,  // e.g., 10% of traffic
            StartDate = DateTime.UtcNow,
            EndDate = request.DurationDays.HasValue
                ? DateTime.UtcNow.AddDays(request.DurationDays.Value)
                : null,
            Status = ABTestStatus.Active,
            Metrics = new ABTestMetrics()
        };

        await _testRepo.CreateAsync(test);

        _logger.LogInformation(
            "Created A/B test: {Name} comparing {ModelA} vs {ModelB} " +
            "for {TaskType} tasks, {Percent}% traffic",
            test.Name, test.ModelA, test.ModelB,
            test.TaskTypeFilter ?? "ALL", test.TrafficPercent);

        return test;
    }

    // Select variant for a specific request
    public string SelectVariant(ABTest test, Guid userId)
    {
        // Consistent hashing: same user always gets same variant
        var hash = userId.GetHashCode() % 100;
        return hash < 50 ? test.ModelA : test.ModelB;
    }

    // Record test result
    public async Task RecordResultAsync(Guid testId, string model, ABTestResult result)
    {
        var test = await _testRepo.GetByIdAsync(testId);
        if (test == null) return;

        var metrics = model == test.ModelA ? test.Metrics.ModelA : test.Metrics.ModelB;

        metrics.TotalRequests++;
        metrics.SuccessCount += result.Success ? 1 : 0;
        metrics.TotalLatencyMs += result.LatencyMs;
        metrics.TotalTokens += result.TokensUsed;

        if (result.UserRating.HasValue)
        {
            metrics.RatingCount++;
            metrics.TotalRating += result.UserRating.Value;
        }

        await _testRepo.UpdateAsync(test);
    }

    // Analyze test results and determine winner
    public async Task<ABTestAnalysis> AnalyzeTestAsync(Guid testId)
    {
        var test = await _testRepo.GetByIdAsync(testId);
        if (test == null)
            throw new NotFoundException($"A/B test {testId} not found");

        var analysis = new ABTestAnalysis
        {
            TestId = testId,
            ModelA = test.ModelA,
            ModelB = test.ModelB,
            MetricsA = CalculateMetrics(test.Metrics.ModelA),
            MetricsB = CalculateMetrics(test.Metrics.ModelB)
        };

        // Determine winner based on composite score
        analysis.Winner = DetermineWinner(analysis.MetricsA, analysis.MetricsB);
        analysis.Confidence = CalculateStatisticalSignificance(
            test.Metrics.ModelA.TotalRequests,
            test.Metrics.ModelB.TotalRequests);

        _logger.LogInformation(
            "A/B test {Name} analysis: {Winner} wins with {Confidence}% confidence",
            test.Name, analysis.Winner, analysis.Confidence);

        return analysis;
    }

    private ModelMetrics CalculateMetrics(ABTestModelMetrics raw)
    {
        return new ModelMetrics
        {
            SuccessRate = raw.TotalRequests > 0
                ? (double)raw.SuccessCount / raw.TotalRequests
                : 0,
            AvgLatencyMs = raw.TotalRequests > 0
                ? raw.TotalLatencyMs / raw.TotalRequests
                : 0,
            AvgRating = raw.RatingCount > 0
                ? (double)raw.TotalRating / raw.RatingCount
                : 0,
            TotalRequests = raw.TotalRequests
        };
    }

    private string DetermineWinner(ModelMetrics a, ModelMetrics b)
    {
        // Composite score: 40% success rate + 30% latency + 30% rating
        var scoreA = (a.SuccessRate * 0.4) +
                     ((1.0 / a.AvgLatencyMs) * 0.3) +
                     ((a.AvgRating / 5.0) * 0.3);

        var scoreB = (b.SuccessRate * 0.4) +
                     ((1.0 / b.AvgLatencyMs) * 0.3) +
                     ((b.AvgRating / 5.0) * 0.3);

        return scoreA > scoreB ? "ModelA" : "ModelB";
    }
}
```

### Dynamic Model Registry

**Principle**: Discover all models from Ollama backend, no hardcoded lists.

```csharp
// Domain/Services/ModelRegistry.cs
public class ModelRegistry : IHostedService
{
    private readonly IOllamaHttpClient _ollamaClient;
    private readonly IModelRepository _modelRepo;
    private readonly ILogger<ModelRegistry> _logger;
    private Timer _syncTimer;

    // Sync models from Ollama backend every 5 minutes
    public Task StartAsync(CancellationToken ct)
    {
        _syncTimer = new Timer(
            async _ => await SyncModelsAsync(),
            null,
            TimeSpan.Zero,          // Run immediately on startup
            TimeSpan.FromMinutes(5) // Then every 5 minutes
        );

        return Task.CompletedTask;
    }

    private async Task SyncModelsAsync()
    {
        try
        {
            _logger.LogInformation("Syncing models from Ollama backend...");

            // Get all models from Ollama
            var ollamaModels = await _ollamaClient.ListModelsAsync();

            // Get current models from database
            var dbModels = await _modelRepo.GetAllAsync();

            // Add new models
            foreach (var ollamaModel in ollamaModels)
            {
                var exists = dbModels.Any(m => m.Name == ollamaModel.Name);
                if (!exists)
                {
                    var model = new OllamaModel
                    {
                        Name = ollamaModel.Name,
                        DisplayName = FormatDisplayName(ollamaModel.Name),
                        SizeGB = ollamaModel.Size / (1024.0 * 1024.0 * 1024.0),
                        ParameterCount = ExtractParameterCount(ollamaModel.Name),
                        Family = ExtractFamily(ollamaModel.Name),
                        IsAvailable = true,
                        LastSyncedAt = DateTime.UtcNow
                    };

                    await _modelRepo.CreateAsync(model);

                    _logger.LogInformation(
                        "Discovered new model: {Name} ({SizeGB:F1}GB, {Params} params)",
                        model.Name, model.SizeGB, model.ParameterCount);
                }
            }

            // Mark removed models as unavailable
            foreach (var dbModel in dbModels)
            {
                var stillExists = ollamaModels.Any(m => m.Name == dbModel.Name);
                if (!stillExists && dbModel.IsAvailable)
                {
                    dbModel.IsAvailable = false;
                    await _modelRepo.UpdateAsync(dbModel);

                    _logger.LogWarning(
                        "Model no longer available: {Name}", dbModel.Name);
                }
            }

            _logger.LogInformation(
                "Model sync complete: {Total} models available",
                ollamaModels.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to sync models from Ollama backend");
        }
    }

    public Task StopAsync(CancellationToken ct)
    {
        _syncTimer?.Dispose();
        return Task.CompletedTask;
    }

    // Helper methods
    private string FormatDisplayName(string name)
    {
        // "codellama:13b" → "CodeLlama 13B"
        var parts = name.Split(':');
        var baseName = parts[0].Replace("-", " ");
        var version = parts.Length > 1 ? parts[1].ToUpper() : "";
        return $"{CultureInfo.CurrentCulture.TextInfo.ToTitleCase(baseName)} {version}".Trim();
    }

    private string ExtractParameterCount(string name)
    {
        // Extract "13b", "7b", etc.
        var match = Regex.Match(name, @"(\d+\.?\d*)b", RegexOptions.IgnoreCase);
        return match.Success ? match.Groups[1].Value + "B" : "Unknown";
    }

    private string ExtractFamily(string name)
    {
        // "codellama:13b" → "codellama"
        return name.Split(':')[0].Split('-')[0];
    }
}
```

### Integration with ML Classifier

**ML Classifier trains on historical performance to predict best model.**

```python
# In ML Classifier Service
class ModelRecommender:
    """Recommends best Ollama model based on task features"""

    def __init__(self, model_path: str):
        self.model = joblib.load(model_path)
        self.feature_extractor = FeatureExtractor()

    async def predict_best_model(
        self,
        request: ModelSelectionRequest
    ) -> ModelRecommendation:
        """
        Predict best model from available models.

        Training data: historical inference results with:
        - Task features (type, complexity, language, context_size)
        - Model used
        - Outcome metrics (success, latency, quality score)
        """
        # Extract features
        features = self.feature_extractor.extract({
            'task_type': request.task_type,
            'complexity': request.complexity,
            'language': request.language,
            'context_size': request.context_size
        })

        # Predict score for each available model
        predictions = {}
        for model_name in request.available_models:
            features_with_model = np.append(features, self._encode_model(model_name))
            score = self.model.predict_proba([features_with_model])[0][1]
            predictions[model_name] = score

        # Return top model
        best_model = max(predictions, key=predictions.get)
        confidence = predictions[best_model]

        return ModelRecommendation(
            recommended_model=best_model,
            confidence=confidence,
            alternatives=[
                (model, score)
                for model, score in sorted(
                    predictions.items(),
                    key=lambda x: x[1],
                    reverse=True
                )[1:4]  # Top 3 alternatives
            ]
        )

    def _encode_model(self, model_name: str) -> np.ndarray:
        """Encode model name into feature vector"""
        # Extract model family and size
        family = model_name.split(':')[0]
        size = self._extract_param_count(model_name)

        return np.array([
            self._hash_feature(family),  # Model family (codellama, deepseek, etc.)
            size,                         # Parameter count (billions)
        ])
```

---

## Integration Patterns

### Pattern 1: Orchestration → Ollama with ML-Driven Model Selection

**Scenario**: Orchestration Service needs to generate code for a bug fix.

**Flow**:
```
1. Orchestration receives task (type: BUG_FIX, complexity: SIMPLE, language: C#)
2. Orchestration calls ML Classifier → confirms task classification
3. Orchestration selects LLM provider (Ollama for SIMPLE tasks)
4. Orchestration sends request to Ollama Service:
   POST /api/ollama/inference
   {
       "prompt": "Fix null reference exception in UserService.cs",
       "task_type": "BUG_FIX",
       "complexity": "SIMPLE",
       "language": "csharp",
       "model": "auto",          // ML-driven selection
       "temperature": 0.3,
       "max_tokens": 2000,
       "context": "... relevant code context ..."
   }
5. Ollama Service (ML-Driven Selection):
   a. MlModelSelector extracts task features
   b. Checks for active A/B tests (10% of traffic)
   c. Queries ModelRegistry for available models
   d. Calls ML Classifier to predict best model
   e. ML Classifier recommends: "deepseek-coder:6.7b" (confidence: 0.89)
   f. Checks Redis cache for this prompt+model combo (miss)
   g. Calls Ollama Backend: POST /api/generate with selected model
   h. Streams response back to Orchestration
   i. Records metrics: success=true, latency=2.3s, tokens=450
   j. Publishes ModelUsageEvent to RabbitMQ (for ML retraining)
   k. If A/B test active: records test result
6. Orchestration receives generated code
7. Background: ML Classifier ingests usage data, retrains weekly
```

**Code Example**:

```csharp
// In Orchestration Service
public class LlmClientFactory
{
    private readonly IOllamaLlmClient _ollamaClient;
    private readonly IOpenAIClient _openAiClient;

    public ILlmClient GetClient(TaskComplexity complexity, decimal budgetRemaining)
    {
        return complexity switch
        {
            TaskComplexity.Simple => _ollamaClient,              // Always use Ollama
            TaskComplexity.Medium when budgetRemaining > 10m => _openAiClient,
            TaskComplexity.Medium => _ollamaClient,              // Fallback to Ollama
            TaskComplexity.Complex => _openAiClient,             // Require cloud for complex
            _ => _ollamaClient
        };
    }
}

// Ollama LLM Client Implementation
public class OllamaLlmClient : ILlmClient
{
    private readonly HttpClient _httpClient;

    public async Task<LlmResponse> GenerateAsync(LlmRequest request, CancellationToken ct)
    {
        var ollamaRequest = new
        {
            model = "auto", // Let Ollama Service route
            prompt = request.Prompt,
            options = new
            {
                temperature = request.Temperature,
                num_predict = request.MaxTokens
            }
        };

        var response = await _httpClient.PostAsJsonAsync(
            "http://ollama-service:5008/code-generation",
            ollamaRequest,
            ct
        );

        var ollamaResponse = await response.Content.ReadFromJsonAsync<OllamaResponse>(ct);

        return new LlmResponse
        {
            Content = ollamaResponse.Response,
            Model = ollamaResponse.ModelName,
            TokensUsed = ollamaResponse.PromptEvalCount + ollamaResponse.EvalCount,
            Cost = 0.0m, // Free!
            Latency = TimeSpan.FromMilliseconds(ollamaResponse.EvalDuration / 1_000_000)
        };
    }
}
```

---

### Pattern 2: Prompt Caching for Deterministic Queries

**Scenario**: Multiple tasks have identical system prompts (e.g., code review guidelines).

**Strategy**: Cache prompt → response mapping in Redis with 24-hour TTL.

```csharp
public async Task<string> GenerateWithCacheAsync(string prompt, string model)
{
    // Generate cache key (SHA256 hash of prompt + model)
    var cacheKey = $"ollama:cache:{model}:{ComputeSHA256(prompt)}";

    // Check Redis
    var cached = await _cache.GetStringAsync(cacheKey);
    if (cached != null)
    {
        _logger.LogInformation("Cache hit: {CacheKey}", cacheKey);
        _metrics.RecordCacheHit();
        return cached;
    }

    // Cache miss - generate via Ollama
    var response = await _ollamaClient.GenerateAsync(model, prompt);

    // Only cache deterministic prompts (temperature = 0)
    if (IsDeterministicPrompt(prompt))
    {
        await _cache.SetStringAsync(
            cacheKey,
            response,
            new DistributedCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = TimeSpan.FromHours(24)
            }
        );
    }

    _metrics.RecordCacheMiss();
    return response;
}

private bool IsDeterministicPrompt(string prompt)
{
    // Heuristic: prompts with "Explain", "Review", "Analyze" are likely deterministic
    return prompt.Contains("Explain") ||
           prompt.Contains("Review") ||
           prompt.Contains("Analyze");
}
```

**Impact**:
- Cache hit rate: **40-60%** for repeated code patterns
- Latency reduction: 5s → **50ms** for cached queries
- Cost savings: Eliminates redundant inference compute

---

### Pattern 3: Circuit Breaker with Cloud API Fallback

**Scenario**: Ollama Service is overloaded or Ollama Backend crashes.

**Strategy**: Use Polly circuit breaker to detect failures, fallback to OpenAI.

```csharp
// In Orchestration Service
services.AddHttpClient<IOllamaLlmClient, OllamaLlmClient>()
    .AddPolicyHandler(GetCircuitBreakerPolicy())
    .AddPolicyHandler(GetRetryPolicy());

static IAsyncPolicy<HttpResponseMessage> GetCircuitBreakerPolicy()
{
    return HttpPolicyExtensions
        .HandleTransientHttpError()
        .CircuitBreakerAsync(
            handledEventsAllowedBeforeBreaking: 5,   // 5 failures
            durationOfBreak: TimeSpan.FromSeconds(30),
            onBreak: (outcome, duration) =>
            {
                Log.Warning("Ollama circuit breaker opened, falling back to OpenAI");
            },
            onReset: () =>
            {
                Log.Information("Ollama circuit breaker reset");
            }
        );
}

// Fallback logic
public async Task<LlmResponse> GenerateWithFallbackAsync(LlmRequest request)
{
    try
    {
        return await _ollamaClient.GenerateAsync(request);
    }
    catch (BrokenCircuitException)
    {
        _logger.LogWarning("Ollama circuit open, attempting fallback to OpenAI");

        // Only fallback if OpenAI is configured and has tokens available
        if (!_openAiClient.IsConfigured() || !await _openAiClient.HasTokensAvailableAsync())
        {
            _logger.LogError("OpenAI fallback unavailable (not configured or no tokens)");
            throw new LlmServiceUnavailableException(
                "Ollama service is down and OpenAI fallback is not available");
        }

        _metrics.RecordFallback("ollama-to-openai");
        return await _openAiClient.GenerateAsync(request);
    }
}
```

**Outcome**:
- **Zero downtime**: Automatic failover to cloud API (when configured and tokens available)
- **Graceful degradation**: Higher cost, but service remains operational
- **Auto-recovery**: Circuit closes after 30s, retries Ollama
- **Fail-safe**: If cloud API unavailable, throws descriptive exception

### Pattern 4: Cloud API Configuration and Token Management

**Scenario**: Ensure cloud APIs are only used when properly configured and have tokens available.

**Strategy**: Implement configuration validation and token tracking.

```csharp
public interface ICloudApiClient
{
    bool IsConfigured();
    Task<bool> HasTokensAvailableAsync(CancellationToken ct = default);
    Task<LlmResponse> GenerateAsync(LlmRequest request, CancellationToken ct = default);
}

public class OpenAiClient : ICloudApiClient
{
    private readonly IConfiguration _config;
    private readonly ITokenUsageRepository _tokenRepo;
    private readonly ILogger<OpenAiClient> _logger;

    public bool IsConfigured()
    {
        var config = _config.GetSection("Orchestration:CloudApis:OpenAI");
        var enabled = config.GetValue<bool>("Enabled");
        var apiKey = config.GetValue<string>("ApiKey");

        return enabled && !string.IsNullOrWhiteSpace(apiKey) && apiKey != "sk-...";
    }

    public async Task<bool> HasTokensAvailableAsync(CancellationToken ct = default)
    {
        if (!IsConfigured())
            return false;

        var config = _config.GetSection("Orchestration:CloudApis:OpenAI");
        var maxTokens = config.GetValue<int>("MaxTokensPerMonth");

        if (maxTokens <= 0)
            return true; // No limit configured

        var currentMonth = DateTime.UtcNow.ToString("yyyy-MM");
        var usedTokens = await _tokenRepo.GetUsageForMonthAsync("openai", currentMonth, ct);

        var available = usedTokens < maxTokens;

        if (!available)
        {
            _logger.LogWarning(
                "OpenAI token limit reached: {UsedTokens}/{MaxTokens} for {Month}",
                usedTokens, maxTokens, currentMonth);
        }

        return available;
    }

    public async Task<LlmResponse> GenerateAsync(LlmRequest request, CancellationToken ct = default)
    {
        if (!IsConfigured())
            throw new InvalidOperationException("OpenAI is not configured");

        if (!await HasTokensAvailableAsync(ct))
            throw new TokenLimitExceededException("OpenAI monthly token limit exceeded");

        // Proceed with API call...
        var response = await _openAiClient.CreateCompletionAsync(request, ct);

        // Track token usage
        await _tokenRepo.RecordUsageAsync(new TokenUsage
        {
            Provider = "openai",
            TokensUsed = response.TokensUsed,
            Month = DateTime.UtcNow.ToString("yyyy-MM"),
            Timestamp = DateTime.UtcNow
        }, ct);

        return response;
    }
}
```

**Configuration Validation on Startup**:

```csharp
// In Program.cs
var builder = WebApplication.CreateBuilder(args);

// Register cloud API clients
builder.Services.AddScoped<ICloudApiClient, OpenAiClient>();
builder.Services.AddScoped<ICloudApiClient, AnthropicClient>();

var app = builder.Build();

// Validate configuration on startup
var logger = app.Services.GetRequiredService<ILogger<Program>>();
var openAiClient = app.Services.GetRequiredService<OpenAiClient>();

if (openAiClient.IsConfigured())
{
    logger.LogInformation("OpenAI is configured and enabled");
}
else
{
    logger.LogWarning("OpenAI is not configured - fallback will not be available");
}

app.Run();
```

**Impact**:
- **Safety**: Prevents API calls without valid configuration
- **Cost Control**: Enforces monthly token limits
- **Transparency**: Logs warnings when limits are reached
- **Fail-fast**: Throws exceptions instead of silent failures

---

## Cost-Benefit Analysis

### Monthly Cost Breakdown (1,000 tasks/day)

| Scenario | Cloud API Cost | Ollama Infra Cost | Total Cost | Savings |
|----------|---------------|-------------------|------------|---------|
| **Baseline (Cloud Only)** | $150/month | $0 | **$150** | - |
| **Ollama Service (85% local)** | $22.50/month | $75/month (GPU instance) | **$97.50** | **35%** |
| **Ollama Service (95% local)** | $7.50/month | $75/month | **$82.50** | **45%** |

**Assumptions**:
- Average task: 500 prompt tokens + 1,500 completion tokens = 2,000 tokens
- Cloud API cost: $2.50 per 1M tokens (GPT-4o)
- GPU instance: AWS EC2 g4dn.xlarge ($0.10/hour × 730 hours = $73/month)

### ROI Calculation

**Break-even point**: After **1 month** (Ollama infrastructure cost < cloud API cost).

```
Monthly Cloud Cost: $150
Monthly Ollama Cost: $97.50
Monthly Savings: $52.50

Annual Savings: $52.50 × 12 = $630
```

### Qualitative Benefits

| Benefit | Value |
|---------|-------|
| **Data Privacy** | High (proprietary code stays on-premise) |
| **Compliance** | High (GDPR, HIPAA, SOC 2) |
| **Offline Support** | Medium (works without internet) |
| **Autonomy** | High (no dependency on external APIs) |
| **Performance** | Medium (5-10s latency vs 10-30s for cloud) |

---

## Implementation Strategy

### Phase 1: Foundation (Week 1-2)

**Goal**: Scaffold Ollama Service, deploy Ollama Backend, detect hardware, initialize models.

**Tasks**:
1. Create `src/Services/Ollama/CodingAgent.Services.Ollama` project
2. Implement domain models (OllamaModel, OllamaRequest, OllamaResponse, HardwareProfile, ABTest)
3. Implement OllamaHttpClient (wrapper around Ollama REST API)
4. **Implement HardwareDetector (detect GPU, VRAM, CPU)**
5. Deploy Ollama Backend in Docker Compose
6. **Auto-detect hardware on startup, determine initial models based on VRAM**
7. **Pull hardware-appropriate models (e.g., 13B for 16GB VRAM, 7B for 8GB VRAM)**
8. **Implement ModelRegistry as IHostedService (sync models every 5 minutes)**
9. Write integration tests (Testcontainers for Ollama)

**Success Criteria**:
- ✅ HardwareDetector correctly identifies GPU type and VRAM
- ✅ Initial models downloaded based on actual hardware capabilities
- ✅ ModelRegistry dynamically discovers all available models
- ✅ Ollama Service can list models from Ollama Backend
- ✅ Ollama Service can generate text via `/generate` endpoint
- ✅ Integration tests pass (happy path)

---

### Phase 2: ML-Driven Model Selection (Week 3-4)

**Goal**: Implement ML-driven model selection, replace hardcoded routing logic.

**Tasks**:
1. **Implement MlModelSelector (replaces InferenceRouter)**
2. **Integrate with ML Classifier service for model prediction**
3. **Extract task features: task_type, complexity, language, context_size**
4. Implement PromptOptimizer (Redis caching)
5. Implement UsageTracker (PostgreSQL logging, RabbitMQ events)
   - **Add accuracy tracking (user feedback, test pass rate)**
   - **Publish detailed metrics for ML training**
6. Add health checks (OllamaHealthCheck)
7. Integrate with Gateway (YARP routing)
8. Add OpenTelemetry tracing

**Success Criteria**:
- ✅ MlModelSelector queries ML Classifier for model recommendation
- ✅ Model selection based on task features, not hardcoded rules
- ✅ ML Classifier returns model with confidence score > 0.7
- ✅ Cache hit rate > 40% for deterministic prompts
- ✅ Usage metrics (including accuracy) logged to PostgreSQL
- ✅ ModelUsageEvent with performance metrics published to RabbitMQ

---

### Phase 3: A/B Testing & Orchestration Integration (Week 5-6)

**Goal**: Implement A/B testing, integrate with Orchestration Service.

**Tasks**:
1. **Implement ABTestingEngine**
   - **Create/manage A/B tests comparing two models**
   - **Route traffic (e.g., 10%) to test variants**
   - **Record test results (latency, success, user rating)**
   - **Analyze results, determine winners**
2. **Update MlModelSelector to check for active A/B tests**
3. Implement OllamaLlmClient in Orchestration Service
4. Implement LlmClientFactory (routing logic)
5. Add Polly circuit breaker + retry policies
6. Implement fallback logic (Ollama → OpenAI, with config/token checks)
7. Update execution strategies to use Ollama for simple tasks
8. Add monitoring dashboards (Grafana)
   - **A/B test results dashboard**
   - **Model performance comparison charts**

**Success Criteria**:
- ✅ A/B tests run automatically for 10% of traffic
- ✅ Test results tracked with statistical significance calculations
- ✅ Winning models automatically promoted after test completion
- ✅ 85% of simple tasks use Ollama Service
- ✅ Circuit breaker fallback to OpenAI (only if configured with tokens)
- ✅ Cost reduction: $150/month → $97.50/month
- ✅ Latency: p95 < 10s for simple tasks

---

### Phase 4: ML Training & Production Readiness (Week 7-8)

**Goal**: Train ML model on usage data, optimize performance, production deployment.

**Tasks**:
1. **ML Classifier: Implement model recommender training pipeline**
   - **Collect historical data: task features + model used + outcome metrics**
   - **Train XGBoost model to predict best model for task**
   - **Weekly retraining on new usage data**
   - **A/B test new model versions before production**
2. GPU optimization (batch inference, model quantization)
3. **Automated model updates: Check for new models weekly, A/B test before adoption**
4. Implement model preloading (warm cache on startup)
5. Load testing (k6): 100 concurrent requests
6. Deploy to production (Kubernetes with GPU nodepool)

**Success Criteria**:
- ✅ ML recommender model trained with 10,000+ data points
- ✅ Model prediction accuracy > 85% (picks best model for task)
- ✅ Automated weekly retraining pipeline operational
- ✅ New models automatically discovered and A/B tested
- ✅ p95 latency < 10s under 100 concurrent requests
- ✅ GPU utilization > 70%
- ✅ 99.5% availability
- ✅ Zero production incidents

---

## Risks and Mitigations

### Risk 1: ML Model Selection Accuracy Lower Than Expected

**Probability**: Medium (30%)

**Impact**: High - Wrong model selected, poor inference quality

**Mitigation**:
1. **Bootstrap with heuristic rules**: Use rule-based fallback until ML model has 1,000+ training samples
2. **Continuous A/B testing**: Always test new models against current champion
3. **User feedback loop**: Collect explicit quality ratings (thumbs up/down)
4. **Automated quality checks**: Run unit tests on generated code, measure pass rate
5. **Champion/challenger pattern**: Keep best-performing model as default, challenge with new candidates

**Risk Level**: Medium
**Impact**: Users complain about poor code quality for Ollama-generated code

**Mitigation**:
1. **Hybrid Strategy**: Reserve OpenAI for complex tasks
2. **Quality Monitoring**: Track user feedback (thumbs up/down)
3. **Auto-Fallback**: If quality score < 70%, fallback to OpenAI
4. **Continuous Model Updates**: Check for new model versions weekly

---

### Risk 2: GPU Instance Costs Higher Than Expected

**Risk Level**: Low
**Impact**: Ollama infrastructure costs exceed budget ($75/month → $150/month)

**Mitigation**:
1. **Spot Instances**: Use AWS EC2 Spot for 70% cost reduction
2. **Auto-Scaling**: Scale down to zero during low-traffic hours (nights, weekends)
3. **Model Quantization**: Use 4-bit quantized models (4GB VRAM → 8GB VRAM)
4. **CPU Fallback**: Use CPU-only instances for development/staging

---

### Risk 3: Ollama Backend Instability

**Risk Level**: Medium
**Impact**: Ollama Backend crashes, causing service outages

**Mitigation**:
1. **Circuit Breaker**: Auto-fallback to OpenAI within 30s
2. **Health Checks**: Kubernetes liveness/readiness probes restart unhealthy pods
3. **Redundancy**: Deploy 2 Ollama Backend replicas (if GPU budget allows)
4. **Monitoring**: Alert on Ollama Backend errors (PagerDuty)

---

### Risk 4: Model Storage Costs

**Risk Level**: Low
**Impact**: 100GB of models → high storage costs

**Mitigation**:
1. **Selective Models**: Only pull models actively used (codellama, deepseek)
2. **Cleanup Policy**: Delete unused models after 30 days
3. **Compressed Storage**: Use persistent volumes with compression

---

## Future Considerations

### Phase 2 Enhancements (Q3 2026)

1. **Fine-Tuning Support**:
   - Fine-tune CodeLlama on proprietary codebase
   - Improve accuracy for domain-specific code

2. **Multi-Model Ensemble**:
   - Generate code with 3 models (CodeLlama, DeepSeek, StarCoder)
   - Use ensemble voting for best result

3. **Model Quantization**:
   - Deploy 4-bit quantized models (GPTQ, AWQ)
   - Reduce VRAM requirements: 13B model from 26GB → 8GB

4. **Edge Deployment**:
   - Deploy Ollama Service on edge devices (developer laptops)
   - Offline code generation for remote work

### Phase 3 Enhancements (Q4 2026)

1. **Custom Model Registry**:
   - Internal Ollama model registry
   - Version control for fine-tuned models

2. **Reinforcement Learning**:
   - Use user feedback to fine-tune models via RLHF
   - Improve accuracy iteratively

3. **Multi-Tenant Support**:
   - Isolate models per tenant/customer
   - Custom model per customer (white-label)

---

## Appendix: Model Comparison

### Open-Source Models for Code Generation

| Model | Size | Context | Accuracy | Latency | Best For |
|-------|------|---------|----------|---------|----------|
| **CodeLlama 13B** | 13GB | 16K | 90% | 5s | General code generation |
| **CodeLlama 34B** | 34GB | 16K | 93% | 15s | Complex refactoring |
| **DeepSeek Coder 6.7B** | 4GB | 4K | 88% | 2s | Fast inference, simple tasks |
| **StarCoder2 7B** | 4GB | 8K | 89% | 3s | Multi-language support |
| **Mistral 7B** | 4GB | 8K | 85% | 3s | Chat, documentation |

### Cloud APIs (Comparison Baseline)

| Model | Cost per 1M Tokens | Accuracy | Latency | Notes |
|-------|-------------------|----------|---------|-------|
| **GPT-4o** | $2.50 | 95% | 10s | Best quality |
| **GPT-4o-mini** | $0.15 | 92% | 5s | Cost-effective |
| **Claude 3.5 Sonnet** | $3.00 | 96% | 12s | Excellent for code |

---

## Conclusion

The **Ollama Service** is a strategic addition to the Coding Agent v2.0 architecture, enabling:
- **Cost Optimization**: 85% reduction in LLM operational costs
- **Data Privacy**: On-premise inference for sensitive codebases
- **Performance**: Lower latency for simple/medium complexity tasks
- **Autonomy**: No dependency on external API rate limits

The hybrid LLM strategy (Ollama for 85% of tasks, cloud APIs for 15%) balances cost, quality, and privacy, positioning the platform for sustainable, scalable growth.

---

**Document Owner**: Technical Lead
**Review Cycle**: Quarterly
**Next Review**: January 2026
**Approval**: ✅ Approved for implementation

