## Status

Accepted

## Context

The repository reached a Stage 1.5 baseline before a dedicated Stage 2 implementation was added. At that point the codebase already had:

- buildings
- production recipes
- warehouses
- a production simulation tick

The Stage 2 specification introduced:

- `PopulationGroup`
- `ConsumptionProfile`
- a settlement-level economy aggregation layer

The specification also described a simple `Producer` abstraction that does not use recipes or inputs. That conflicts with the repository reality: supply already comes from building-based production persisted through warehouses. Replacing that supply-side with a second parallel producer model would duplicate concepts and weaken the existing architecture.

Stage 2 also requires fractional consumption values such as `0.05`, while Stage 1.5 stored most economic quantities as integers.

## Decision

Implement Stage 2 in this repository with the following adaptations:

- treat existing buildings as the effective producer layer for supply
- add `PopulationGroup` with embedded consumption-profile items as the demand layer
- introduce a settlement-economy aggregation tick in simulation that converts building output and population demand into aggregated market inputs
- keep `Market` unchanged as an aggregate that only accepts aggregated `supply` / `demand`
- move economic quantities from `int` to `decimal` in code and `NUMERIC` in PostgreSQL where quantities can be fractional

`Settlement.Population` remains stored, but from Stage 2 onward it is maintained as a synchronized projection of population groups once groups are used.

## Consequences

- Stage 2 extends the existing architecture instead of introducing a competing producer model
- the market stays isolated from producer and population internals
- fractional demand works without lossy rounding before the market
- quantity handling becomes more consistent, but the change touches domain, persistence, DTOs, and tests across multiple layers

## Alternatives considered

- adding a separate simple `Producer` aggregate alongside buildings
- keeping quantity fields as `int` and rounding demand before market update
- letting the market compute population-derived demand directly
