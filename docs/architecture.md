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
- `RPGEconomy.Domain` owns entities, shared `Result` / `Result<T>`, market pricing rules, population demand calculation, and core invariants.
- `RPGEconomy.Infrastructure` owns Dapper repositories, SQL queries, migrations, database connection management, and simulation executor decorators.
- `RPGEconomy.Simulation` owns the in-memory tick engine, production tick, and settlement-economy aggregation tick.

### Implemented domain scope

The current codebase now implements an early **Stage 3** baseline:

- worlds and settlements
- warehouses and inventory items
- product types
- production recipes and buildings
- population groups with consumption profiles
- local markets with per-product prices, supply, and demand
- synchronous simulation advancement with persisted simulation jobs
- currencies and resource types as CRUD-capable foundational entities
- resource-dependent production with production chains and production demand

Important Stage 3 interpretation in this repository:

- buildings remain the practical producer layer
- no separate `Producer` aggregate is introduced alongside buildings
- settlement warehouse stock is the shared source of production inputs and the carrier of produced outputs
- recipes keep the existing `Inputs` + `Outputs` shape instead of switching to a separate `InputRequirement` model
- the market still receives only aggregated `supply` / `demand`
- market demand is the sum of population consumption demand and production input demand
- zero-input recipes remain allowed as legacy source recipes for foundational goods

### Request handling and API style

- ASP.NET Core MVC controllers with attribute routing
- thin controllers that call one application service and map `Result`
- request models as local `record` types next to controllers
- no MediatR, FluentValidation pipeline, Minimal APIs, or centralized validation layer

Important routes currently include:

- `/api/worlds`
- `/api/worlds/{worldId}/settlements`
- `/api/settlements/{settlementId}/buildings`
- `/api/settlements/{settlementId}/population-groups`
- `/api/settlements/{settlementId}/market/prices`
- `/api/settlements/{settlementId}/market/products`
- `/api/settlements/{settlementId}/market/products/{productTypeId}`
- `/api/worlds/{worldId}/simulation/advance`

### Persistence and data model

- PostgreSQL with `Npgsql`
- Dapper repositories with handwritten SQL query classes
- DbUp-based startup migrations
- aggregate-style persistence for warehouses, markets, recipes, and population-group profiles via explicit child-row replacement

Money-related columns use `NUMERIC` in the database and `decimal` in code for:

- market offer price
- product base price
- currency exchange rate to base

Economic quantities also use `decimal` / `NUMERIC` for:

- warehouse inventory quantity
- recipe ingredient quantity
- market supply volume
- market demand volume
- consumption-per-person values inside population-group profiles

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
2. settlement-economy aggregation tick

The production tick currently:

- computes labor-limited building capacity
- limits actual output by available input resources in the settlement warehouse
- allows partial production when inputs are insufficient
- consumes inputs from the settlement warehouse
- writes outputs back to the settlement warehouse
- records aggregated production demand for missing inputs

The settlement-economy tick currently derives:

- supply from warehouse stock after building-based production
- demand from `PopulationGroup` consumption profiles
- additional demand from production-side missing inputs

The market remains independent from why demand or supply changed. Buildings and population groups adapt into aggregate values before they reach the market.

### Validation and error handling

Validation is distributed across layers:

- controllers validate simple request-shape rules
- application services validate orchestration-level inputs and references
- domain objects enforce stateful invariants such as duplicate market products, non-positive initial price, duplicate consumption-profile items, invalid recipe definitions, and negative supply/demand

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
- dedicated producer models beyond buildings
- institutions and policies
- economy zones
- cross-settlement trade and richer macroeconomics

These concepts are **not implemented today** unless they are explicitly present in the codebase. Vision documents under `docs/vision` describe intended future layers and causal chains, not the current repository boundary.

## Known trade-offs

- simulation remains synchronous at the HTTP boundary
- validation is still imperative rather than centralized
- market product names are resolved at application/query time instead of being embedded in the market aggregate
- integration tests require a live PostgreSQL environment
- buildings still serve as the producer abstraction, so richer producer behavior must continue adapting into warehouse and market boundaries instead of bypassing them
- production chains are intentionally deterministic and simple: building order is stable, but there is no optimization or priority model yet
