# Project Retrospective

*A living document updated after each milestone. Lessons feed forward into future planning.*

## Milestone: v1.0 — MinVP

**Shipped:** 2026-07-17
**Phases:** 5 | **Plans:** 23 | **Tasks:** 56 | **Commits:** 131 | **Elapsed:** ~2.3 days (2026-07-14 → 2026-07-17)

### What Was Built

- **A machine-enforced geometry schema.** Two tables whose CHECK constraints — not application code —
  reject a non-square circle, a zero-area rectangle, or a zero-length line. The database is the last
  line of defence, and it holds.
- **A pure C# geometry core** (normalise, delta-clamp, circle-as-inscribed-square, per-type min-size
  guard) with zero Blazor dependency, consumed unchanged by three downstream phases.
- **Cookie auth across two render modes** — static SSR for `/login` and `POST /logout`,
  InteractiveServer for the canvas — because an interactive component cannot set a cookie.
- **The three verbs**: draw all four shapes, drag with per-axis edge clamping that slides, delete.
- **Live cross-tab sync with real-time drag glide** — an in-memory notifier keyed by `user_id`,
  50ms throttle with guaranteed trailing edge, and the nine irreducible consistency rules.
- **405 tests**, ~2,500 LOC application + ~2,000 LOC tests, and **zero hand-authored JavaScript**.

### What Worked

- **Making the hardest feature part of the definition of done.** Live cross-tab sync was written into
  the definition of done, so no phase could quietly defer it. It shipped.
- **Front-loading risk.** The ADR audit found the log "airtight wherever the human was in the room,
  silent wherever the framework was." Phase 1 therefore built the database, schema and geometry maths
  first — the silent-failure surface — before any UI existed to hide it.
- **Machine-enforced invariants over trusted-in-code ones.** Integer coordinates exist specifically so
  geometric truths can be CHECK constraints. That decision paid: the DB refuses illegal rows
  independently of `MinSizeGuard`.
- **Gap-closure plans as a first-class mechanism.** BC-01's verification found three Critical defects;
  plans 01-05/01-06 closed them with regression tests that were RED on the pre-fix code, rather than
  the findings becoming a backlog item nobody read.
- **Composing primitives instead of duplicating them.** `ClampDrawRadius` clamps its centre by calling
  `Movement.ClampDelta` — one hardened primitive, reused. The v1.0 integration audit confirmed zero
  duplicate clamp/normalise logic anywhere outside `Geometry/`.

### What Was Inefficient

- **Verification outlived its own gaps by two days.** BC-01's `01-VERIFICATION.md` sat at
  `gaps_found` (4/6) from 2026-07-15 until 2026-07-17 even though 01-05/01-06 had closed all three
  Critical defects almost immediately. The report was never re-run. It would have blocked the
  milestone close and misreported project health in every progress check in between.
- **Bookkeeping drifted silently in four separate places, all found at close, none caught by any
  test:** STATE.md's body contradicted its own frontmatter (claiming "Phase 4 complete / 80%" while
  the frontmatter correctly said 5/5 and 23/23); the REQUIREMENTS.md traceability Status column read
  `Pending` for four already-verified requirements; `docs/DECISIONS.md` and PROJECT.md both still
  specified Postgres port 5432 after the compose file had moved to 5433 on day one; and PROJECT.md
  still listed DATA-02 and TEST-01 as Active after they had shipped in Phase 1.
- **A resolved question stayed phrased as an open one.** The D-11/D-54 contradiction was correctly
  caught at ingest, confirmed by the user, and fixed at source — but PROJECT.md still read "it is
  worth one sentence of confirmation before Phase 5" long after Phase 5 shipped.
- **69 commits went unpushed across the entire build.** `origin/master` sat at Phase 3 *planning*
  while Phases 3, 4 and 5 existed only on one machine. Nothing in the workflow surfaced this; a clean
  working tree was mistaken for safe work.

### Patterns Established

- **Amend a locked decision, never silently rewrite it.** When reality diverged from D-27's port, the
  entry gained a dated, user-approved amendment block explaining *why* and noting that the decision's
  intent was untouched — rather than the number quietly changing.
- **Guard degenerate ranges explicitly.** When a clamp's `lo`/`hi` bounds are themselves derived from
  untrusted input, `Math.Min(Math.Max(v, lo), hi)` silently inverts. Guard `lo > hi` outright.
- **Close the verification loop.** Gap closure is not done when the fix commits — it is done when the
  verification that found the gap has been re-run and says so.
- **Structural fixes beat documented hazards.** CR-03's wrong-server risk was closed by making
  `CanvasDbContextFactory` *throw* rather than guess — the hazard became unreachable instead of
  merely documented.

### Key Lessons

1. **A stale `gaps_found` is worse than no report** — it actively misreports health, and it outlives
   the gaps it describes unless something forces a re-run. Re-verify as the last step of gap closure,
   not as a separate errand.
2. **Code drifts loudly; records drift silently.** Every inconsistency found at milestone close was a
   *record* failing to track reality — no test can catch that class of error. Reconcile tracking files
   at phase close, when the facts are still fresh.
3. **A clean working tree is not a safe one.** "No uncommitted changes" says nothing about whether the
   work exists anywhere but this disk. Push state deserves the same visibility as commit state.
4. **Machine-enforced invariants only hold in the input region the tests reach.** The CHECK constraints
   were correct and the 32-case guard matrix passed — yet CR-01/CR-02 hid in that matrix's blind spot
   (no negative or out-of-canvas coordinates). Coverage of the *decision space* matters more than
   count of cases.
5. **Writing the hardest feature into the definition of done prevents its deferral.** This worked
   exactly as intended and is worth repeating.

### Cost Observations

- Model mix: not tracked this milestone. Executor and verifier agents resolved to `sonnet`;
  orchestration ran on Opus.
- Sessions: not tracked.
- Notable: parallel worktree execution was disabled for this project (`branching_strategy: none`,
  all work committed directly to `master`), so plan-level parallelism was not exercised.

---

## Cross-Milestone Trends

### Process Evolution

| Milestone | Phases | Plans | Key Change |
|-----------|--------|-------|------------|
| v1.0 | 5 | 23 | Baseline — ADR-first planning from 58 locked decisions; gap-closure plans introduced after BC-01 verification |

### Cumulative Quality

| Milestone | Tests | App LOC | Test LOC | Requirements |
|-----------|-------|---------|----------|--------------|
| v1.0 | 405 | ~2,500 | ~2,000 | 15/15 validated |

### Top Lessons (Verified Across Milestones)

*Single milestone so far — lessons above are unverified against a second data point. Revisit when a
future milestone exists to cross-validate.*
