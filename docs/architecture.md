# Architecture

This document describes the architecture that exists in the codebase today. It reflects the current implementation and naming as found in the solution.

## Solution shape

The solution contains five production projects:

- `RPGEconomy.API`
- `RPGEconomy.Application`
- `RPGEconomy.Domain`
- `RPGEconomy.Infrastructure`
- `RPGEconomy.Simulation`

It also contains six test-support and test-execution projects:

- `RPGEconomy.API.IntegrationTests`
- `RPGEconomy.Application.Tests`
- `RPGEconomy.Domain.Tests`
- `RPGEconomy.Infrastructure.IntegrationTests`
- `RPGEconomy.Simulation.Tests`
- `RPGEconomy.Testing`

The current project reference directions are:

- `RPGEconomy.API` -> `RPGEconomy.Application`
- `RPGEconomy.API` -> `RPGEconomy.Infrastructure`
- `RPGEconomy.API` -> `RPGEconomy.Simulation`
- `RPGEconomy.Application` -> `RPGEconomy.Domain`
- `RPGEconomy.Infrastructure` -> `RPGEconomy.Application`
- `RPGEconomy.Infrastructure` -> `RPGEconomy.Domain`
- `RPGEconomy.Simulation` -> `RPGEconomy.Application`
- `RPGEconomy.Simulation` -> `RPGEconomy.Domain`

In practice, this forms a layered structure with runtime composition concentrated in `RPGEconomy.API`.

## Project boundaries

### `RPGEconomy.API`

`RPGEconomy.API` is the HTTP entry point. It contains:

- `Program.cs`
- MVC API controllers
- custom middleware for request logging and exception handling

The API project does not contain business logic or persistence code. Controllers depend on application service interfaces such as `IWorldService`, `ISettlementService`, and `ISimulationService`.

Request models are defined as small `record` types next to controllers rather than in a separate contracts assembly.

### `RPGEconomy.Application`

`RPGEconomy.Application` contains:

- repository abstractions
- service abstractions
- simulation execution contracts
- DTOs returned to the API layer
- concrete application services

The service classes orchestrate use cases. They call repositories, perform validation checks, construct DTOs, manage simulation job lifecycle, and return `Result` / `Result<T>` instead of throwing for normal business failures.

This project is not purely abstractions: interfaces and concrete service implementations live together.

### `RPGEconomy.Domain`

`RPGEconomy.Domain` contains the domain model and shared primitives:

- entities and aggregate roots
- value-like enums such as `QualityGrade`
- simulation job state and lifecycle
- the shared `Result` / `Result<T>` type

Domain objects expose methods such as `Update`, `AdvanceDays`, `AddItem`, `Withdraw`, and `UpdateMarket`. Some invariants are enforced here by returning `Result.Failure(...)`.

The domain model also exposes constructors that are used by Dapper materialization.

### `RPGEconomy.Infrastructure`

`RPGEconomy.Infrastructure` contains persistence and technical cross-cutting concerns:

- repository implementations
- SQL query classes
- database connection factory
- database migrations
- decorators around `ISimulationExecutor`

This project registers repository implementations and provides technical decorators that are composed by the API project.

### `RPGEconomy.Simulation`

`RPGEconomy.Simulation` contains the simulation runtime:

- `SimulationEngine`
- simulation context and clock
- internal simulation services for production and market updates
- local executor DI registration

This project implements the `ISimulationExecutor` abstraction from `RPGEconomy.Application` and operates by loading state through repository interfaces, mutating in-memory domain objects, and persisting selected aggregates back through repositories.

## Dependency directions

The dependency flow in code is:

1. HTTP requests enter controllers in `RPGEconomy.API`.
2. Controllers call service interfaces from `RPGEconomy.Application`.
3. `SimulationService` in `RPGEconomy.Application` creates and updates `SimulationJob` records, then delegates execution through `ISimulationExecutor`.
4. Application services use repository interfaces from `RPGEconomy.Application`.
5. Repository implementations in `RPGEconomy.Infrastructure` talk to PostgreSQL through Dapper.
6. Domain entities from `RPGEconomy.Domain` are shared across application, infrastructure, and simulation code.
7. `RPGEconomy.Simulation` depends on application repository abstractions and domain entities.
8. `RPGEconomy.API` composes application services, infrastructure repositories, local simulation execution, and execution decorators.

This means the runtime composition is owned by the API project, while infrastructure stays focused on persistence and technical adapters.

## Conceptual Model

The system represents a time-driven economic simulation.

Core concepts:
- World
- Settlement
- Economy
- Market
- Production chains
- Simulation ticks

The simulation is the central domain process that evolves the system state over time.


## Request handling style

The HTTP layer uses ASP.NET Core controllers with attribute routing:

- `[ApiController]`
- `[Route(...)]`
- action methods returning `Task<IActionResult>`

The API is controller-based, not Minimal API based.

Controllers are thin. Their usual pattern is:

1. Accept route parameters and a `[FromBody]` request record.
2. Call one application service method.
3. Translate `Result` into `Ok`, `CreatedAtAction`, `BadRequest`, `NotFound`, or `NoContent`.

Routes are organized around resource hierarchies, for example:

- `/api/worlds`
- `/api/worlds/{worldId}/settlements`
- `/api/worlds/{worldId}/simulation/advance`

The current code does not use MediatR, command handlers, query handlers, or endpoint-specific request pipelines. Controllers call services directly.

The simulation HTTP endpoint still behaves synchronously, but it now goes through an application service that tracks an internal simulation job lifecycle before returning the result.

## Persistence approach

Persistence is implemented in `RPGEconomy.Infrastructure` using:

- PostgreSQL via `Npgsql`
- Dapper for data access
- DbUp for migrations

The current persistence pattern is repository-based:

- application defines repository interfaces such as `IWorldRepository`
- infrastructure implements them in classes such as `WorldRepository`
- simulation jobs follow the same repository pattern through `ISimulationJobRepository`

Each repository method creates and opens a database connection through `IDbConnectionFactory`.

SQL is written manually and stored in static query classes such as `WorldQueries`, `SettlementQueries`, and `MarketQueries`. Repositories call Dapper APIs such as:

- `QueryFirstOrDefaultAsync`
- `QueryAsync`
- `ExecuteAsync`
- `ExecuteScalarAsync`

There is no EF Core `DbContext` in the current code.

For aggregate-like objects with child collections, repositories persist the root and then replace child rows explicitly. Examples include markets and warehouses, where offers/items are deleted and re-inserted during save operations.

Database migrations run during application startup. Connection string resolution is centralized around the shared ASP.NET Core configuration:

- `RPGEconomy.Infrastructure` resolves `ConnectionStrings:DefaultConnection` from `IConfiguration` when constructing `IDbConnectionFactory`
- `Program.cs` calls `RunDatabaseMigrations()` on the application configuration, and that extension resolves the same configured connection string before invoking `MigrationRunner`

The runner:

- ensures the PostgreSQL database exists
- runs embedded SQL scripts with DbUp

The schema also contains a `simulation_jobs` table used to persist the internal job lifecycle for simulation execution.

## Validation

Validation is primarily imperative and distributed across controllers, application services, and some domain methods.

Current patterns include:

- direct `if` checks in controllers for simple request rules
- direct `if` checks in application services for required fields and numeric ranges
- domain methods returning `Result.Failure(...)` for some state-based invariants

Examples of validated conditions include:

- empty names
- non-positive day counts
- non-positive population values
- missing referenced entities
- duplicate or missing market offers
- insufficient warehouse stock

The current codebase does not contain:

- FluentValidation
- DataAnnotations-based request validation attributes
- explicit `ModelState` handling
- a centralized validation pipeline

## Error handling

There are two distinct error handling styles in the code:

### Expected business errors

Expected failures are usually returned as `Result` / `Result<T>`.

Application services and some domain methods use `Result.Failure(...)` for cases such as:

- entity not found
- invalid input values
- invalid domain actions

Controllers map those results to HTTP responses, mostly `400 Bad Request` or `404 Not Found`.

### Unexpected errors

Unexpected exceptions are handled by custom middleware in `RPGEconomy.API`.

`ExceptionHandlingMiddleware`:

- wraps the rest of the pipeline in `try/catch`
- logs the exception
- returns HTTP 500 with a JSON payload containing `error` and `message`

Exceptions are still used in a few infrastructure/startup paths, for example:

- missing connection string during infrastructure registration or migration startup
- failed database upgrade in `MigrationRunner`
- invalid day count in `SimulationClock`

## Logging

Logging uses the built-in `ILogger<T>` abstraction.

The current code logs in three places:

### Request logging middleware

`RequestLoggingMiddleware` logs:

- incoming HTTP method, path, and query string
- outgoing method, path, status code, and elapsed milliseconds

### Exception middleware

`ExceptionHandlingMiddleware` logs unhandled exceptions as errors.

### Simulation decorator

`LoggingSimulationDecorator` wraps `ISimulationExecutor` and logs:

- simulation start
- successful completion with job id and day range
- failed completion with the returned error

In addition, DbUp migration execution is configured with `LogToConsole()`.

There is no separate logging package or custom logging infrastructure in the solution today.

## Testing style

The solution now includes automated tests split by concern:

- `RPGEconomy.Domain.Tests` for domain unit tests
- `RPGEconomy.Application.Tests` for application-service unit tests
- `RPGEconomy.Simulation.Tests` for simulation unit tests
- `RPGEconomy.Infrastructure.IntegrationTests` for repository and infrastructure integration tests against PostgreSQL
- `RPGEconomy.API.IntegrationTests` for end-to-end HTTP tests
- `RPGEconomy.Testing` as shared test support for PostgreSQL reset/bootstrap and test configuration

Integration tests use a dedicated PostgreSQL configuration from `RPGEconomy.Testing/appsettings.Test.json`. The API integration host and lower-level database helpers both resolve the same test connection string through configuration rather than hardcoded values.

`RPGEconomy.Testing` is also the home for the shared integration-test fixture and synchronization primitives:

- `DatabaseFixture` centralizes database bootstrap and reset lifecycle
- `IntegrationTestCollection` centralizes the xUnit collection name used by integration-test assemblies
- `GlobalTestDatabaseLock` serializes access to the shared PostgreSQL test database across integration-test processes

The integration-test projects intentionally keep only thin assembly-specific `CollectionDefinition` glue so the actual lifecycle logic is not duplicated across projects.

At the test runner level, the practical full-suite command is `dotnet test RPGEconomy.slnx -m:1`. Even though integration-test assemblies disable xUnit test parallelization internally, solution-level sequential execution is still important because both integration-test projects share one PostgreSQL environment.

## Notable structural patterns currently present

The codebase currently combines several recognizable patterns:

- layered projects with explicit project references
- repository pattern for persistence
- DTO-based service responses
- `Result`-based business error propagation
- thin MVC controllers
- decorator pattern around `ISimulationExecutor`
- internal simulation job tracking in the application layer
- startup-time database migration
- manual SQL with Dapper

These patterns are all present in the implementation today and are the basis of the current architecture.

## Known Trade-offs

- Simulation requests are still synchronous at the HTTP boundary
- PostgreSQL integration tests require a live database
- Full-solution test runs should be executed sequentially at the project level because API and infrastructure integration tests share one PostgreSQL environment
- No centralized validation
