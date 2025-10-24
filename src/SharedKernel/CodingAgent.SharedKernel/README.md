# CodingAgent.SharedKernel

[![NuGet Version](https://img.shields.io/badge/nuget-v2.0.0-blue)](https://www.nuget.org)
[![.NET Version](https://img.shields.io/badge/.NET-9.0-purple)](https://dotnet.microsoft.com)

Shared kernel library for the CodingAgent microservices platform. This package provides common domain primitives, value objects, events, abstractions, and result patterns used across all microservices.

## Installation

```bash
dotnet add package CodingAgent.SharedKernel
```

## Features

- **Domain Primitives**: Base classes for entities and value objects following DDD principles
- **Value Objects**: Common enums for task types, complexity levels, and execution strategies
- **Domain Events**: Event contracts for inter-service communication
- **Abstractions**: Repository, Unit of Work, and Event Publisher interfaces
- **Result Pattern**: Type-safe error handling without exceptions
- **Custom Exceptions**: Domain-specific exception hierarchy

## Usage Examples

### 1. Domain Entities

Create domain entities by inheriting from `BaseEntity`:

```csharp
using CodingAgent.SharedKernel.Domain.Entities;

public class Task : BaseEntity
{
    public string Title { get; private set; }
    public string Description { get; private set; }
    public TaskStatus Status { get; private set; }

    public Task(string title, string description)
    {
        Title = title;
        Description = description;
        Status = TaskStatus.Pending;
    }

    public void Complete()
    {
        Status = TaskStatus.Completed;
        MarkAsUpdated(); // Updates the UpdatedAt timestamp
    }
}
```

**BaseEntity provides**:
- `Id` (Guid): Unique identifier
- `CreatedAt` (DateTime): Creation timestamp
- `UpdatedAt` (DateTime): Last update timestamp
- Entity equality based on Id
- `MarkAsUpdated()` helper method

### 2. Value Objects

Create immutable value objects by inheriting from `ValueObject`:

```csharp
using CodingAgent.SharedKernel.Domain.ValueObjects;

public class Money : ValueObject
{
    public decimal Amount { get; }
    public string Currency { get; }

    public Money(decimal amount, string currency)
    {
        Amount = amount;
        Currency = currency;
    }

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Amount;
        yield return Currency;
    }
}

// Usage
var price1 = new Money(100m, "USD");
var price2 = new Money(100m, "USD");
var areEqual = price1 == price2; // true
```

### 3. Predefined Value Objects

Use built-in enums for common concepts:

```csharp
using CodingAgent.SharedKernel.Domain.ValueObjects;

// Task Type
var taskType = TaskType.BugFix; // BugFix, Feature, Refactor, Documentation, Test, Deployment

// Task Complexity
var complexity = TaskComplexity.Medium; // Simple, Medium, Complex, Epic

// Execution Strategy
var strategy = ExecutionStrategy.Iterative; // SingleShot, Iterative, MultiAgent, HybridExecution
```

### 4. Domain Events

Publish and consume domain events:

```csharp
using CodingAgent.SharedKernel.Domain.Events;
using CodingAgent.SharedKernel.Domain.ValueObjects;

// Create event
var taskCompletedEvent = new TaskCompletedEvent
{
    TaskId = Guid.NewGuid(),
    TaskType = TaskType.Feature,
    Complexity = TaskComplexity.Medium,
    Strategy = ExecutionStrategy.Iterative,
    Success = true,
    TokensUsed = 5000,
    CostUsd = 0.15m,
    Duration = TimeSpan.FromMinutes(5)
};

// Publish via event bus
await eventPublisher.PublishAsync(taskCompletedEvent);
```

**Available Events**:
- `TaskCreatedEvent`
- `TaskCompletedEvent`
- `MessageSentEvent`
- `ConversationCreatedEvent`

### 5. Result Pattern

Handle success and failure without throwing exceptions:

```csharp
using CodingAgent.SharedKernel.Results;

// Success case
public Result<User> GetUser(Guid userId)
{
    var user = FindUser(userId);
    if (user == null)
        return Result<User>.Failure(Error.NotFound("User.NotFound", $"User {userId} not found"));
    
    return Result<User>.Success(user);
}

// Usage with Match
var result = GetUser(userId);
var response = result.Match(
    onSuccess: user => $"Found user: {user.Name}",
    onFailure: error => $"Error: {error.Message}"
);

// Or check IsSuccess
if (result.IsSuccess)
{
    var user = result.Value;
    // Use user
}
else
{
    var error = result.Error;
    // Handle error
}
```

**Result without return value**:

```csharp
public Result DeleteUser(Guid userId)
{
    if (!UserExists(userId))
        return Result.Failure("User.NotFound", "User not found");
    
    // Delete user
    return Result.Success();
}
```

### 6. Error Handling

Create typed errors:

```csharp
using CodingAgent.SharedKernel.Results;

// Validation error
var validationError = Error.Validation(
    "Task.InvalidTitle",
    "Task title cannot be empty"
);

// Not found error
var notFoundError = Error.NotFound(
    "Task.NotFound",
    $"Task with ID {taskId} was not found"
);

// With metadata
var errorWithMetadata = new Error(
    "Task.ExecutionFailed",
    "Task execution failed",
    ErrorType.Failure,
    new Dictionary<string, object>
    {
        { "TaskId", taskId },
        { "AttemptNumber", 3 }
    }
);
```

### 7. Validation Errors

Handle validation errors with field-level details:

```csharp
using CodingAgent.SharedKernel.Results;

// Single field error
var validationError = ValidationError.ForProperty("Email", "Email is required");

// Multiple errors
var errors = new Dictionary<string, string[]>
{
    { "Email", new[] { "Email is required", "Email format is invalid" } },
    { "Password", new[] { "Password must be at least 8 characters" } }
};
var validationError = ValidationError.FromErrors(errors);

// Use in Result
return Result<User>.Failure(validationError);
```

### 8. Custom Exceptions

Use domain exceptions when exceptions are necessary:

```csharp
using CodingAgent.SharedKernel.Exceptions;

// Not found exception
throw new NotFoundException("Task", taskId);

// Validation exception
var errors = new Dictionary<string, string[]>
{
    { "Title", new[] { "Title is required" } }
};
throw new ValidationException(errors);

// Domain exception
throw new DomainException("Cannot complete a cancelled task");
```

### 9. Repository Pattern

Implement repositories using the provided interface:

```csharp
using CodingAgent.SharedKernel.Abstractions;
using CodingAgent.SharedKernel.Domain.Entities;

public class TaskRepository : IRepository<Task>
{
    private readonly DbContext _context;

    public TaskRepository(DbContext context)
    {
        _context = context;
    }

    public async Task<Task?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _context.Tasks.FindAsync(new object[] { id }, cancellationToken);
    }

    public async Task<Task> AddAsync(Task entity, CancellationToken cancellationToken = default)
    {
        await _context.Tasks.AddAsync(entity, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);
        return entity;
    }

    // Implement other methods...
}
```

### 10. Event Publishing

Publish domain events to a message bus:

```csharp
using CodingAgent.SharedKernel.Abstractions;
using CodingAgent.SharedKernel.Domain.Events;

public class TaskService
{
    private readonly IEventPublisher _eventPublisher;

    public TaskService(IEventPublisher eventPublisher)
    {
        _eventPublisher = eventPublisher;
    }

    public async Task CompleteTask(Guid taskId)
    {
        // Complete task logic...

        // Publish event
        var taskCompletedEvent = new TaskCompletedEvent
        {
            TaskId = taskId,
            TaskType = TaskType.Feature,
            Complexity = TaskComplexity.Medium,
            Strategy = ExecutionStrategy.Iterative,
            Success = true,
            TokensUsed = 5000,
            CostUsd = 0.15m,
            Duration = TimeSpan.FromMinutes(5)
        };

        await _eventPublisher.PublishAsync(taskCompletedEvent);
    }
}
```

## Package Structure

```
CodingAgent.SharedKernel/
├── Domain/
│   ├── Entities/
│   │   └── BaseEntity.cs                    # Base class for all entities
│   ├── ValueObjects/
│   │   ├── ValueObject.cs                   # Base class for value objects
│   │   ├── TaskType.cs                      # Task type enum
│   │   ├── TaskComplexity.cs                # Task complexity enum
│   │   └── ExecutionStrategy.cs             # Execution strategy enum
│   └── Events/
│       ├── IDomainEvent.cs                  # Domain event interface
│       ├── TaskCreatedEvent.cs              # Task created event
│       ├── TaskCompletedEvent.cs            # Task completed event
│       ├── MessageSentEvent.cs              # Message sent event
│       └── ConversationCreatedEvent.cs      # Conversation created event
├── Abstractions/
│   ├── IRepository.cs                       # Generic repository interface
│   ├── IUnitOfWork.cs                       # Unit of work interface
│   └── IEventPublisher.cs                   # Event publisher interface
├── Results/
│   ├── Result.cs                            # Result type for operations
│   ├── Error.cs                             # Error details
│   └── ValidationError.cs                   # Validation error details
└── Exceptions/
    ├── DomainException.cs                   # Base domain exception
    ├── NotFoundException.cs                 # Not found exception
    └── ValidationException.cs               # Validation exception
```

## Testing

This package includes comprehensive unit tests with 85%+ code coverage. To run tests:

```bash
cd src/SharedKernel/CodingAgent.SharedKernel.Tests
dotnet test
```

## Design Patterns

This library implements several design patterns:

- **Domain-Driven Design (DDD)**: Entities, value objects, and domain events
- **Repository Pattern**: Abstract data access
- **Unit of Work Pattern**: Manage transactions
- **Result Pattern**: Type-safe error handling
- **Event Sourcing**: Domain events for state changes

## Contributing

See [CONTRIBUTING.md](../../.github/CONTRIBUTING.md) for contribution guidelines.

## License

This project is licensed under the MIT License - see [LICENSE](../../LICENSE) file for details.

## Related Packages

- `CodingAgent.Services.Chat` - Chat service implementation
- `CodingAgent.Services.Orchestration` - Task orchestration service
- `CodingAgent.Services.GitHub` - GitHub integration service

## Support

For issues and questions:
- GitHub Issues: [https://github.com/JustAGameZA/coding-agent/issues](https://github.com/JustAGameZA/coding-agent/issues)
- Documentation: [https://github.com/JustAGameZA/coding-agent/docs](https://github.com/JustAGameZA/coding-agent/docs)
