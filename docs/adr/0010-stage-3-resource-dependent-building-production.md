## Status

Accepted

## Context

The repository had already implemented Stage 2 through:

- buildings as the producer layer
- shared settlement warehouses
- recipes with explicit `Inputs` and `Outputs`
- markets that accept only aggregated `supply` / `demand`

The Stage 3 draft introduced resource-dependent production, production chains, production demand, and a `Producer` / `ProducerStock` framing. That framing conflicts with the current repository shape because adding a separate producer aggregate or a second stock model would duplicate the existing building and warehouse model.

At the same time, Stage 3 still needs the economy to stop producing goods "from air" for recipes that depend on real inputs.

## Decision

Implement Stage 3 as an extension of the existing building + warehouse + aggregated-market architecture:

- keep `Building` as the effective producer abstraction
- keep settlement `Warehouse` as the source of production inputs and the holder of produced outputs
- keep `ProductionRecipe` in its existing `Inputs` + `Outputs` form
- do not introduce a persisted `ProducerStock` in Stage 3 v1
- compute production input shortages during the production tick and expose them as aggregated production demand
- keep `Market` unchanged as an aggregate that only accepts aggregated `supply` / `demand`

Production behavior for Stage 3 is:

- labor determines planned production capacity for the tick
- available warehouse inputs determine actual achievable output
- partial production is allowed when some, but not all, required inputs are present
- missing inputs generate production demand for the same tick

Legacy zero-input recipes remain allowed as source recipes for foundational goods.

## Consequences

- Stage 3 fits the existing architecture instead of introducing a parallel producer model
- production chains can be modeled immediately through recipes and shared warehouse stock
- market demand now reflects both consumption and production pressure without making the market producer-aware
- the system remains intentionally simple and deterministic, but building order now matters for same-tick chain propagation

## Alternatives considered

- introducing a new `Producer` aggregate alongside buildings
- introducing a persisted `ProducerStock` separate from settlement warehouses
- making the market aware of producer internals instead of accepting aggregated demand
- forbidding zero-input recipes entirely, which would remove a practical way to represent foundational resource sources in the current model
