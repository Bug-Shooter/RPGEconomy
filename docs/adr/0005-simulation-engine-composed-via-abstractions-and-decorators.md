## Status

Accepted

## Context

`ISimulationExecutor` is defined in `RPGEconomy.Application`, implemented by `SimulationEngine` in `RPGEconomy.Simulation`, and wired by `RPGEconomy.API` through `AddSimulation()`. The executor depends on application-level repository abstractions, loads a simulation context, runs production and market ticks in memory, and persists selected aggregates afterward. Infrastructure decorates the executor with `LoggingSimulationDecorator` and `TransactionSimulationDecorator` using Scrutor, so cross-cutting concerns are applied outside the core simulation implementation.

## Decision

Keep simulation as a dedicated runtime component in `RPGEconomy.Simulation`, exposed through the application-level `ISimulationExecutor` abstraction and composed in the API runtime with infrastructure decorators for cross-cutting concerns.

## Consequences

- Simulation logic remains separate from HTTP and CRUD-oriented application services.
- The engine can reuse repository abstractions without depending on infrastructure implementations.
- Logging and transaction handling stay additive and do not complicate the core engine code.
- Runtime behavior depends on infrastructure composition order, so decorator wiring must remain intentional.

## Alternatives considered

- Folding simulation logic into application services.
- Letting the API project construct the simulation engine directly.
- Embedding logging and transaction logic inside `SimulationEngine` rather than decorating it.
