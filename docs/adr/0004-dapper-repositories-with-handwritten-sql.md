## Status

Accepted

## Context

Persistence lives in `RPGEconomy.Infrastructure`. Repository implementations depend on `IDbConnectionFactory`, open connections directly, and use Dapper async APIs such as `QueryFirstOrDefaultAsync`, `QueryAsync`, `ExecuteAsync`, and `ExecuteScalarAsync`. SQL is stored in dedicated query classes under `Persistence/Queries`. Aggregate-like saves use explicit child-row replacement in some repositories, such as deleting and reinserting market offers in `MarketRepository`. Database schema changes are managed through embedded SQL scripts executed by `MigrationRunner` with DbUp.

## Decision

Use repository implementations backed by Dapper and handwritten SQL query classes, with connections created through `IDbConnectionFactory` and schema changes applied through embedded SQL migrations.

## Consequences

- SQL stays explicit and close to the repository behavior it supports.
- The codebase avoids introducing EF Core change tracking and `DbContext`.
- Persistence behavior is predictable, but more manual mapping and save logic is required.
- Aggregate updates can be straightforward to reason about, but row replacement patterns may perform more writes than differential updates.

## Alternatives considered

- Entity Framework Core with `DbContext` and mapped entities.
- Inline SQL strings directly inside repository methods.
- A micro-ORM plus stored procedures instead of query classes and embedded migration scripts.
