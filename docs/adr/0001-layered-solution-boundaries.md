## Status

Accepted

## Context

The solution is split into five production projects: `RPGEconomy.API`, `RPGEconomy.Application`, `RPGEconomy.Domain`, `RPGEconomy.Infrastructure`, and `RPGEconomy.Simulation`. The project references enforce a directed dependency flow: the API references Application and Infrastructure; Application references Domain; Infrastructure references Application, Domain, and Simulation; Simulation references Application and Domain. The code in each project also follows those boundaries, with controllers in the API project, services and abstractions in Application, entities and `Result` in Domain, persistence and composition in Infrastructure, and the simulation runtime in Simulation.

## Decision

Use a layered multi-project architecture with explicit project boundaries and dependency directions:

- `API` is the HTTP entry point.
- `Application` owns use-case orchestration, DTOs, and service/repository abstractions.
- `Domain` owns core entities and shared primitives.
- `Infrastructure` owns persistence, composition, migrations, and decorators.
- `Simulation` owns the simulation runtime while depending only on Application abstractions and Domain types.

## Consequences

- Architectural responsibilities are easy to locate and reason about.
- The domain and application layers stay reusable by both HTTP and simulation flows.
- Infrastructure becomes the main composition point for non-HTTP runtime concerns.
- Cross-layer changes require care because project references intentionally limit shortcuts.

## Alternatives considered

- A single-project monolith with folders instead of project boundaries.
- A stricter clean architecture split with a dedicated composition root outside Infrastructure.
- Feature-sliced projects that group HTTP, application, and persistence code per feature.
