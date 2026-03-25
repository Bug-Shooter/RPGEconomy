## Status

Accepted

## Context

The solution now contains two integration-test projects, `RPGEconomy.API.IntegrationTests` and `RPGEconomy.Infrastructure.IntegrationTests`, and both execute against the same PostgreSQL test database. Earlier, each project carried its own copy of `DatabaseFixture` and collection metadata, while individual tests also reset the same database state. Assembly-level xUnit parallelization had already been disabled, but that only serialized tests inside a single test assembly. It did not prevent solution-level runs from starting both integration-test projects at the same time, which caused database contention, flaky failures, and hangs once shared locking was introduced.

## Decision

Keep a single shared PostgreSQL integration-test environment, but centralize its lifecycle and serialize access to it:

- move shared integration-test infrastructure into `RPGEconomy.Testing`
- expose one shared `DatabaseFixture` and one shared `IntegrationTestCollection` name
- guard fixture initialization with a global file-based lock in `GlobalTestDatabaseLock`
- keep xUnit parallelization disabled in each integration-test assembly
- recommend `dotnet test RPGEconomy.slnx -m:1` for full-suite runs so integration-test projects do not start concurrently at the solution level

## Consequences

- Shared integration-test behavior is defined in one place instead of being duplicated across test assemblies.
- API and infrastructure integration tests can continue to reuse one PostgreSQL database without stepping on each other when executed correctly.
- Full-solution test runs become more predictable, but slower, because integration-test projects should run sequentially.
- Tests must not call fixture lifecycle methods manually, because the shared fixture now owns locking and bootstrap behavior.

## Alternatives considered

- Giving every integration-test project its own dedicated PostgreSQL database.
- Creating a fresh database or schema per test run.
- Keeping duplicated fixtures in each test project and relying only on assembly-level xUnit parallelization settings.
