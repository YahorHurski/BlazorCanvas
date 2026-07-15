# Deferred Items — Phase BC-02

## 02-02: Database integration tests require Docker Postgres running

**Discovered during:** Task 3 verification (full `dotnet test` run, not the plan's own narrower
`--filter UsernameNormalizer` check).

**Observation:** `BlazorCanvas.Tests.Database.CheckConstraintTests` (68 test cases, Phase BC-01) fail
with `NpgsqlException: Failed to connect to 127.0.0.1:5433` — Docker Compose's Postgres container is
not currently running on this dev machine.

**Scope:** Out of scope for 02-02. These tests exercise the BC-01 schema/CHECK-constraint work, not
anything touched by this plan (`UsernameNormalizer`, `Program.cs` cookie-auth wiring). Not caused by,
and not fixable within, this plan's file set.

**Action taken:** None — logged only, per the executor's scope-boundary rule. The plan's own
verification step (`dotnet test --filter "FullyQualifiedName~UsernameNormalizer"`) passes cleanly (6/6),
and `dotnet build BlazorCanvas.sln` succeeds. Bringing the Postgres container up
(`docker compose up -d`) before running the full suite is a pre-existing environment-state item, not a
code defect.
