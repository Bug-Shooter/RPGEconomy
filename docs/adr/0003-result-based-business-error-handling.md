## Status

Accepted

## Context

Expected business failures are represented with `Result` and `Result<T>` from `RPGEconomy.Domain.Common`. Application services such as `WorldService` and `MarketService` return failure results for invalid input or missing entities, and some domain methods also return `Result` for state-based rules. Controllers inspect `IsSuccess` and convert those failures into `400` or `404` responses. Exceptions are still used for unexpected failures and startup/infrastructure problems, for example missing configuration in `Program.cs`, migration failures in `MigrationRunner`, and unhandled exceptions processed by `ExceptionHandlingMiddleware`.

## Decision

Represent expected business failures with `Result` / `Result<T>` instead of throwing exceptions, and reserve exceptions for unexpected or technical failures.

## Consequences

- Business failure paths are explicit in service and domain method signatures.
- Controllers can map expected failures to HTTP responses without exception-driven control flow.
- Callers must consistently inspect `IsSuccess`, which adds ceremony to use-case code.
- Error semantics are split between result-based failures and exception-based technical faults.

## Alternatives considered

- Throwing exceptions for both business and technical failures.
- Returning nullable values or booleans plus out parameters for failure cases.
- Adopting a third-party discriminated-union or error-object library instead of the local `Result` type.
