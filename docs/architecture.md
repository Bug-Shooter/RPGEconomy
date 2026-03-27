# Architecture

This document describes the architecture implemented in the repository today. It separates current implementation from future-facing product vision so roadmap documents do not get mistaken for the current code shape.

## Implemented now

### Solution shape

Production projects:

- `RPGEconomy.API`
- `RPGEconomy.Application`
- `RPGEconomy.Domain`
- `RPGEconomy.Infrastructure`
- `RPGEconomy.Simulation`

Test and test-support projects:

- `RPGEconomy.API.IntegrationTests`
- `RPGEconomy.Application.Tests`
- `RPGEconomy.Domain.Tests`
- `RPGEconomy.Infrastructure.IntegrationTests`
- `RPGEconomy.Simulation.Tests`
- `RPGEconomy.Testing`

Project references:

- `RPGEconomy.API` -> `RPGEconomy.Application`, `RPGEconomy.Infrastructure`, `RPGEconomy.Simulation`
- `RPGEconomy.Application` -> `RPGEconomy.Domain`
- `RPGEconomy.Infrastructure` -> `RPGEconomy.Application`, `RPGEconomy.Domain`
- `RPGEconomy.Simulation` -> `RPGEconomy.Application`, `RPGEconomy.Domain`

### Runtime responsibilities

- `RPGEconomy.API` is the HTTP entry point with thin controller-based endpoints and middleware.
- `RPGEconomy.Application` owns use-case orchestration, DTOs, repository abstractions, and simulation job lifecycle orchestration.
- `RPGEconomy.Domain` owns entities, shared `Result` / `Result<T>`, market pricing rules, and core invariants.
- `RPGEconomy.Infrastructure` owns Dapper repositories, SQL queries, migrations, database connection management, and simulation executor decorators.
- `RPGEconomy.Simulation` owns the in-memory tick engine and simulation-side demand/production services.

### Implemented domain scope

The current codebase is broader than the original local-market-only draft. It already includes:

- worlds and settlements
- warehouses and inventory items
- product types
- production recipes and buildings
- local markets with per-product prices, supply, and demand
- synchronous simulation advancement with persisted simulation jobs
- currencies and resource types as CRUD-capable foundational entities

The current milestone should be understood as **Stage 1.5**:

- local market remains the central completed capability
- minimal supply-side simulation already exists
- warehouse, recipes, and buildings are accepted as foundation rather than treated as out-of-scope accidents

### Request handling and API style

- ASP.NET Core MVC controllers with attribute routing
- thin controllers that call one application service and map `Result`
- request models as local `record` types next to controllers
- no MediatR, FluentValidation pipeline, Minimal APIs, or centralized validation layer

Important routes currently include:

- `/api/worlds`
- `/api/worlds/{worldId}/settlements`
- `/api/settlements/{settlementId}/buildings`
- `/api/settlements/{settlementId}/market/prices`
- `/api/settlements/{settlementId}/market/products`
- `/api/settlements/{settlementId}/market/products/{productTypeId}`
- `/api/worlds/{worldId}/simulation/advance`

### Persistence and data model

- PostgreSQL with `Npgsql`
- Dapper repositories with handwritten SQL query classes
- DbUp-based startup migrations
- aggregate-style persistence for warehouses and markets via explicit child-row replacement

Money-related columns use `NUMERIC` in the database and `decimal` in code for:

- market offer price
- product base price
- currency exchange rate to base

Non-money continuous values still use `double` where appropriate, including:

- `weight_per_unit`
- `labor_days_required`
- `regeneration_rate_per_day`

### Market model

The market is intentionally modeled as an **aggregator of local state**, not an order book:

- each market belongs to one settlement
- each product appears at most once per market
- market state is stored as current price, supply volume, and demand volume
- the market accepts aggregated `supply` / `demand` inputs
- price recalculation is deterministic and isolated in domain logic

Current market price behavior:

- price rises when `demand > supply`
- price falls when `supply > demand`
- price rises when `supply = 0` and `demand > 0`
- price stays stable when both are zero
- price is clamped to a minimum floor and to a bounded per-tick change

### Simulation flow

The simulation runtime loads settlements and related aggregates once, runs ticks in memory, and persists selected aggregates after execution.

Current tick order:

1. production tick
2. market tick

The market tick currently derives:

- supply from warehouse stock
- demand from a simulation-side population-based stub provider

This keeps the market aggregate independent from the origin of demand while leaving room for future producer/population models.

### Validation and error handling

Validation is distributed across layers:

- controllers validate simple request-shape rules
- application services validate orchestration-level inputs and references
- domain objects enforce stateful invariants such as duplicate market products, non-positive initial price, and negative supply/demand

Expected business errors use `Result` / `Result<T>`.
Unexpected failures are handled by API middleware.

### Testing

The repository contains:

- domain unit tests
- application service unit tests
- simulation unit tests
- infrastructure integration tests against PostgreSQL
- API integration tests through `WebApplicationFactory`

The recommended full verification command remains:

`dotnet test RPGEconomy.slnx -m:1`

## Future vision

The broader economy vision still includes concepts such as:

- states
- regions
- trade routes
- population groups
- producers
- institutions and policies
- economy zones
- cross-settlement trade and richer macroeconomics

These concepts are **not implemented today** unless they are explicitly present in the codebase. Vision documents under `docs/vision` describe intended future layers and causal chains, not the current repository boundary.

## Known trade-offs

- simulation remains synchronous at the HTTP boundary
- validation is still imperative rather than centralized
- market product names are resolved at application/query time instead of being embedded in the market aggregate
- integration tests require a live PostgreSQL environment
- current demand generation is intentionally simplistic and simulation-side
