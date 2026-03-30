## Status

Accepted

## Context

The repository accumulated several correctness and maintainability risks:

- settlement population was stored both on `Settlement` and in `PopulationGroup`
- the previous migration chain was tied to old schemas and journals, which made dev/test databases drift from the current code
- market price inputs mixed gross demand with post-transfer warehouse leftovers
- simulation silently tolerated settlements without required warehouse or market aggregates

Because the system is not in production, backward compatibility for old local databases and old migration history is not required.

## Decision

Adopt the following repository-wide rules:

1. Use a destructive baseline-reset migration as the authoritative schema entry point.
2. Make `PopulationGroup` the only source of truth for settlement population.
3. Compute market prices from:
   - warehouse stock snapshot at the start of the market phase
   - gross tick demand from household consumption, reserve demand, and production demand
4. Treat missing warehouse or market aggregates as invalid persisted state and fail the simulation instead of skipping the settlement.

## Consequences

- Local and test databases can be rebuilt into a known-good schema without preserving old migration history.
- Settlement read models stay consistent because population is derived instead of manually synchronized.
- Market price changes are deterministic and no longer depend on whether warehouse transfers happened earlier in the same phase.
- Simulation errors surface faster when persistence is incomplete or corrupted.

## Alternatives considered

- keeping `settlements.population` and continuing manual synchronization
- preserving the old migration chain and supporting mixed historical schemas
- calculating market supply from post-transfer warehouse stock
- silently skipping invalid settlements during simulation
