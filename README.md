# RPG Economy Simulation

A backend simulation of a medieval RPG economy. Settlements produce goods, population groups create demand, and local market prices shift dynamically from aggregated supply and demand.

## Core Concepts

- `World` — top-level container with a day counter
- `Settlement` — a city with a warehouse, market, and synchronized population summary
- `Building` — the practical producer layer, creating supply through recipes
- `PopulationGroup` — aggregated residents with a consumption profile that creates demand
- `Market` — tracks supply, demand, and dynamic pricing per product
- `Simulation` — advances the world by N days, running production and settlement-economy ticks
