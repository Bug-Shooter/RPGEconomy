# AGENTS

## Repository Structure

- `RPGEconomy.API`: ASP.NET Core controller-based HTTP entry point (`Program.cs`, controllers, request/exception middleware).
- `RPGEconomy.Application`: repository/service abstractions, simulation execution contracts, DTOs, and concrete application services.
- `RPGEconomy.Domain`: domain entities, simulation job state, enums, and shared `Result` / `Result<T>`.
- `RPGEconomy.Infrastructure`: Dapper repositories, SQL query classes, connection factory, migrations, and simulation execution decorators.
- `RPGEconomy.Simulation`: local simulation executor, simulation context, production and market simulation services.
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
- `dotnet test RPGEconomy.slnx`
  - There are currently no test projects in the solution, so this is only meaningful after adding tests.

## Coding Rules Grounded In This Codebase

- Target framework is `net10.0` with nullable and implicit usings enabled in every project.
- Keep request models as small `record` types next to controllers; there is no separate contracts assembly.
- Validation is imperative in controllers/application services/domain methods. Do not introduce MediatR, FluentValidation, DataAnnotations pipelines, or Minimal APIs unless the repo is being intentionally reworked.
- Repositories open connections through `IDbConnectionFactory` and use Dapper async APIs. Follow the existing `Queries` + `Repositories` split.
- For aggregate-like saves, preserve the current explicit child-row replacement style where repositories already use it.
- Logging uses built-in `ILogger<T>` plus the existing middleware/decorator pattern; do not add a separate logging stack casually.

## Documentation / Update Expectations

- When changing project boundaries, dependency directions, persistence approach, simulation wiring, or error-handling flow, update `docs/architecture.md` in the same change.
- Update this file when repository structure, build/test commands, or the enforced coding conventions above stop matching the codebase.
