# Stage 1.5 Local Market

## Summary

Stage 1.5 defines the repository baseline that exists today:

- one settlement has one local market
- products can be registered on that market
- each registered product stores current price, supply, and demand
- prices are recalculated deterministically from aggregated supply/demand
- settlements also have a warehouse
- minimal supply-side simulation already exists through buildings, recipes, warehouse stock, and market ticks

This is intentionally broader than the original Stage 1 draft, but still much smaller than the long-term economy vision.

## What is implemented

- `World` and `Settlement`
- automatic market and warehouse provisioning for new settlements
- `ProductType`
- `Market` and `MarketOffer`
- market product registration
- market product state updates through aggregated `supply` / `demand`
- single-product and full-market price queries
- simulation tick integration with warehouse-derived supply
- simulation-side population-based demand stub

## Market rules

- duplicate product registration is rejected
- initial price must be greater than zero
- supply and demand cannot be negative
- price rises when demand exceeds supply
- price falls when supply exceeds demand
- price rises when supply is zero and demand is positive
- price stays stable when both are zero
- price never falls below `0.01`
- per-tick change is bounded to avoid explosive jumps

## Accepted foundation in this stage

The following parts are accepted as supporting foundation and are not treated as scope violations:

- `Warehouse`
- `InventoryItem`
- `ProductionRecipe`
- `Building`
- synchronous `SimulationEngine`
- simulation job persistence

They are part of the current Stage 1.5 baseline, but they do not redefine the market as a producer-aware or agent-level exchange.

## Explicitly out of scope for this stage

- order book and matching engine
- individual NPC transactions
- producer and population domain models as first-class market inputs
- trade routes and cross-settlement arbitrage
- state policy, taxation, or macroeconomic regulation
- currency zones and inflation mechanics beyond foundational CRUD entities

## API surface

Current local-market endpoints:

- `GET /api/settlements/{settlementId}/market/prices`
- `GET /api/settlements/{settlementId}/market/products/{productTypeId}`
- `POST /api/settlements/{settlementId}/market/products`
- `PUT /api/settlements/{settlementId}/market/products/{productTypeId}`

Error handling:

- `404` for missing market or missing market product
- `400` for invalid input or invalid market state transitions

## Design constraints

- market stays an aggregate over aggregated supply/demand, not an order-matching engine
- money uses `decimal` in code and `NUMERIC` in PostgreSQL
- market pricing rules stay in domain logic
- demand-source specifics stay outside the market aggregate
