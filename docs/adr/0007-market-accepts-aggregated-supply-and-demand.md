## Status

Accepted

## Context

The original local-market draft focused on a market for a single settlement where price changes are driven by aggregate `supply` and `demand` rather than individual trades. The repository has since grown to include warehouses, buildings, recipes, and simulation ticks. Without an explicit decision, it would be easy for future code to push producer-specific, population-specific, or transaction-specific concerns directly into the market aggregate.

## Decision

Keep `Market` as an aggregate that accepts **aggregated supply and demand inputs** per product.

The market:

- stores local price and aggregate state
- validates aggregate state transitions
- recalculates price from aggregate values

The market does not:

- model an order book
- match individual trades
- own producer behavior
- own population behavior
- know why demand or supply changed

Those concerns belong to simulation-side or future higher-level models that compute aggregate inputs before they reach the market.

## Consequences

- the market stays small, deterministic, and easy to test
- future producer/population models can be added without rewriting the market aggregate
- demand and supply origins remain replaceable
- detailed agent simulation, if ever added, must adapt into aggregated market inputs rather than bypass market boundaries

## Alternatives considered

- embedding producer and population logic directly into the market aggregate
- modeling the local market as an order book / matching engine
- storing only warehouse stock and deriving all market state implicitly at read time
