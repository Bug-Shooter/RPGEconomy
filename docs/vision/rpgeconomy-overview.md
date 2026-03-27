# RPGEconomy Vision Overview

This document describes the intended long-term product vision. It is not a description of the current repository state.

## Status

Future-facing vision only.

The following concepts may appear here even when they are not implemented in code yet:

- `State`
- `Region`
- `TradeRoute`
- `EconomyZone`
- `Producer`
- `PopulationGroup`
- `Institution`
- `Policy`

Use `docs/architecture.md` for the current implementation truth.

## Long-term idea

RPGEconomy aims to model causal economic processes in a fantasy world:

- world events create constraints
- actors change behavior
- production and consumption reshape availability
- inventories and trade flows reshape local access
- local markets translate those conditions into prices
- monetary systems later express those prices in broader macroeconomic terms

## Intended future capabilities

- richer producer and population behavior
- multi-settlement trade
- route disruption and regional divergence
- state policy effects
- broader monetary and macroeconomic layers

## Current boundary

Today the repository implements only the early foundation of that vision:

- local markets
- warehouses and inventory
- recipes and buildings
- synchronous simulation ticks

Everything else should be treated as roadmap unless explicitly visible in code.
