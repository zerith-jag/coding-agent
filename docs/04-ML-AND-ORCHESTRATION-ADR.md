# ML and Orchestration Architecture Decision Record (ADR)

**Version**: 2.0.0
**Last Updated**: October 24, 2025
**Status**: Approved for v2.0 Microservices Architecture

---

## Table of Contents

1. [Executive Summary](#executive-summary)
2. [ML Classifier Architecture](#ml-classifier-architecture)
3. [Orchestration Service Architecture](#orchestration-service-architecture)
4. [Self-Learning System](#self-learning-system)
5. [Model Selection & Routing](#model-selection--routing)
6. [Feedback Loops](#feedback-loops)
7. [Performance Targets](#performance-targets)
8. [Migration from v1.0](#migration-from-v10)

---

## Executive Summary

### Key Architectural Decisions

1. **Python for ML**: ML Classifier service uses Python FastAPI with scikit-learn/XGBoost for optimal ML ecosystem access
2. **.NET for Orchestration**: Core orchestration logic in .NET 9 for type safety, performance, and integration
3. **Hybrid Classification**: Heuristic-first for speed (90% accuracy), ML fallback for precision (95%+ accuracy)
4. **Event-Driven Learning**: Async feedback loops using RabbitMQ for continuous model improvement
5. **Multi-Strategy Execution**: SingleShot, Iterative, MultiAgent strategies selected dynamically
6. **PostgreSQL + Redis Learning**: Persistent feedback storage with fast caching for real-time optimization

### Design Philosophy

> "Start simple, learn fast, scale intelligently"

- **v1.0**: Rule-based classification → Works but static
- **v2.0**: ML-powered classification + continuous learning → Adapts and improves
- **v3.0** (future): Deep learning + reinforcement learning → Autonomous optimization

---

## ML Classifier Architecture

### 1. Technology Stack Decision

**Decision**: Use Python FastAPI + scikit-learn/XGBoost for ML service

**Rationale**:
- **Python ML Ecosystem**: Access to scikit-learn, XGBoost, TensorFlow, PyTorch, Hugging Face
- **Data Science Tools**: pandas, numpy for feature engineering
- **Model Serving**: ONNX Runtime for optimized inference
- **Rapid Iteration**: Easier to experiment with new models vs .NET ML.NET

**Alternatives Considered**:
- ❌ **ML.NET**: Limited model selection, less mature ecosystem
- ❌ **Azure ML**: Vendor lock-in, higher costs
- ✅ **Python FastAPI**: Winner - best ML tools, framework-agnostic

### 2. Hybrid Classification Strategy

**Architecture**:

```python
def classify(request: ClassificationRequest) -> ClassificationResult:
    # Phase 1: Fast heuristic classification
    heuristic_result = heuristic_classify(request.task_description)

    if heuristic_result.confidence > 0.85:
        return heuristic_result  # Fast path (90% of requests)

    # Phase 2: ML classification for ambiguous cases
    ml_result = ml_classify(request.task_description)

    if ml_result.confidence > 0.70:
        return ml_result  # Accurate path (9% of requests)

    # Phase 3: LLM classification for edge cases
    llm_result = llm_classify(request.task_description)
    return llm_result  # Most accurate (1% of requests)
```

**Performance Characteristics**:

| Strategy | Accuracy | Latency | Cost | Usage % |
|----------|----------|---------|------|---------|
| **Heuristic** | 90% | 5ms | $0 | 85% |
| **ML (XGBoost)** | 95% | 50ms | $0 | 14% |
| **LLM (GPT-4)** | 98% | 800ms | $0.01 | 1% |

**Trade-offs**:
- Optimizes for **speed** (5ms median) while maintaining **accuracy** (93% overall)
- **Cost-effective**: Only 1% of classifications use expensive LLM calls
- **Scalable**: 95% of work handled by cheap compute (heuristic + ML)

### 3. Feature Engineering

**Input Features**:

```python
class TaskFeatures(BaseModel):
    # Text features
    title_length: int
    description_length: int
    title_tokens: int
    description_tokens: int

    # Keyword features (TF-IDF)
    bug_keywords_count: int
    feature_keywords_count: int
    refactor_keywords_count: int
    test_keywords_count: int

    # Code features
    files_changed_count: int
    has_code_snippets: bool
    programming_languages: List[str]

    # Metadata features
    label_count: int
    has_priority_label: bool
    is_urgent: bool

    # Historical features (from ML feedback)
    similar_task_success_rate: float
    avg_complexity_for_keywords: float
```

**Feature Extraction Pipeline**:

```python
def extract_features(task: ClassificationRequest) -> TaskFeatures:
    # 1. Text analysis
    title_tokens = tokenize(task.task_description)

    # 2. TF-IDF vectorization
    tfidf_vector = vectorizer.transform([task.task_description])

    # 3. Code pattern detection
    code_patterns = detect_code_patterns(task.context)

    # 4. Historical lookup
    similar_tasks = query_similar_tasks(title_tokens, limit=10)

    return TaskFeatures(
        title_length=len(task.task_description),
        # ... other features
        similar_task_success_rate=calculate_success_rate(similar_tasks)
    )
```

### 4. Model Training Pipeline

**Training Architecture**:

```
User Feedback → PostgreSQL → Training Queue → Feature Engineering → Model Training → Validation → Deployment
                    ↓                                                                      ↓
                Redis Cache ←─────────────────────────────────────────────────────────────┘
```

**Automated Retraining Triggers**:

1. **Batch Size**: Retrain every 1,000 new feedback samples
2. **Time-Based**: Retrain weekly regardless of sample count
3. **Accuracy Drop**: Retrain if accuracy falls below 92%
4. **Manual**: API endpoint for on-demand retraining

**Training Process**:

```python
async def train_classifier():
    # 1. Fetch training data from PostgreSQL
    training_data = await fetch_training_samples(limit=10000)

    # 2. Split train/validation/test (70/15/15)
    X_train, X_val, X_test, y_train, y_val, y_test = split_data(training_data)

    # 3. Feature engineering
    X_train_features = extract_features_batch(X_train)

    # 4. Train XGBoost model
    model = xgb.XGBClassifier(
        n_estimators=100,
        max_depth=6,
        learning_rate=0.1,
        objective='multi:softmax',
        num_class=len(TaskType)
    )
    model.fit(X_train_features, y_train)

    # 5. Validation
    val_accuracy = model.score(X_val_features, y_val)
    if val_accuracy < 0.92:
        raise ValueError(f"Model accuracy {val_accuracy} below threshold")

    # 6. Test evaluation
    test_metrics = evaluate_model(model, X_test_features, y_test)

    # 7. Export to ONNX for fast inference
    onnx_model = convert_to_onnx(model)

    # 8. Deploy to production
    save_model(onnx_model, version=get_next_version())

    # 9. Cache in Redis
    await cache_model_metadata(test_metrics)

    return {
        "version": get_current_version(),
        "accuracy": test_metrics.accuracy,
        "precision": test_metrics.precision,
        "recall": test_metrics.recall,
        "f1_score": test_metrics.f1_score
    }
```

### 5. Model Versioning

**Version Schema**: `v{major}.{minor}.{patch}`

- **Major**: Breaking API changes
- **Minor**: New features (e.g., new task types)
- **Patch**: Bug fixes, retraining with same architecture

**Model Registry**:

```sql
CREATE TABLE ml_models (
    id SERIAL PRIMARY KEY,
    model_name VARCHAR(100) NOT NULL,
    version VARCHAR(20) NOT NULL,
    model_path TEXT NOT NULL,
    onnx_path TEXT,

    -- Performance metrics
    accuracy DECIMAL(5,4) NOT NULL,
    precision DECIMAL(5,4) NOT NULL,
    recall DECIMAL(5,4) NOT NULL,
    f1_score DECIMAL(5,4) NOT NULL,

    -- Training metadata
    training_samples_count INT NOT NULL,
    training_duration_seconds INT,
    hyperparameters JSONB,

    -- Deployment
    is_active BOOLEAN DEFAULT FALSE,
    deployed_at TIMESTAMPTZ,
    created_at TIMESTAMPTZ DEFAULT NOW(),

    -- Provenance
    trained_by VARCHAR(100),
    training_job_id VARCHAR(100),

    UNIQUE(model_name, version)
);

CREATE INDEX idx_ml_models_active ON ml_models(is_active, model_name);
```

**A/B Testing Support**:

```python
# Route 10% of traffic to new model for canary testing
def get_active_model(request_id: str) -> MLModel:
    if is_canary_traffic(request_id, percentage=10):
        return get_model(version="v2.1.0-canary")
    else:
        return get_model(version="v2.0.5-stable")
```

---

## Orchestration Service Architecture

### 1. Core Responsibilities

The Orchestration Service is the **brain** of the system:

1. **Task Classification**: Determines task type, complexity, and execution requirements
2. **Strategy Selection**: Chooses optimal execution strategy based on classification
3. **Agent Coordination**: Manages multi-agent workflows for complex tasks
4. **Resource Management**: Allocates compute resources, manages rate limits
5. **Execution Monitoring**: Tracks progress, handles failures, collects metrics
6. **Learning Coordination**: Sends feedback to ML service for continuous improvement

### 2. Execution Strategies

**Strategy Selection Matrix**:

| Complexity | Lines of Code | Strategy | Models Used | Execution Time |
|------------|---------------|----------|-------------|----------------|
| **Simple** | < 50 LOC | SingleShot | gpt-4o-mini | 10-30s |
| **Medium** | 50-200 LOC | Iterative | gpt-4o | 30-120s |
| **Complex** | 200-1000 LOC | MultiAgent | gpt-4o + claude-3.5-sonnet | 120-600s |
| **Epic** | > 1000 LOC | HybridExecution | Ensemble (3+ models) | 600-3600s |

#### Strategy 1: SingleShot

**Use Case**: Simple, well-defined tasks (bug fixes, small features)

**Workflow**:

```csharp
public async Task<ExecutionResult> ExecuteSingleShotAsync(CodingTask task)
{
    // 1. Prepare context
    var context = await BuildContextAsync(task);

    // 2. Generate prompt
    var prompt = PromptBuilder.CreateSingleShotPrompt(task, context);

    // 3. Call LLM once
    var response = await _llmClient.GenerateAsync(prompt, model: "gpt-4o-mini");

    // 4. Parse and validate
    var codeChanges = ParseCodeChanges(response);
    if (!await ValidateChangesAsync(codeChanges))
        throw new ValidationException("Generated code failed validation");

    // 5. Apply changes
    await ApplyChangesAsync(codeChanges);

    return new ExecutionResult
    {
        Status = ExecutionStatus.Success,
        ChangesApplied = codeChanges.Count,
        TokensUsed = response.TokenCount,
        Duration = stopwatch.Elapsed
    };
}
```

**Metrics**:
- Success Rate: 85%
- Avg Tokens: 2,000
- Avg Cost: $0.02
- Avg Duration: 15s

#### Strategy 2: Iterative

**Use Case**: Medium complexity, may need refinement

**Workflow**:

```csharp
public async Task<ExecutionResult> ExecuteIterativeAsync(CodingTask task)
{
    var context = await BuildContextAsync(task);
    var maxIterations = 3;

    for (int i = 0; i < maxIterations; i++)
    {
        // 1. Generate solution
        var prompt = PromptBuilder.CreateIterativePrompt(task, context, iteration: i);
        var response = await _llmClient.GenerateAsync(prompt);

        // 2. Parse changes
        var codeChanges = ParseCodeChanges(response);

        // 3. Validate (run tests)
        var validationResult = await ValidateChangesAsync(codeChanges);

        if (validationResult.IsSuccess)
        {
            await ApplyChangesAsync(codeChanges);
            return new ExecutionResult { Status = ExecutionStatus.Success };
        }

        // 4. Add validation errors to context for next iteration
        context.AddValidationErrors(validationResult.Errors);
    }

    throw new MaxIterationsExceededException();
}
```

**Metrics**:
- Success Rate: 92%
- Avg Iterations: 1.5
- Avg Tokens: 6,000
- Avg Cost: $0.12
- Avg Duration: 60s

#### Strategy 3: MultiAgent

**Use Case**: Complex tasks requiring specialized expertise

**Architecture**:

```
    ┌─────────────┐
    │ Orchestrator│ (Coordinator)
    └──────┬──────┘
           │
     ┌─────┴─────┬─────────┬─────────┐
     │           │         │         │
┌────▼────┐ ┌───▼────┐ ┌──▼──────┐ ┌▼────────┐
│ Planner │ │ Coder  │ │ Reviewer│ │ Tester  │
│ Agent   │ │ Agent  │ │ Agent   │ │ Agent   │
└─────────┘ └────────┘ └─────────┘ └─────────┘
```

**Workflow**:

```csharp
public async Task<ExecutionResult> ExecuteMultiAgentAsync(CodingTask task)
{
    var agents = new[]
    {
        new PlannerAgent(_llmClient),  // Breaks down task
        new CoderAgent(_llmClient),    // Implements changes
        new ReviewerAgent(_llmClient), // Code review
        new TesterAgent(_llmClient)    // Writes/runs tests
    };

    // 1. Planning phase
    var plan = await agents[0].CreatePlanAsync(task);

    // 2. Implementation phase (parallel where possible)
    var implementationTasks = plan.Steps.Select(step =>
        agents[1].ImplementStepAsync(step)
    );
    var implementations = await Task.WhenAll(implementationTasks);

    // 3. Review phase
    var reviews = await agents[2].ReviewChangesAsync(implementations);

    // 4. Apply approved changes
    var approvedChanges = reviews.Where(r => r.IsApproved).Select(r => r.Changes);
    await ApplyChangesAsync(approvedChanges);

    // 5. Testing phase
    var testResults = await agents[3].RunTestsAsync();

    return new ExecutionResult
    {
        Status = testResults.AllPassed ? ExecutionStatus.Success : ExecutionStatus.Failed,
        AgentInvocations = agents.Length,
        TotalTokens = agents.Sum(a => a.TokensUsed)
    };
}
```

**Metrics**:
- Success Rate: 95%
- Avg Agents: 4
- Avg Tokens: 20,000
- Avg Cost: $0.80
- Avg Duration: 300s

#### Strategy 4: HybridExecution (Ensemble)

**Use Case**: Critical/complex tasks requiring highest accuracy

**Approach**: Use multiple models, ensemble voting

```csharp
public async Task<ExecutionResult> ExecuteHybridAsync(CodingTask task)
{
    var models = new[] { "gpt-4o", "claude-3.5-sonnet", "gemini-1.5-pro" };

    // 1. Generate solutions from multiple models in parallel
    var tasks = models.Select(model =>
        GenerateSolutionAsync(task, model)
    );
    var solutions = await Task.WhenAll(tasks);

    // 2. Score each solution
    var scoredSolutions = await Task.WhenAll(
        solutions.Select(sol => ScoreSolutionAsync(sol))
    );

    // 3. Ensemble voting (weighted by model confidence)
    var bestSolution = EnsembleVoting(scoredSolutions);

    // 4. Apply best solution
    await ApplyChangesAsync(bestSolution.CodeChanges);

    return new ExecutionResult
    {
        Status = ExecutionStatus.Success,
        ModelsUsed = models.Length,
        SelectedModel = bestSolution.Model,
        EnsembleConfidence = bestSolution.Confidence
    };
}
```

**Metrics**:
- Success Rate: 98%
- Avg Models: 3
- Avg Tokens: 35,000
- Avg Cost: $1.50
- Avg Duration: 480s

### 3. SAGA Pattern for Distributed Transactions

**Problem**: Complex workflows span multiple services (GitHub, Browser, CI/CD)

**Solution**: SAGA pattern with compensating transactions

```csharp
public async Task<WorkflowResult> ExecuteTaskWorkflowAsync(CodingTask task)
{
    var saga = new Saga();

    try
    {
        // Step 1: Create GitHub branch
        var branch = await saga.ExecuteAsync(
            forward: () => _githubService.CreateBranchAsync(task.Id),
            compensate: (br) => _githubService.DeleteBranchAsync(br.Name)
        );

        // Step 2: Generate code changes
        var changes = await saga.ExecuteAsync(
            forward: () => ExecuteStrategyAsync(task),
            compensate: (ch) => RollbackChangesAsync(ch)
        );

        // Step 3: Create pull request
        var pr = await saga.ExecuteAsync(
            forward: () => _githubService.CreatePullRequestAsync(branch, changes),
            compensate: (p) => _githubService.ClosePullRequestAsync(p.Number)
        );

        // Step 4: Run CI/CD tests
        var ciResult = await _cicdService.TriggerBuildAsync(pr);

        if (!ciResult.IsSuccess)
        {
            // Compensate all previous steps
            await saga.CompensateAsync();
            throw new WorkflowException("CI/CD tests failed");
        }

        return new WorkflowResult { Status = WorkflowStatus.Success, PullRequest = pr };
    }
    catch (Exception ex)
    {
        await saga.CompensateAsync();
        throw new WorkflowException("Workflow failed, compensating transactions", ex);
    }
}
```

### 4. Circuit Breaker Pattern

**Problem**: Downstream service failures should not cascade

**Solution**: Polly circuit breaker with exponential backoff

```csharp
services.AddHttpClient<IMLClassifierClient, MLClassifierClient>()
    .AddPolicyHandler(GetRetryPolicy())
    .AddPolicyHandler(GetCircuitBreakerPolicy());

static IAsyncPolicy<HttpResponseMessage> GetRetryPolicy()
{
    return HttpPolicyExtensions
        .HandleTransientHttpError()
        .WaitAndRetryAsync(
            retryCount: 3,
            sleepDurationProvider: retryAttempt =>
                TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)),
            onRetry: (outcome, timespan, retryAttempt, context) =>
            {
                Log.Warning($"Retry {retryAttempt} after {timespan}");
            }
        );
}

static IAsyncPolicy<HttpResponseMessage> GetCircuitBreakerPolicy()
{
    return HttpPolicyExtensions
        .HandleTransientHttpError()
        .CircuitBreakerAsync(
            handledEventsAllowedBeforeBreaking: 5,
            durationOfBreak: TimeSpan.FromSeconds(30),
            onBreak: (outcome, duration) =>
            {
                Log.Error($"Circuit breaker opened for {duration}");
            },
            onReset: () =>
            {
                Log.Information("Circuit breaker reset");
            }
        );
}
```

---

## Self-Learning System

### 1. Learning Architecture

```
┌──────────────────────────────────────────────────────────────┐
│                     Self-Learning Pipeline                    │
└───────────┬──────────────────────────────────────────────────┘
            │
    ┌───────▼────────┐
    │ Data Collection│ (Every task execution)
    └───────┬────────┘
            │
    ┌───────▼────────┐
    │ Feature Extraction│ (PostgreSQL + Redis)
    └───────┬────────┘
            │
    ┌───────▼────────┐
    │ Model Training │ (XGBoost, batch every 1K samples)
    └───────┬────────┘
            │
    ┌───────▼────────┐
    │ Model Validation│ (95%+ accuracy required)
    └───────┬────────┘
            │
    ┌───────▼────────┐
    │ Model Deployment│ (A/B testing, gradual rollout)
    └───────┬────────┘
            │
    ┌───────▼────────┐
    │ Inference      │ (ONNX Runtime, <50ms)
    └────────────────┘
```

### 2. Training Data Collection

**Event-Driven Collection**:

```csharp
// Publish event after task completion
await _eventBus.PublishAsync(new TaskCompletedEvent
{
    TaskId = task.Id,
    TaskType = task.Type,
    Complexity = task.Complexity,
    Strategy = executionResult.Strategy,
    ModelUsed = executionResult.ModelUsed,
    TokensUsed = executionResult.TokensUsed,
    Duration = executionResult.Duration,
    Success = executionResult.Status == ExecutionStatus.Success,
    ErrorMessage = executionResult.ErrorMessage,
    ChangesApplied = executionResult.ChangesApplied,
    TestsPassed = executionResult.TestResults?.PassedCount ?? 0,
    TestsFailed = executionResult.TestResults?.FailedCount ?? 0
});
```

**ML Classifier Listener**:

```python
@consumer("TaskCompletedEvent")
async def collect_training_sample(event: TaskCompletedEvent):
    """Store task execution as training sample"""

    # 1. Create training sample
    sample = TrainingSample(
        task_id=event.task_id,
        task_description=event.task_description,
        predicted_type=event.task_type,
        predicted_complexity=event.complexity,
        actual_type=event.task_type,  # Ground truth from execution
        actual_complexity=calculate_actual_complexity(event),
        success=event.success,
        tokens_used=event.tokens_used,
        duration_ms=event.duration.total_seconds() * 1000,
        model_used=event.model_used,
        strategy_used=event.strategy,
        timestamp=datetime.utcnow()
    )

    # 2. Store in PostgreSQL
    async with db_pool.acquire() as conn:
        await conn.execute("""
            INSERT INTO training_samples
            (task_id, task_description, predicted_type, ...)
            VALUES ($1, $2, $3, ...)
        """, sample.task_id, sample.task_description, ...)

    # 3. Check if retraining needed
    sample_count = await get_sample_count_since_last_training()
    if sample_count >= 1000:
        await trigger_retraining()
```

### 3. Confidence Learning System

**Adaptive Multipliers**:

```python
# Keyword-based confidence adjustment
DEFAULT_MULTIPLIERS = {
    0: 0.4,  # No keyword matches → low confidence
    1: 0.6,  # 1 match → medium-low confidence
    2: 0.8,  # 2 matches → medium-high confidence
    3: 1.0   # 3+ matches → full confidence
}

async def learn_optimal_multipliers():
    """Continuously optimize multipliers based on accuracy"""

    # Fetch recent feedback (last 1000 classifications)
    feedback = await db.fetch("""
        SELECT keyword_count, adjusted_confidence, was_correct
        FROM ml_classification_feedback
        WHERE timestamp > NOW() - INTERVAL '7 days'
        ORDER BY timestamp DESC
        LIMIT 1000
    """)

    # Group by keyword count
    accuracy_by_keywords = defaultdict(list)
    for row in feedback:
        accuracy_by_keywords[row['keyword_count']].append(
            (row['adjusted_confidence'], row['was_correct'])
        )

    # Calculate optimal multipliers
    updated_multipliers = {}
    for keyword_count, results in accuracy_by_keywords.items():
        # Find multiplier that maximizes accuracy
        confidences = [r[0] for r in results]
        correct = [r[1] for r in results]

        avg_confidence_when_correct = np.mean([c for c, cor in zip(confidences, correct) if cor])
        avg_confidence_when_wrong = np.mean([c for c, cor in zip(confidences, correct) if not cor])

        # Adjust multiplier to increase confidence when correct, decrease when wrong
        current_multiplier = DEFAULT_MULTIPLIERS.get(keyword_count, 1.0)
        if avg_confidence_when_correct > avg_confidence_when_wrong:
            # Model is calibrated well, small adjustment
            new_multiplier = current_multiplier + 0.02
        else:
            # Model needs recalibration
            new_multiplier = current_multiplier - 0.05

        # Bound multipliers to [0.3, 1.2]
        updated_multipliers[keyword_count] = max(0.3, min(1.2, new_multiplier))

    # Cache in Redis (24 hour TTL)
    await redis.set(
        "ml:confidence_multipliers",
        json.dumps(updated_multipliers),
        ex=86400
    )

    return updated_multipliers
```

---

## Model Selection & Routing

### 1. Dynamic Model Selection

**Selection Criteria**:

```csharp
public string SelectOptimalModel(CodingTask task, RequestClassification classification)
{
    // 1. Cost-based selection for simple tasks
    if (classification.Complexity == TaskComplexity.Simple)
        return "gpt-4o-mini";  // $0.15/M tokens

    // 2. Accuracy-based selection for medium tasks
    if (classification.Complexity == TaskComplexity.Medium)
    {
        // Check user budget
        if (task.User.PlanType == PlanType.Free)
            return "gpt-4o-mini";
        else
            return "gpt-4o";  // $2.50/M tokens
    }

    // 3. Performance-based selection for complex tasks
    if (classification.Complexity == TaskComplexity.Complex)
    {
        // Query historical success rates
        var modelPerformance = _metricsRepo.GetModelPerformanceForTaskType(
            taskType: classification.Type
        );

        // Select best-performing model
        return modelPerformance.OrderByDescending(m => m.SuccessRate).First().ModelName;
    }

    // 4. Ensemble for epic tasks
    return "ensemble:gpt-4o+claude-3.5-sonnet+gemini-1.5-pro";
}
```

### 2. A/B Testing Framework

```csharp
public async Task<RoutingDecision> RouteWithABTestAsync(CodingTask task)
{
    var abTestId = GetActiveABTest();

    if (abTestId != null)
    {
        // 10% canary traffic to new model
        if (ShouldUseCanaryModel(task.Id, percentage: 10))
        {
            return new RoutingDecision
            {
                Model = "gpt-4o-turbo-canary",
                Reason = $"A/B test: {abTestId}",
                IsCanary = true
            };
        }
    }

    // Default routing
    return RouteToStableModel(task);
}

// Measure canary performance
await _metricsRepo.RecordABTestResultAsync(new ABTestResult
{
    TestId = abTestId,
    TaskId = task.Id,
    Variant = "canary",
    SuccessRate = executionResult.Success,
    Duration = executionResult.Duration,
    TokensUsed = executionResult.TokensUsed,
    UserSatisfaction = await GetUserFeedbackAsync(task.Id)
});
```

### 3. Cost Optimization

**Budget Management**:

```csharp
public class CostOptimizer
{
    public async Task<RoutingDecision> OptimizeForCostAsync(
        CodingTask task,
        decimal monthlyBudget,
        decimal spentThisMonth
    )
    {
        var remainingBudget = monthlyBudget - spentThisMonth;
        var daysLeftInMonth = DateTime.DaysInMonth(DateTime.Now.Year, DateTime.Now.Month)
                              - DateTime.Now.Day;
        var dailyBudget = remainingBudget / daysLeftInMonth;

        if (dailyBudget < 1.0m)
        {
            // Budget depleted, use cheapest model
            return new RoutingDecision
            {
                Model = "gpt-4o-mini",
                Reason = "Cost optimization: budget depleted"
            };
        }

        // Predict task cost
        var estimatedCost = EstimateTaskCost(task);

        if (estimatedCost > dailyBudget * 0.1m)
        {
            // Task would consume >10% of daily budget, downgrade
            return new RoutingDecision
            {
                Model = "gpt-4o",  // Mid-tier instead of gpt-4
                Reason = $"Cost optimization: estimated ${estimatedCost:F2}"
            };
        }

        // Within budget, use optimal model
        return RouteToOptimalModel(task);
    }
}
```

---

## Feedback Loops

### 1. Real-Time Feedback

**User Feedback UI**:

```typescript
// Angular component
export class ChatMessageComponent {
  provideFeedback(messageId: string, helpful: boolean) {
    this.chatService.submitFeedback({
      messageId,
      helpful,
      timestamp: new Date()
    }).subscribe(() => {
      this.feedbackSubmitted = true;
    });
  }
}
```

**Backend Processing**:

```csharp
[HttpPost("feedback")]
public async Task<IActionResult> SubmitFeedback([FromBody] FeedbackRequest request)
{
    // Store feedback
    await _feedbackRepo.CreateAsync(new ChatFeedback
    {
        MessageId = request.MessageId,
        UserId = User.GetUserId(),
        IsHelpful = request.Helpful,
        Comments = request.Comments,
        Timestamp = DateTime.UtcNow
    });

    // Publish event for ML learning
    await _eventBus.PublishAsync(new FeedbackReceivedEvent
    {
        MessageId = request.MessageId,
        IsHelpful = request.Helpful,
        ModelUsed = await GetModelUsedForMessage(request.MessageId)
    });

    return Ok();
}
```

**ML Processing**:

```python
@consumer("FeedbackReceivedEvent")
async def process_user_feedback(event: FeedbackReceivedEvent):
    """Update model weights based on user feedback"""

    # Fetch original classification
    classification = await db.fetchrow("""
        SELECT * FROM classifications WHERE message_id = $1
    """, event.message_id)

    # Update model performance metrics
    await db.execute("""
        INSERT INTO model_feedback (model_name, helpful, timestamp)
        VALUES ($1, $2, $3)
    """, event.model_used, event.is_helpful, datetime.utcnow())

    # If negative feedback, analyze what went wrong
    if not event.is_helpful:
        await analyze_failure_pattern(classification, event)
```

### 2. Automated Feedback (CI/CD Results)

```csharp
// CI/CD Monitor publishes build results
await _eventBus.PublishAsync(new BuildCompletedEvent
{
    PullRequestId = pr.Id,
    TaskId = task.Id,
    BuildStatus = buildResult.Status,
    TestsPassed = buildResult.TestResults.PassedCount,
    TestsFailed = buildResult.TestResults.FailedCount,
    BuildDuration = buildResult.Duration
});

// Orchestrator consumes event
@consumer("BuildCompletedEvent")
async def update_task_quality_score(event: BuildCompletedEvent):
    """Use CI/CD results as automated quality signal"""

    quality_score = calculate_quality_score(
        tests_passed=event.tests_passed,
        tests_failed=event.tests_failed,
        build_duration=event.build_duration
    )

    # Update training data with quality score
    await db.execute("""
        UPDATE training_samples
        SET quality_score = $1, automated_feedback = TRUE
        WHERE task_id = $2
    """, quality_score, event.task_id)

    # If quality is poor, trigger review
    if quality_score < 0.7:
        await trigger_human_review(event.task_id)
```

---

## Performance Targets

### 1. ML Classifier SLAs

| Metric | Target | Measurement |
|--------|--------|-------------|
| **Latency (p50)** | < 20ms | Median response time |
| **Latency (p95)** | < 50ms | 95th percentile |
| **Latency (p99)** | < 100ms | 99th percentile |
| **Accuracy** | > 93% | Overall classification accuracy |
| **Availability** | 99.9% | Uptime (8.76 hours downtime/year) |
| **Throughput** | 1000 req/s | Peak classification requests |

### 2. Orchestration Service SLAs

| Metric | Target | Measurement |
|--------|--------|-------------|
| **Task Success Rate** | > 90% | Tasks completed without errors |
| **Simple Task Duration** | < 30s | SingleShot strategy |
| **Medium Task Duration** | < 120s | Iterative strategy |
| **Complex Task Duration** | < 600s | MultiAgent strategy |
| **Cost per Task** | < $0.50 | Average across all strategies |
| **Concurrent Tasks** | 100 | Max simultaneous executions |

### 3. Learning System Targets

| Metric | Target | Measurement |
|--------|--------|-------------|
| **Training Frequency** | Weekly | Model retraining cadence |
| **Training Duration** | < 10 min | From data fetch to deployment |
| **Model Accuracy Improvement** | +2% per month | Accuracy gains from learning |
| **Feedback Collection Rate** | > 20% | % of tasks with user feedback |
| **Feedback Processing Latency** | < 1s | Time to update metrics |

---

## Migration from v1.0

### 1. Current State (v1.0)

**Pain Points**:
- ❌ Rule-based classification only (static, 85% accuracy)
- ❌ No self-learning capability
- ❌ Single execution strategy (one-size-fits-all)
- ❌ Monolithic orchestration (hard to scale)
- ❌ No cost optimization
- ❌ Limited observability

### 2. Migration Strategy

**Phase 1: Parallel Run (Weeks 21-22)**

```csharp
public async Task<ClassificationResult> ClassifyWithMigration(CodingTask task)
{
    // Call both v1.0 (rule-based) and v2.0 (ML) classifiers
    var v1Result = await _v1Classifier.ClassifyAsync(task);
    var v2Result = await _v2Classifier.ClassifyAsync(task);

    // Log differences for analysis
    if (v1Result.Type != v2Result.Type)
    {
        await _migrationMetrics.RecordDivergenceAsync(new ClassificationDivergence
        {
            TaskId = task.Id,
            V1Type = v1Result.Type,
            V2Type = v2Result.Type,
            V1Confidence = v1Result.Confidence,
            V2Confidence = v2Result.Confidence
        });
    }

    // Use v1.0 for now (safe), but collect v2.0 data
    return v1Result;
}
```

**Phase 2: Shadow Mode (Week 22)**

```csharp
// Start using v2.0 for 10% of traffic (canary)
public async Task<ClassificationResult> ClassifyWithShadow(CodingTask task)
{
    if (ShouldUseV2(task.Id, percentage: 10))
    {
        var result = await _v2Classifier.ClassifyAsync(task);
        result.IsCanary = true;
        return result;
    }

    return await _v1Classifier.ClassifyAsync(task);
}
```

**Phase 3: Full Cutover (Week 23)**

```csharp
// 100% traffic to v2.0, keep v1.0 as fallback
public async Task<ClassificationResult> ClassifyWithFallback(CodingTask task)
{
    try
    {
        return await _v2Classifier.ClassifyAsync(task);
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "v2.0 classifier failed, falling back to v1.0");
        await _metricsCollector.RecordFallbackAsync();
        return await _v1Classifier.ClassifyAsync(task);
    }
}
```

**Phase 4: Decommission v1.0 (Week 24)**

- Remove v1.0 code after 1 week of stable v2.0 operation
- Archive v1.0 classification data for historical analysis
- Update documentation to remove v1.0 references

### 3. Rollback Plan

**Trigger Conditions**:
- v2.0 accuracy drops below 88% (worse than v1.0)
- v2.0 latency exceeds 200ms (2x target)
- Critical production bug in ML service

**Rollback Procedure**:

```bash
# 1. Flip feature flag
curl -X POST http://gateway:5000/admin/feature-flags \
  -H "Content-Type: application/json" \
  -d '{"flag": "UseMlClassifierV2", "enabled": false}'

# 2. Drain v2.0 traffic (30 second grace period)
kubectl scale deployment ml-classifier --replicas=0

# 3. Scale up v1.0 fallback
kubectl scale deployment rule-based-classifier --replicas=3

# 4. Verify traffic restored
kubectl logs -f deployment/gateway | grep "classifier_version=v1.0"

# 5. Investigate and fix v2.0 issue
```

---

## External API Integration

### 1. Ollama-Compatible Endpoints

**Purpose**: Allow external tools (VS Code Copilot, Continue, AI Toolkit) to consume the orchestrator as an Ollama model

**Endpoints**:

```http
# Text Generation
POST /api/generate
Content-Type: application/json

{
  "model": "coding-agent-orchestrator",
  "prompt": "Implement user authentication",
  "stream": true,
  "options": {
    "temperature": 0.7,
    "num_predict": 2000
  }
}

# Chat Completion  
POST /api/chat
Content-Type: application/json

{
  "model": "coding-agent-orchestrator",
  "messages": [
    {"role": "user", "content": "Fix the login bug"}
  ],
  "stream": true
}

# List Models
GET /api/tags

Response:
{
  "models": [
    {
      "name": "coding-agent-orchestrator:latest",
      "modified_at": "2025-10-24T12:00:00Z",
      "size": 1024000
    }
  ]
}

# Model Info
POST /api/show
{
  "name": "coding-agent-orchestrator"
}
```

**Streaming Format**: NDJSON (newline-delimited JSON)

```json
{"model":"coding-agent-orchestrator","response":"Analyzing","done":false}
{"model":"coding-agent-orchestrator","response":" the","done":false}
{"model":"coding-agent-orchestrator","response":" issue...","done":true}
```

**Key Features**:
- ✅ **Real-time streaming**: Tokens streamed as generated (not simulated)
- ✅ **Session tracking**: IP-based session management for unauthorized access
- ✅ **snake_case JSON**: Full Ollama API compliance
- ✅ **No authentication required**: Open for IDE integration

### 2. OpenAI-Compatible Endpoint

**Purpose**: Support tools expecting OpenAI API format (Continue IDE extension)

**Endpoint**:

```http
POST /v1/chat/completions
Content-Type: application/json

{
  "model": "coding-agent-orchestrator",
  "messages": [
    {"role": "system", "content": "You are a coding assistant"},
    {"role": "user", "content": "Write a function to sort an array"}
  ],
  "stream": true,
  "temperature": 0.7,
  "max_tokens": 2000
}
```

**Streaming Format**: Server-Sent Events (SSE)

```
data: {"id":"chatcmpl-123","object":"chat.completion.chunk","choices":[{"delta":{"content":"Here"}}]}

data: {"id":"chatcmpl-123","object":"chat.completion.chunk","choices":[{"delta":{"content":" is"}}]}

data: {"id":"chatcmpl-123","object":"chat.completion.chunk","choices":[{"delta":{},"finish_reason":"stop"}]}

data: [DONE]
```

**Key Features**:
- ✅ **OpenAI API compatibility**: Drop-in replacement for `api.openai.com`
- ✅ **SSE streaming**: Server-Sent Events format
- ✅ **Token usage tracking**: Approximate token counts in response

### 3. Integration Examples

#### VS Code Copilot Configuration

```json
// .vscode/settings.json
{
  "github.copilot.advanced": {
    "debug.overrideEngine": "coding-agent-orchestrator",
    "debug.overrideProxyUrl": "http://localhost:5000"
  }
}
```

#### Continue IDE Extension

```json
// ~/.continue/config.json
{
  "models": [
    {
      "title": "Coding Agent",
      "provider": "openai",
      "model": "coding-agent-orchestrator",
      "apiBase": "http://localhost:5000/v1",
      "apiKey": "dummy"
    }
  ]
}
```

#### AI Toolkit (Ollama Mode)

```json
// model-config.json
{
  "models": [
    {
      "name": "coding-agent-orchestrator",
      "endpoint": "http://localhost:5000",
      "provider": "ollama"
    }
  ]
}
```

#### cURL Examples

```bash
# Ollama-style generation
curl -N http://localhost:5000/api/generate \
  -H "Content-Type: application/json" \
  -d '{
    "model": "coding-agent-orchestrator",
    "prompt": "Create a REST API for users",
    "stream": true
  }'

# OpenAI-style chat
curl -N http://localhost:5000/v1/chat/completions \
  -H "Content-Type: application/json" \
  -d '{
    "model": "coding-agent-orchestrator",
    "messages": [{"role": "user", "content": "Hello"}],
    "stream": true
  }'
```

### 4. Implementation Details

**Controller**: `OllamaOrchestratorController.cs`

```csharp
[ApiController]
[Route("")]
[AllowAnonymous] // No auth required for external tools
public class OllamaOrchestratorController : ControllerBase
{
    // Ollama endpoints
    [HttpPost("api/generate")]
    public async Task<IActionResult> Generate([FromBody] OllamaGenerateRequest request) { ... }
    
    [HttpPost("api/chat")]
    public async Task<IActionResult> Chat([FromBody] OllamaChatRequest request) { ... }
    
    [HttpGet("api/tags")]
    public IActionResult ListModels() { ... }
    
    // OpenAI endpoint
    [HttpPost("v1/chat/completions")]
    public async Task<IActionResult> OpenAIChatCompletions([FromBody] JsonElement requestBody) { ... }
}
```

**Benefits**:
1. **Zero Configuration**: External tools work out-of-the-box
2. **Unified Interface**: Single API serves Angular dashboard + external IDEs
3. **Real-time Feedback**: Streaming provides immediate response
4. **Broad Compatibility**: Supports Ollama AND OpenAI clients
5. **No Vendor Lock-in**: Standard APIs, not proprietary

---

## Appendix: Key Files & Components

### .NET Components

| File | Purpose |
|------|---------|
| `CodingAgent.Core/Orchestration/OrchestrationService.cs` | Core orchestration logic |
| `CodingAgent.Core/Orchestration/ExecutionStrategies/` | Strategy implementations |
| `CodingAgent.Core/Interfaces/IOrchestrationLearningService.cs` | Learning interface |
| `CodingAgent.Infrastructure/Services/OrchestrationLearningService.cs` | Learning implementation |
| `CodingAgent.Infrastructure/Services/TaskClassifierAdapter.cs` | ML API client |

### Python Components

| File | Purpose |
|------|---------|
| `agent/classifier.py` | Hybrid classifier (heuristic + ML + LLM) |
| `agent/orchestrator.py` | Python orchestration logic |
| `agent/router.py` | Model selection and routing |
| `ml_api_service.py` | FastAPI ML service |
| `agent/ml/training_pipeline.py` | Model training |
| `agent/ml/feature_engineering.py` | Feature extraction |

### Database Schema

| Table | Purpose |
|-------|---------|
| `training_samples` | Historical task execution data |
| `ml_models` | Model registry and versioning |
| `ml_classification_feedback` | User feedback for learning |
| `model_metrics` | Performance metrics per model |
| `orchestration_feedback` | Task execution outcomes |

---

**Next Document**: [03-DATA-MODELS.md](./03-DATA-MODELS.md) - Complete database schemas and entity relationships

**Document History**:
- v1.0 (2025-10-10): Initial draft
- v2.0 (2025-10-24): Added microservices architecture, migration strategy, performance targets
