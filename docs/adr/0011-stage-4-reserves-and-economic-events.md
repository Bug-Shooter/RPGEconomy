## Status

Accepted

## Context

Stage 4 adds reserve-driven demand and economic events to a repository that already committed to:

- buildings as the effective producer abstraction
- settlement warehouses as the main physical stock outside the market aggregate
- markets that accept only aggregated `supply` / `demand`

The raw Stage 4 specification talks about generic inventories and producers, but that would conflict with the current repository shape and ADRs 0009 and 0010.

## Decision

Implement Stage 4 as an extension of the current architecture:

- keep `Building` as the producer abstraction
- keep `Warehouse` as settlement market stock
- add household reserve stock to `PopulationGroup`
- add producer input reserve stock to `Building`
- add persisted `EconomicEvent` and `EconomicEffect` aggregates that modify simulation parameters, not market prices
- keep the market unchanged as an aggregate over aggregated state

Simulation now follows a richer but still deterministic pipeline:

1. consume household stock
2. run production from building input reserves
3. compute reserve demand
4. transfer warehouse stock into immediate consumption and reserve replenishment
5. send aggregated `supply` / `demand` to the market

## Consequences

- Stage 4 fits the existing repository instead of introducing a parallel producer model
- reserve stock becomes explicit and persisted without turning the market into a transaction engine
- economic events remain causal and indirect because they only change behavior inputs
- product-category targeting stays deferred until categories exist in the domain model

## Alternatives considered

- introducing a separate `Producer` aggregate
- introducing one polymorphic inventory table with owner-type dispatch in v1
- letting events set prices or mutate market offers directly
