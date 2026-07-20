---
phase: BC-01-database-schema-geometry-core
reviewed: 2026-07-15T00:00:00Z
depth: standard
files_reviewed: 27
files_reviewed_list:
  - src/BlazorCanvas/BlazorCanvas.csproj
  - src/BlazorCanvas/Data/CanvasDbContext.cs
  - src/BlazorCanvas/Data/CanvasDbContextFactory.cs
  - src/BlazorCanvas/Data/Figure.cs
  - src/BlazorCanvas/Data/User.cs
  - src/BlazorCanvas/Geometry/Box.cs
  - src/BlazorCanvas/Geometry/CanvasBounds.cs
  - src/BlazorCanvas/Geometry/CircleEncoding.cs
  - src/BlazorCanvas/Geometry/FigureType.cs
  - src/BlazorCanvas/Geometry/FigureTypeNames.cs
  - src/BlazorCanvas/Geometry/MinSizeGuard.cs
  - src/BlazorCanvas/Geometry/Movement.cs
  - src/BlazorCanvas/Geometry/Normalisation.cs
  - src/BlazorCanvas/Migrations/20260714212457_InitialSchema.Designer.cs
  - src/BlazorCanvas/Migrations/20260714212457_InitialSchema.cs
  - src/BlazorCanvas/Migrations/CanvasDbContextModelSnapshot.cs
  - src/BlazorCanvas/Program.cs
  - src/BlazorCanvas/appsettings.Development.json
  - tests/BlazorCanvas.Tests/BlazorCanvas.Tests.csproj
  - tests/BlazorCanvas.Tests/Database/CheckConstraintTests.cs
  - tests/BlazorCanvas.Tests/Database/DatabaseFixture.cs
  - tests/BlazorCanvas.Tests/Database/GuardMirrorsChecksTests.cs
  - tests/BlazorCanvas.Tests/Database/SchemaShapeTests.cs
  - tests/BlazorCanvas.Tests/Geometry/CircleEncodingTests.cs
  - tests/BlazorCanvas.Tests/Geometry/ClampTests.cs
  - tests/BlazorCanvas.Tests/Geometry/MinSizeGuardTests.cs
  - tests/BlazorCanvas.Tests/Geometry/NormalisationTests.cs
findings:
  critical: 3
  warning: 9
  info: 5
  total: 17
status: issues_found
---

# Phase BC-01: Code Review Report

**Reviewed:** 2026-07-15
**Depth:** standard
**Files Reviewed:** 27
**Status:** issues_found

## Summary

The core D-50 mirror (`MinSizeGuard` vs the three CHECK constraints) is, arm for arm, **correct**. I traced every predicate:

| Rule | SQL | C# | Agree? |
|---|---|---|---|
| `line_is_a_line` | `x2 >= x1 AND (x2 > x1 OR y2 <> y1)` | `b.X2 >= b.X1 && (b.X2 > b.X1 \|\| b.Y2 != b.Y1)` | yes |
| `box_is_a_box` | `x2 > x1 AND y2 > y1` | `b.X2 > b.X1 && b.Y2 > b.Y1` | yes |
| `circle_is_a_circle` | `x2-x1 = y2-y1 AND x2 > x1 AND (x2-x1) % 2 = 0` | `b.Width == b.Height && b.X2 > b.X1 && b.Width % 2 == 0` | yes |

`%` truncates toward zero in both C# and PostgreSQL, and both circle arms are gated by `x2 > x1` first, so the negative-modulo divergence never fires. `Normalisation` correctly swaps the whole point pair for lines, and `ClampMove` correctly recomputes a min/max bounding box before clamping — both landmines D-41 warns about are avoided.

**The defects are in the layers around that mirror, not in the mirror itself:**

1. The clamps (`ClampDrawRadius`, `ClampMove`) are the *only* thing keeping figures on the canvas, and **both of them can be driven out of bounds** — and *nothing* (not the guard, not any CHECK) rejects an off-canvas figure. The phase's headline claim ("the database itself rejects illegal rows") therefore holds only for min-size/normalisation, not for the canvas domain.
2. `ClampDrawRadius` can return a **negative radius**, which after `Normalisation` becomes a *legal-looking* circle in the wrong place that every guard and every CHECK accepts.
3. `CanvasDbContextFactory` will **silently apply DDL to a different PostgreSQL server** (the native PG-18 on 5432) if the connection string isn't found.

Migrations, snapshot and DbContext are in exact agreement with each other; the test suite is thorough on the happy paths but its D-50 matrix is 8 hand-picked boxes, none of which use negative or out-of-canvas coordinates — precisely the region where the bugs below live.

---

## Critical Issues

### CR-01: `ClampDrawRadius` can return a negative radius, producing an off-canvas circle that every guard and every CHECK accepts

**File:** `src/BlazorCanvas/Geometry/CircleEncoding.cs:27-36`

**Issue:** The radius cap has an upper bound but **no lower bound**:

```csharp
return Math.Min(rounded, Math.Min(Math.Min(cx, cy), Math.Min(CanvasBounds.Width - cx, CanvasBounds.Height - cy)));
```

If `cx < 0`, `cy < 0`, `cx > 1280` or `cy > 720`, one of the four terms is negative and the function returns a **negative radius**. `FromCentreRadius` then emits an inverted box, and `Normalisation.Normalise(Circle, …)` — which axis-sorts — turns that inverted box into a **perfectly well-formed circle**:

- `ClampDrawRadius(cx: -5, cy: 360, distance: 50)` → `r = -5`
- `FromCentreRadius(-5, 360, -5)` → `Box(0, 365, -10, 355)`
- `Normalise(Circle, …)` → `Box(-10, 355, 0, 365)` — square, side 10, even, `X2 > X1`
- `MinSizeGuard.IsDrawable(Circle, …)` → **true**
- `circle_is_a_circle` → **passes**. The row is written with `x1 = -10`.

The user gets a circle at coordinates that do not exist, and neither the guard nor the database says a word. This is exactly the class of silent divergence D-50 exists to prevent, and it is reachable the moment BC-02 feeds raw pointer coordinates in (pointer capture during a drag routinely reports coordinates outside the element; a scaled SVG `viewBox` can also round past `1280`).

**Fix:** Floor the radius at 0 and clamp the centre into the canvas before capping. Both belong here, not in the caller — a geometry primitive that trusts its inputs is the drift vector.

```csharp
public static int ClampDrawRadius(int cx, int cy, double distance)
{
    // The centre is the press point: it must be inside the canvas before it can cap anything.
    var ccx = Movement.ClampDelta(cx, 0, CanvasBounds.Width);
    var ccy = Movement.ClampDelta(cy, 0, CanvasBounds.Height);

    var rounded = (int)Math.Round(distance, MidpointRounding.AwayFromZero);

    var capped = Math.Min(
        rounded,
        Math.Min(
            Math.Min(ccx, ccy),
            Math.Min(CanvasBounds.Width - ccx, CanvasBounds.Height - ccy)));

    return Math.Max(0, capped); // never negative — a negative radius normalises into a valid circle
}
```

---

### CR-02: `ClampMove(box, 0, 0)` is not the identity — an out-of-canvas or oversized box is silently teleported

**File:** `src/BlazorCanvas/Geometry/Movement.cs:10, 23-24`

**Issue:** `ClampDelta` is `Math.Min(Math.Max(v, lo), hi)`. When `lo > hi` — which happens whenever the box is wider than the canvas or already extends past the right/bottom edge — `Math.Max(v, lo)` lifts the value to `lo`, then `Math.Min(…, hi)` drops it to `hi`, and the function **returns `hi`, which is less than `lo`**. The clamp silently inverts.

Concrete: `ClampMove(new Box(0, 0, 2000, 100), dx: 0, dy: 0)`
- `bx1 = 0`, `bx2 = 2000`
- `dxPrime = ClampDelta(0, lo: -0, hi: 1280 - 2000 = -720)` → `Math.Min(Math.Max(0, 0), -720)` = **-720**
- Result: `Box(-720, 0, 1280, 100)` — a zero-delta "move" translated the figure 720px to the left.

There is no assertion, no exception, no log. Any figure that gets out of bounds (see CR-01, and any future import/seed path) will jump on the very next drag event, including a drag with no movement. `ClampTests.ShapeInvarianceCases` only ever feeds boxes that already fit inside the canvas, so this is untested.

**Fix:** Make the degenerate `lo > hi` case explicit rather than letting `Min`/`Max` order decide it. Given the canvas is 1280×720 and figures are drawn inside it, the correct behaviour for an oversized box is "do not move it":

```csharp
public static int ClampDelta(int v, int lo, int hi) =>
    lo > hi ? 0 : Math.Min(Math.Max(v, lo), hi);
```

and add a test: `ClampMove(new Box(0, 0, 2000, 100), 0, 0)` must return the input unchanged.

---

### CR-03: `CanvasDbContextFactory` silently falls back to a hardcoded `localhost:5432` — a *different* PostgreSQL server on this machine

**File:** `src/BlazorCanvas/Data/CanvasDbContextFactory.cs:18-25`

**Issue:** Both `AddJsonFile` calls are `optional: true`, the base path is `Directory.GetCurrentDirectory()`, and the missing-config path is a hardcoded literal:

```csharp
var connectionString = configuration.GetConnectionString("Canvas")
    ?? "Host=localhost;Port=5432;Database=canvas;Username=postgres;Password=postgres";
```

Two independent problems compound:

1. **Wrong port.** The fallback says **5432**. The project's actual database is on **5433** (`docker-compose.yml` publishes `5433:5432`; `appsettings.Development.json` says `Port=5433`). Port 5432 on this machine is occupied by the **native postgresql-x64-18 Windows service** — a completely different server, per the phase's own deviation note.
2. **The fallback is reachable in normal use.** `dotnet ef migrations add` / `dotnet ef database update` invoked from the repository root (rather than `src/BlazorCanvas/`) will find neither `appsettings.json` nor `appsettings.Development.json` — both are `optional: true`, so no error — silently hit the fallback, and **apply the migration DDL to the native PG-18 instance**, creating `users`, `figures` and `__EFMigrationsHistory` in whatever `canvas` database exists there. A subsequent `Down`/`drop` targets that same wrong server. This is DDL executed against an unintended database with no warning whatsoever.

The hardcoded `Username=postgres;Password=postgres` in committed C# source is a secondary issue (it will be copy-pasted into the production path).

**Fix:** Fail loudly instead of guessing. A design-time factory that cannot find its configuration must not invent one.

```csharp
public CanvasDbContext CreateDbContext(string[] args)
{
    // Anchor to the project directory, not the caller's CWD — `dotnet ef` may be invoked
    // from the repo root, and an optional-file miss must never silently reach a fallback.
    var basePath = Path.GetDirectoryName(typeof(CanvasDbContextFactory).Assembly.Location)!;

    var configuration = new ConfigurationBuilder()
        .SetBasePath(Directory.GetCurrentDirectory())
        .AddJsonFile("appsettings.json", optional: true)
        .AddJsonFile("appsettings.Development.json", optional: true)
        .AddEnvironmentVariables()
        .Build();

    var connectionString = configuration.GetConnectionString("Canvas")
        ?? throw new InvalidOperationException(
            "ConnectionStrings:Canvas is not configured. Run `dotnet ef` from src/BlazorCanvas/ " +
            "(so appsettings.Development.json is found) or set ConnectionStrings__Canvas. " +
            "Refusing to guess a connection string: port 5432 on this machine is a DIFFERENT " +
            "PostgreSQL server and applying migrations to it would corrupt it.");

    var optionsBuilder = new DbContextOptionsBuilder<CanvasDbContext>();
    optionsBuilder.UseNpgsql(connectionString);
    return new CanvasDbContext(optionsBuilder.Options);
}
```

---

## Warnings

### WR-01: Nothing — not the guard, not any CHECK — rejects an off-canvas figure

**File:** `src/BlazorCanvas/Data/CanvasDbContext.cs:48-68`, `src/BlazorCanvas/Geometry/MinSizeGuard.cs:11-24`

**Issue:** `CanvasBounds` declares the domain as `0..1280 × 0..720`, but the domain is enforced **only** by the two clamp functions — which are advisory helpers a caller can simply not call, and which are themselves defective (CR-01, CR-02). The database will happily accept:

```sql
INSERT INTO figures (user_id, type, x1, y1, x2, y2) VALUES (1, 'rectangle', -99999, -99999, 999999, 999999);
```

All four CHECKs pass. `MinSizeGuard.IsDrawable(Rectangle, …)` returns `true`. The ROADMAP's success criterion 3 ("the database itself rejects illegal rows") therefore covers min-size and normalisation but **not** the canvas domain — an unbounded rectangle is as illegal as a zero-height one, and only one of the two is caught. `CheckConstraintTests` contains no negative or out-of-canvas case, so the gap is invisible in the suite.

**Fix:** Add a fourth CHECK and its C# mirror, keeping the D-50 pairing intact. **This changes the schema contract, so it needs a user decision (D-19/D-50 do not state it explicitly) — raise it rather than assuming it.**

```csharp
// CanvasDbContext.cs
t.HasCheckConstraint(
    "figure_is_on_canvas",
    "x1 >= 0 AND y1 >= 0 AND x2 >= 0 AND y2 >= 0 AND " +
    "x1 <= 1280 AND x2 <= 1280 AND y1 <= 720 AND y2 <= 720");
```

```csharp
// MinSizeGuard.cs — mirror it, or the D-50 invariant is broken by the fix itself
private static bool IsOnCanvas(Box b) =>
    b.X1 >= 0 && b.Y1 >= 0 && b.X2 >= 0 && b.Y2 >= 0 &&
    b.X1 <= CanvasBounds.Width && b.X2 <= CanvasBounds.Width &&
    b.Y1 <= CanvasBounds.Height && b.Y2 <= CanvasBounds.Height;

public static bool IsDrawable(FigureType type, Box b) => IsOnCanvas(b) && type switch { /* … */ };
```

---

### WR-02: `AddDbContext` in a Blazor Server app — scoped-per-circuit DbContext will throw on concurrent operations

**File:** `src/BlazorCanvas/Program.cs:12-13`

**Issue:** In Blazor Server, a scope lives for the **entire lifetime of the circuit**, not for a single request. `AddDbContext` therefore gives every user a single, long-lived, shared `DbContext` across every component and every event handler on that circuit. Two overlapping async handlers (two rapid mouse-ups, a render + a save) will hit `InvalidOperationException: A second operation was started on this context instance before a previous operation completed`, and the accumulating change-tracker will hold every figure the user has ever touched for the life of the circuit. This is the documented Blazor Server EF pitfall; Microsoft's guidance is `AddDbContextFactory`.

Nothing consumes the DbContext yet, so this does not fail today — it will fail the moment BC-02 wires the canvas up.

**Fix:**
```csharp
builder.Services.AddDbContextFactory<CanvasDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("Canvas")));
```
and use `await using var db = await factory.CreateDbContextAsync();` per operation.

---

### WR-03: Migration retry catches `NpgsqlException`, which is exactly what the comment says it will not catch

**File:** `src/BlazorCanvas/Program.cs:17-39`

**Issue:** The comment states: *"let any other exception propagate — a migration failure (e.g. a CHECK constraint that cannot be created) must fail loudly at boot."* But `PostgresException` **derives from** `NpgsqlException`. A malformed CHECK, a syntax error in the DDL, or a wrong password (`28P01`) is a `PostgresException` → caught by `catch (NpgsqlException)` → **retried nine times over ~18 seconds, silently, with no logging**, before finally surfacing on attempt 10. The end state is correct (it does eventually throw), but the diagnostic is buried under an 18-second silent pause, and the code does not do what its own comment claims.

**Fix:** Narrow the filter to genuinely transient connection failures and log each retry.

```csharp
catch (NpgsqlException ex) when (attempt < maxAttempts && ex is not PostgresException)
{
    app.Logger.LogWarning(ex, "Database not reachable (attempt {Attempt}/{Max}); retrying in {Delay}s.",
        attempt, maxAttempts, delay.TotalSeconds);
    await Task.Delay(delay);
}
```

---

### WR-04: `Figure.Type` is an unvalidated raw string — nothing in C# mirrors `figures_type_is_known`

**File:** `src/BlazorCanvas/Data/Figure.cs:17`, `src/BlazorCanvas/Geometry/MinSizeGuard.cs:11`

**Issue:** `MinSizeGuard.IsDrawable` takes a `FigureType` **enum** — so the type literal is never the thing it validates. `Figure.Type` is a bare `public string` with a `string.Empty` default. Any code path that does `new Figure { Type = "Circle" }` (or `""`, or `type.ToString()`) sails past every C# guard and is caught only by a `PostgresException` at `SaveChanges` — a runtime 500, not a silent-reject. That is precisely the D-46 landmine the doc comment describes, left with no C#-side gate. The suite proves the *database* catches it (`CheckConstraintTests` line 44, "PascalCase type literal") but nothing proves the app never emits it.

**Fix:** Make the string unconstructible by hand — route every write through the enum:

```csharp
public static bool IsKnownDbValue(string value) =>
    value is "line" or "rectangle" or "circle" or "triangle";
```
and add an assertion/validation at the single INSERT point in BC-02, plus a unit test that `Figure.Type` is only ever assigned from `FigureTypeNames.ToDbValue`.

---

### WR-05: The D-50 mirror has no mechanical link — the same predicate is hand-transcribed four times

**File:** `src/BlazorCanvas/Data/CanvasDbContext.cs:51-68`, `src/BlazorCanvas/Migrations/20260714212457_InitialSchema.cs:44-47`, `src/BlazorCanvas/Migrations/CanvasDbContextModelSnapshot.cs:67-73`, `src/BlazorCanvas/Geometry/MinSizeGuard.cs:15-21`

**Issue:** Each CHECK predicate exists as a **free-text SQL string in three files** and again as a **hand-written C# boolean in a fourth**. They agree today (I verified all four). Nothing enforces that they keep agreeing. `GuardMirrorsChecksTests.Matrix()` is the only cross-check, and it is **8 hand-picked boxes × 4 types**, all with non-negative coordinates ≤ 100. That matrix would **not** catch:
- a bounds clause added to the SQL but not the guard (or vice-versa) — no out-of-canvas box is ever probed;
- a minimum-size clause (`x2 - x1 >= 2`) added to one side — no 1px box is ever probed;
- `% 2` changed to `% 4` on one side — no side-6 circle is ever probed.

Given D-50 is the phase's central invariant, an 8-point matrix is a thin net.

**Fix:** Add a randomised agreement test — the guard and the database are cheap to disagree with over a few hundred random boxes:

```csharp
[Fact]
public async Task Guard_And_Database_Agree_OverRandomBoxes()
{
    var rng = new Random(20260715); // fixed seed — reproducible
    foreach (var type in Enum.GetValues<FigureType>())
    {
        for (var i = 0; i < 50; i++)
        {
            var box = new Box(rng.Next(-20, 1300), rng.Next(-20, 740), rng.Next(-20, 1300), rng.Next(-20, 740));
            var guard = MinSizeGuard.IsDrawable(type, box);
            var db = (await _fixture.TryInsertFigureAsync(type, box)).Succeeded;
            Assert.True(guard == db, $"D-50 disagreement: {type} {box}: guard={guard}, db={db}");
        }
    }
}
```

(Note this test will currently **fail** on out-of-canvas boxes only if WR-01 is adopted — which is itself the point: it makes the gap visible.)

---

### WR-06: The container-teardown test destroys shared infrastructure mid-suite and leaks rows on failure

**File:** `tests/BlazorCanvas.Tests/Database/GuardMirrorsChecksTests.cs:109-158`

**Issue:** `FiguresWrittenViaEfCore_SurviveContainerTeardown` runs `docker compose down` on the container that **every other test in the `Database` collection depends on**. Two failure modes:

1. If `docker compose up -d --wait` (line 137) fails, or `WaitForDatabaseAsync` times out, the shared container is left **down** and every subsequent test in `CheckConstraintTests`, `SchemaShapeTests` and the rest of this class fails with a connection error — a single infrastructure hiccup cascades into a wall of unrelated red.
2. The cleanup block (lines 150-157) sits **after** `Assert.Equal(insertedIds, survivingIds)`. If that assertion fails, the user and its three `rectangle` rows are **never deleted** and persist in the named volume forever, polluting every future run.

It is also the only test in the file that isn't about the guard/CHECK mirror — it is a persistence test wearing the wrong class name.

**Fix:** Wrap the destructive section in `try/finally` so cleanup always runs, and move the test to its own collection (or its own class with a dedicated fixture) so a failed container restart cannot take the rest of the suite down with it.

---

### WR-07: `ClampDrawRadius` returns 0 at the exact canvas edge — the press is silently dead, contradicting its own doc comment

**File:** `src/BlazorCanvas/Geometry/CircleEncoding.cs:22-36`

**Issue:** The doc comment says: *"Known and accepted consequence: pressing near an edge forces a tiny circle."* But at the edge itself the behaviour is not "a tiny circle" — it's **no circle at all**. `ClampDrawRadius(cx: 0, cy: 360, distance: 200)` → `min(200, min(0, 360), …)` → **0** → `FromCentreRadius(0, 360, 0)` → `Box(0, 360, 0, 360)` → `MinSizeGuard.IsDrawable(Circle, …)` = `false` (`X2 > X1` fails) → the draw is silently discarded. `CircleEncodingTests.ZeroRadius_ProducesABoxTheGuardRejects` asserts exactly this and calls it correct; `ClampDrawRadius_NearLeftEdge_ForcesATinyCircle` only tests `cx: 10`, never `cx: 0`.

The user presses on the edge, drags, releases, and nothing happens — with no feedback. That may be acceptable (D-50 says a rejected draw is silent), but the documented behaviour and the actual behaviour differ, which means nobody has decided this consciously.

**Fix:** Either enforce a minimum radius (e.g. `Math.Max(1, …)` — but note that would then be clipped by the edge cap anyway), or correct the comment to state the real behaviour: *"pressing ON an edge yields r = 0 and the draw is silently discarded."* This is a user-facing behaviour question — surface it rather than picking one.

---

### WR-08: Plaintext passwords, committed dev credentials, and no production connection string

**File:** `src/BlazorCanvas/Data/User.cs:13`, `src/BlazorCanvas/appsettings.Development.json:3`, `src/BlazorCanvas/appsettings.json`

**Issue:** Three related items, calibrated to the "throwaway learning project" context:

1. **Plaintext password (D-08, locked).** `users.password` is `text`, stored and compared as-is. Recorded here as an **accepted, documented risk**, not a request to change a locked decision — but it must be recorded, because "it was reviewed and nobody mentioned it" is how such things ship.
2. **Committed credentials.** `appsettings.Development.json` (tracked in git, and `.gitignore` does not exclude it) carries `Password=postgres`. Correct for a throwaway Docker container. The risk is the **pattern**: the same key in `appsettings.json` would be a real production secret in the same tracked file.
3. **`appsettings.json` has no `ConnectionStrings:Canvas` at all.** In any non-Development environment, `builder.Configuration.GetConnectionString("Canvas")` returns `null`, `UseNpgsql(null)` is accepted, and the app fails at `db.Database.Migrate()` with an `InvalidOperationException` — which is *not* an `NpgsqlException`, so it does propagate immediately. Loud, but the message will be an EF internals error, not "you forgot to configure the database."

**Fix:** For (2)/(3), add an explicit startup check so the failure is legible, and keep secrets out of `appsettings.json`:

```csharp
var connectionString = builder.Configuration.GetConnectionString("Canvas")
    ?? throw new InvalidOperationException(
        "ConnectionStrings:Canvas is not configured. Set it via user-secrets, " +
        "the ConnectionStrings__Canvas environment variable, or appsettings.Development.json.");
builder.Services.AddDbContextFactory<CanvasDbContext>(o => o.UseNpgsql(connectionString));
```
For (1), no code change — it is locked. Just carry the risk forward explicitly.

---

### WR-09: `TryInsertRawFigureAsync`'s catch filter hides every non-CHECK rejection as a test crash

**File:** `tests/BlazorCanvas.Tests/Database/DatabaseFixture.cs:108-112`

**Issue:** The filter is `ex.InnerException is PostgresException pg && pg.SqlState == PostgresErrorCodes.CheckViolation`. Any **other** database rejection — `NOT NULL` violation (`23502`), FK violation (`23503`), or `22003 integer out of range` (which `x2 - x1` in `circle_is_a_circle` will raise for extreme coordinates, see IN-01) — escapes as a raw `DbUpdateException` and fails the test with a stack trace instead of being reported as `InsertAttempt.Rejected`.

That matters for `GuardMirrorsChecksTests.GuardVerdict_MatchesDatabaseVerdict`: a case where the guard says "drawable" but the database rejects for a **non-CHECK** reason is a genuine D-50 disagreement, and it will surface as an unhandled exception rather than as the carefully-worded "resolve in favour of CONSTRAINT-schema" assertion message.

**Fix:** Catch any `PostgresException` and let the assertion compare verdicts; keep the SQLSTATE for the `CheckConstraintTests` assertions, which already check it explicitly.

```csharp
catch (DbUpdateException ex) when (ex.InnerException is PostgresException pg)
{
    return InsertAttempt.Rejected(pg);
}
```

---

## Info

### IN-01: `Box.Width`/`Height` use unchecked `int` subtraction; PostgreSQL raises on the same expression

**File:** `src/BlazorCanvas/Geometry/Box.cs:9-11`

`X2 - X1` overflows silently in C# (unchecked by default) but raises `22003 integer out of range` in PostgreSQL for the identical `x2 - x1` in `circle_is_a_circle`. A `Box(int.MinValue, 0, int.MaxValue, 0)` gives the guard a *negative* width while the DB errors out — a D-50 divergence in the extreme tail. Not reachable through the UI (coordinates come from pointer events), but if a future import path accepts arbitrary ints it becomes real. A bounds CHECK (WR-01) closes it for free.

### IN-02: Index naming is inconsistent

**File:** `src/BlazorCanvas/Data/CanvasDbContext.cs:37-38, 97-98`

`ix_figures_user_id` is named explicitly; the username unique index is left EF-default (`IX_users_username`) — PascalCase in an otherwise all-snake_case schema. `SchemaShapeTests.UsersUsername_HasAUniqueConstraint` correctly asserts existence rather than name, so nothing breaks. Suggest `.HasDatabaseName("ix_users_username")` for consistency.

### IN-03: `ToCentreRadius` silently returns garbage for a non-square box

**File:** `src/BlazorCanvas/Geometry/CircleEncoding.cs:14-20`

`r` is derived from the X extent only; `cy` is then computed from that `r`. For a box that is not square (which the CHECK forbids in the DB, but which any in-memory `Box` can be), the returned centre is wrong with no complaint. Worth an explicit precondition in the doc comment, or a `Debug.Assert(b.Width == b.Height)`.

### IN-04: `FigureTypeNames.Parse` has no `TryParse` and throws at the read boundary

**File:** `src/BlazorCanvas/Geometry/FigureTypeNames.cs:19-26`

`Parse` is exact-match and throws `ArgumentException` on anything else. Safe today because `figures_type_is_known` guarantees the column's contents — but it means a single bad row (introduced by a future manual `UPDATE`, or a migration that changes the literals) turns a canvas load into an unhandled exception rather than a skipped figure. A `TryParse` companion would let the read path degrade gracefully.

### IN-05: `Migrate()` at startup has no environment guard

**File:** `src/BlazorCanvas/Program.cs:21-39`

Auto-migrate on boot is fine for a single-instance dev app (D-42), but it runs in every environment including Production, where two instances starting simultaneously race on `__EFMigrationsHistory`. Out of scope for a throwaway learning project — noted only so the decision is conscious if it ever ships.

---

_Reviewed: 2026-07-15_
_Reviewer: Claude (gsd-code-reviewer)_
_Depth: standard_
