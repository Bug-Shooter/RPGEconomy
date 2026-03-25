# AGENTS

## Repository Structure

- `RPGEconomy.API`: ASP.NET Core controller-based HTTP entry point (`Program.cs`, controllers, request/exception middleware).
- `RPGEconomy.API.IntegrationTests`: HTTP integration tests built on `WebApplicationFactory` against the shared Postgres test database.
- `RPGEconomy.Application`: repository/service abstractions, simulation execution contracts, DTOs, and concrete application services.
- `RPGEconomy.Application.Tests`: unit tests for application services and orchestration.
- `RPGEconomy.Domain`: domain entities, simulation job state, enums, and shared `Result` / `Result<T>`.
- `RPGEconomy.Domain.Tests`: unit tests for domain behavior and invariants.
- `RPGEconomy.Infrastructure`: Dapper repositories, SQL query classes, connection factory, migrations, and simulation execution decorators.
- `RPGEconomy.Infrastructure.IntegrationTests`: repository and decorator integration tests against the shared Postgres test database.
- `RPGEconomy.Simulation`: local simulation executor, simulation context, production and market simulation services.
- `RPGEconomy.Simulation.Tests`: unit tests for simulation engine and simulation services.
- `RPGEconomy.Testing`: shared testing utilities for the Postgres test database, seed data, and cross-project test coordination.
- `docs/architecture.md`: current architecture snapshot; keep it aligned with actual project boundaries and dependency flow.

## Architecture Constraints Already In Use

- Keep reference directions: `API -> Application, Infrastructure, Simulation`; `Application -> Domain`; `Infrastructure -> Application, Domain`; `Simulation -> Application, Domain`.
- Keep controllers thin: accept route/body data, call one application service, translate `Result` to HTTP responses.
- Put business orchestration in `RPGEconomy.Application` services. Expected business failures use `Result` / `Result<T>`, not exceptions.
- Keep persistence in `RPGEconomy.Infrastructure` with repository implementations plus hand-written SQL in `Persistence/Queries`; no EF Core/`DbContext`.
- Keep startup DB migration in `RPGEconomy.API` via `MigrationRunner`; SQL scripts stay embedded under `RPGEconomy.Infrastructure/Migrations/Scripts`.
- Keep `ISimulationExecutor` implemented in `RPGEconomy.Simulation` and decorated in infrastructure (`LoggingSimulationDecorator`, `TransactionSimulationDecorator`).
- Keep simulation orchestration and job lifecycle in `RPGEconomy.Application` via `ISimulationService`.

## Build / Test Commands

- `dotnet restore RPGEconomy.slnx`
- `dotnet build RPGEconomy.slnx`
- `dotnet run --project RPGEconomy.API`
- `dotnet test RPGEconomy.slnx -m:1`
  - Recommended full-suite command.
  - `-m:1` keeps solution-level test project execution sequential, which avoids contention around the shared integration-test environment.
- `dotnet test RPGEconomy.API.IntegrationTests/RPGEconomy.API.IntegrationTests.csproj --no-build`
- `dotnet test RPGEconomy.Infrastructure.IntegrationTests/RPGEconomy.Infrastructure.IntegrationTests.csproj --no-build`
  - Recommended focused commands for integration projects after a successful build.
  - Integration tests use a shared Postgres test database and shared fixtures from `RPGEconomy.Testing`.
  - xUnit parallelization is disabled at the integration-test assembly level, and the shared database fixture acquires a global file lock before bootstrapping the database.

## Coding Rules Grounded In This Codebase

- Target framework is `net10.0` with nullable and implicit usings enabled in every project.
- Keep request models as small `record` types next to controllers; there is no separate contracts assembly.
- Validation is imperative in controllers/application services/domain methods. Do not introduce MediatR, FluentValidation, DataAnnotations pipelines, or Minimal APIs unless the repo is being intentionally reworked.
- Repositories open connections through `IDbConnectionFactory` and use Dapper async APIs. Follow the existing `Queries` + `Repositories` split.
- For aggregate-like saves, preserve the current explicit child-row replacement style where repositories already use it.
- Logging uses built-in `ILogger<T>` plus the existing middleware/decorator pattern; do not add a separate logging stack casually.
- Keep shared integration-test infrastructure in `RPGEconomy.Testing`. Do not duplicate database fixtures or shared collection names in individual integration-test projects.
- Do not call xUnit collection fixture lifecycle methods manually from tests. Let xUnit own fixture initialization and cleanup.

## Documentation / Update Expectations

- When changing project boundaries, dependency directions, persistence approach, simulation wiring, or error-handling flow, update `docs/architecture.md` in the same change.
- When changing cross-cutting architectural decisions, add or update an ADR under `docs/adr`.
- Update this file when repository structure, build/test commands, or the enforced coding conventions above stop matching the codebase.
