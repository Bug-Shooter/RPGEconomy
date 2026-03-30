# RPGEconomy Vision Layers

This document describes the target logical layers of the simulation. It is a vision map, not a statement that every layer already exists in code.

## Status

Future-facing vision only.

Implemented today:

- part of the local market layer
- part of the physical economy layer through recipes/buildings/warehouses

Not implemented today as first-class layers:

- world geography and event systems
- actor and behavior systems
- trade-route systems
- macroeconomic layers

## Target layer model

1. World, geography, and events
2. Actors and behavior
3. Physical economy
4. Inventories and flows
5. Local market
6. Monetary and macroeconomic layer

## Important modeling principle

The intended causal chain is:

`events -> behavior -> production/consumption -> inventories/flows -> market -> price`

That means upper layers should not set prices directly. They should change conditions that later influence price through the market.

## Current implementation note

The repository currently uses a simplified early-stage approximation:

- warehouse stock feeds market supply
- a simulation-side population stub feeds market demand
- market pricing remains local and aggregate

This should be understood as a foundation for future layers, not as completion of the full target stack.
