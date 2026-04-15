# Codery.Mediator

Lightweight, high-performance mediator library for .NET. A focused replacement for MediatR, built for Codery projects.

## Features

- **Request/Response** dispatching via `IRequest<TResponse>` and `IRequestHandler<TRequest, TResponse>`
- **Notifications** (multicast) via `INotification` and `INotificationHandler<TNotification>`
- **Pipeline behaviors** for cross-cutting concerns (logging, validation, etc.)
- **Zero per-call reflection** — cached generic wrapper pattern eliminates reflection overhead after first use
- **Two-package design** — `Codery.Mediator.Abstractions` has zero dependencies; `Codery.Mediator` adds DI support
- Multi-targeting: `net10.0` + `netstandard2.0`

## Quick Start

```bash
dotnet add package Codery.Mediator
```

Register in your DI container:

```csharp
builder.Services.AddCoderyMediator(typeof(Program).Assembly);
```

With pipeline behaviors:

```csharp
builder.Services.AddCoderyMediator(
    opts => opts.AddOpenBehavior(typeof(LoggingBehavior<,>)),
    typeof(Program).Assembly);
```

### Define a request and handler

```csharp
public record GetUser(int Id) : IRequest<UserDto>;

public class GetUserHandler : IRequestHandler<GetUser, UserDto>
{
    public Task<UserDto> Handle(GetUser request, CancellationToken cancellationToken)
    {
        // ...
        return Task.FromResult(new UserDto(request.Id, "Alice"));
    }
}
```

### Send requests

```csharp
app.MapGet("/users/{id}", async (int id, ISender sender) =>
    await sender.Send(new GetUser(id)));
```

### Notifications

```csharp
public record OrderPlaced(string OrderId) : INotification;

public class SendEmailHandler : INotificationHandler<OrderPlaced>
{
    public Task Handle(OrderPlaced notification, CancellationToken cancellationToken)
    {
        // Send email...
        return Task.CompletedTask;
    }
}

// Publish to all handlers
await publisher.Publish(new OrderPlaced("ORD-123"));
```

## Migration from MediatR

1. Replace `MediatR` NuGet with `Codery.Mediator`
2. Find & replace: `using MediatR;` → `using Codery.Mediator;`
3. Replace `services.AddMediatR(cfg => ...)` → `services.AddCoderyMediator(typeof(Program).Assembly)`
4. `IRequest` (void) handlers use `IRequestHandler<TRequest, Unit>` (no shorthand `IRequestHandler<TRequest>`)
5. Pipeline behaviors: `opts.AddOpenBehavior(typeof(MyBehavior<,>))`

## Packages

| Package | Description |
|---------|-------------|
| `Codery.Mediator.Abstractions` | Interfaces only. Zero dependencies. Reference from handler projects. |
| `Codery.Mediator` | Implementation + DI registration. Reference from startup/host project. |

## License

[MIT](LICENSE)
