## Status

Accepted

## Context

The repository originally stored money-related values as floating-point numbers. That was acceptable for a quick prototype, but it created unnecessary risk for price calculations, persistence round-trips, and future economic extensions.

The current codebase needs a stable rule for:

- market prices
- product base prices
- currency exchange rates

At the same time, non-money continuous values such as weight, labor time, and regeneration rate do not need to be forced into the same representation.

## Decision

Use:

- `decimal` in C# for money-related values
- `NUMERIC` in PostgreSQL for money-related columns

This applies to:

- `MarketOffer.CurrentPrice`
- `ProductType.BasePrice`
- `Currency.ExchangeRateToBase`

Non-money values may continue to use `double` / `FLOAT` where appropriate.

## Consequences

- money values become deterministic and persistence-friendly
- Dapper round-trips align with domain expectations for economic calculations
- future monetary and pricing rules can build on a safer numeric foundation
- a mixed numeric model remains in the codebase, so engineers must distinguish money from non-money quantities intentionally

## Alternatives considered

- keeping all numeric values as `double`
- converting every continuous numeric field in the system to `decimal`
- introducing a full money value object before the model needs that level of sophistication
