## Status

Accepted

## Context

The HTTP entry point uses ASP.NET Core controllers with attribute routing and `Task<IActionResult>` actions. Controllers such as `WorldsController` and `SimulationController` inject one service dependency, accept route/body data, perform only lightweight imperative checks, call one application service, and translate `Result` values into HTTP responses. Request models are declared as small `record` types in the same file as the controller instead of being moved into a separate contracts assembly.

## Decision

Keep the HTTP layer controller-based and thin:

- use MVC controllers rather than Minimal APIs;
- keep request records next to controllers;
- delegate business orchestration to application services;
- translate service results into HTTP responses in the controller.

## Consequences

- HTTP concerns stay easy to trace and test separately from business rules.
- Controllers remain small and predictable instead of accumulating orchestration logic.
- Request contracts are close to their endpoints, which reduces indirection.
- Validation and response mapping stay imperative rather than centralized in a pipeline.

## Alternatives considered

- Minimal APIs with endpoint handlers.
- Controllers that contain richer business logic.
- CQRS or MediatR-based endpoint handlers with separate request/handler objects.
