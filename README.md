# RPG Economy Simulation

A backend simulation of a medieval RPG economy. Settlements produce goods, 
trade on local markets, and prices shift dynamically based on supply and demand.

# Core Concepts
World — top-level container with a day counter 

Settlement — a city with population, buildings, and a local market

Building — produces goods using recipes each simulation day

Market — tracks supply, demand, and dynamic pricing per product

Simulation — advance the world by N days, triggering production and trade cycles
