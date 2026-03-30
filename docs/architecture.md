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

The current codebase now implements an early **Stage 4** baseline:

- worlds and settlements
- warehouses and inventory items
- product types
- production recipes and buildings
- population groups with consumption profiles
- household reserve stocks and reserve coverage rules
- local markets with per-product prices, supply, and demand
- synchronous simulation advancement with persisted simulation jobs
- currencies and resource types as CRUD-capable foundational entities
- resource-dependent production with production chains and production demand
- building input reserve stocks
- settlement-scoped economic events and effects

Important data-model decisions in the current baseline:

- `PopulationGroup` is the only source of truth for settlement population
- settlement population is computed in read models as the sum of `population_groups.population_size`
- the active SQL baseline is destructive and reset-oriented: old local/test databases are expected to be recreated or rebuilt by the current baseline script

Important Stage 4 interpretation in this repository:

- buildings remain the practical producer layer
- no separate `Producer` aggregate is introduced alongside buildings
- settlement warehouse stock is market-visible stock and the carrier of produced outputs
- building input reserves are persisted separately from the warehouse
- household reserve stock is persisted on `PopulationGroup`
- recipes keep the existing `Inputs` + `Outputs` shape instead of switching to a separate `InputRequirement` model
- the market still receives only aggregated `supply` / `demand`
- market demand is the sum of population consumption demand, production demand, and reserve demand
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
- `/api/settlements/{settlementId}/economic-events`
- `/api/settlements/{settlementId}/population-groups`
- `/api/settlements/{settlementId}/market/prices`
- `/api/settlements/{settlementId}/market/products`
- `/api/settlements/{settlementId}/market/products/{productTypeId}`
- `/api/worlds/{worldId}/simulation/advance`

### Persistence and data model

- PostgreSQL with `Npgsql`
- Dapper repositories with handwritten SQL query classes
- DbUp-based startup migrations
- a single destructive baseline migration script that drops legacy tables before recreating the current schema
- aggregate-style persistence for warehouses, markets, recipes, population-group children, building reserve children, and event effects via explicit child-row replacement
- repository child-row replacement uses local SQL transactions unless the caller already runs inside an ambient transaction

Database integrity rules enforced directly in the current baseline include:

- foreign keys from inventory, market offers, recipe ingredients, and population consumption to `product_types`
- unique constraints for one product per market offer, one product-quality row per warehouse item, one product per reserve row, and one scoped economic effect per event
- check constraints for non-negative quantities, positive prices and exchange rates, non-negative reserve coverage, and valid event windows

Money-related columns use `NUMERIC` in the database and `decimal` in code for:

- market offer price
- product base price
- currency exchange rate to base

Economic quantities also use `decimal` / `NUMERIC` for:

- warehouse inventory quantity
- household reserve quantity
- building input reserve quantity
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

Current price input semantics:

- `supply` is the settlement warehouse stock snapshot visible at the start of the market phase
- `demand` is gross tick demand: unmet household consumption plus production demand plus reserve demand
- internal transfers into household consumption and reserves happen after market state is updated, so post-transfer warehouse остаток does not feed back into the same tick's market supply

### Simulation flow

The simulation runtime loads settlements and related aggregates once, runs ticks in memory, and persists selected aggregates after execution.

Current tick order:

1. household stock consumption
2. production tick from building input reserves
3. snapshot market-visible warehouse stock
4. compute gross demand and update market state
5. transfer warehouse stock into immediate consumption and reserve replenishment

The production tick currently:

- computes labor-limited building capacity
- limits actual output by available building input reserves
- allows partial production when inputs are insufficient
- consumes inputs from building reserve stock
- writes outputs back to the settlement warehouse
- records aggregated production demand for missing inputs

The settlement-economy tick currently derives:

- demand from household consumption not covered by household stock
- additional demand from production-side missing inputs
- household reserve demand from reserve gaps
- producer reserve demand from input-reserve gaps
- supply from warehouse stock snapshot before market-phase transfers

Simulation safety rules currently enforced:

- the engine fails fast if a settlement is missing a warehouse or market
- missing infrastructure is treated as an invalid persisted state, not as a silent skip

Economic events:

- belong to one settlement
- expose active effects for the current simulation day
- may multiply consumption demand, desired reserve coverage, producer reserve coverage, or final demand
- never set prices directly

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
- production chains are intentionally deterministic and simple: building order is stable, reserve transfers use fixed-band ordering plus deterministic pro-rata allocation, and there is still no optimization or richer priority model
- settlement bootstrap is coordinated in the application layer with compensating cleanup instead of a shared cross-repository transaction abstraction
