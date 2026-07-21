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

## Milestone: v1.1 — Canvas resize · selection UX · no-JS removal

**Shipped:** 2026-07-21
**Phases:** 3 | **Plans:** 4 | **Tasks:** 9 | **Commits:** 35 since the v1.0 tag | **Elapsed:** ~1 day of execution (roadmap 2026-07-20 → shipped 2026-07-21)

### What Was Built

- **The canvas enlarged to 1472 × 828** with no migration — `CanvasBounds` constants changed, the
  `Home.razor` SVG bound to them, and the geometry edge tests re-pinned to the new inclusive bounds.
  Every figure stored under 1280 × 720 kept its exact position.
- **A real selection lifecycle** — the armed tool survives a draw, the just-drawn figure is selected,
  at most one figure is ever selected, and deselect fires on canvas-outside-figure, arming a tool, or
  any toolbar press except Delete.
- **A topmost blue+white dashed `SelectionTrace`** on the figure's own outline (`pointer-events:none`),
  replacing the red 2px outline — visible even when the selected figure sits behind a later, larger one.
- **The "no hand-authored JavaScript" rule formally retired** across the ADR, the project summary, and
  the derived constraint, with D-06/D-18/D-33/D-37/D-57 re-motivated to MVP simplicity or their own
  independent behavioural rationale. Permissive only: no new JS, no runtime change.
- **Total source footprint: 11 files, +106/−60.** 405/405 tests still passing, unchanged in count.

### What Worked

- **Treating a documentation-only change as a real phase with a real gate.** BC-08 could have been a
  quick edit. Run as a phase, its verifier found that `.planning/intel/constraints.md` — the *derived*
  constraint the agents actually read — still asserted the retired rule while the authoritative ADR
  said it was gone. A "quick edit" to `docs/DECISIONS.md` alone would have left the contradiction live.
- **Verification that proves a negative.** BC-08's gate checked that the implementation commit touched
  *only* doc paths (`git diff --name-only`), plus a clean build and full test run. "We changed no
  behaviour" became a checked claim rather than an assurance.
- **Human verification as its own plan.** Splitting BC-07 into 07-01 (build) and 07-02 (a human
  confirms the five criteria plus the two-tab remote-delete edge on the running app) kept the
  build plan honest and gave the milestone a genuine human sign-off instead of an inferred one.
- **The v1.0 geometry core absorbed a canvas resize as a constant change.** Because clamping and
  normalisation were pure functions over `CanvasBounds`, enlarging the surface touched constants and
  test expectations — not logic. The v1.0 investment paid out exactly where it was supposed to.

### What Was Inefficient

- **v1.0's "records drift silently" lesson recurred — in new files, undetected for the whole
  milestone.** ROADMAP.md's `## What's Next` still read *"v1.1 is active — none has started yet.
  Suggested next step: `/gsd-plan-phase 6`"* after all three phases had shipped and been verified.
  STATE.md's body again contradicted its own frontmatter (`Current focus: … execution complete; phase
  verification pending` while the frontmatter said `completed`) — the identical defect class flagged
  at v1.0 close, in a different file.
- **Stale prose actively misrouted the operator.** The dead `/gsd-plan-phase 6` suggestion above was
  followed at milestone close and had to be stopped by the closed-phase gate. Stale *status* is
  misleading; stale *instructions* cause wrong actions.
- **No milestone audit.** `/gsd-audit-milestone` was never run for v1.1, so the close rested on
  per-phase verifications plus the open-artifact audit rather than a cross-phase integration check.
  Defensible at this size — all four requirements map 1:1 to a single phase and the source diff is 11
  files — but it is a gate that was skipped, not a gate that passed.
- **The BC-08 plan needed three revision passes before it was executable** (external policy scope,
  regression-gate hardening, plan finalisation) — more churn than a 2-task documentation plan should
  need, suggesting the phase's real scope (reconciling three records that disagreed) was not what the
  roadmap described (deleting a constraint).

### Patterns Established

- **A documentation phase earns a behavioural gate.** Build + full test suite + a path-scope assertion
  that the commit touched no source files. It turns "doc-only" from a claim into a verified property.
- **Reconcile the derived layer, not just the source of truth.** The project keeps an authoritative ADR
  (`docs/DECISIONS.md`) *and* a derived constraint file agents actually consume. Amending only the
  authority leaves the derivation lying. Changes to a locked decision must land in both.
- **Retire a rule permissively and say so explicitly.** ARCH-01 removed a constraint while changing
  zero behaviour, and every record says exactly that — re-opened options (Delete-key, `setPointerCapture`,
  Escape-to-cancel) are each named as a *future* decision, so nobody reads permission as intent.

### Key Lessons

1. **Fixing one instance of a defect class does not fix the class.** v1.0's close found records drifting
   from reality and fixed those records. v1.1 reproduced the same failure in different files, because
   what shipped was corrections — not a check. Record drift needs something mechanical.
2. **Forward-looking prose is the most dangerous thing to leave stale.** A stale status line misinforms;
   a stale "next step: run X" issues an instruction that will be followed. Sections that tell the
   operator what to do next must be rewritten at every close, or not written at all.
3. **The derived layer is where policy actually lives.** The retired-JS rule survived in
   `.planning/intel/constraints.md` after the ADR had retired it — and that file is what gets read
   during planning. Authority and derivation drift apart silently.
4. **Skipping a gate is a decision worth recording.** The v1.1 audit was skipped for defensible
   reasons; those reasons belong in the record, so a future reader can tell "audited clean" from
   "never audited".

### Cost Observations

- Model mix: executor and verifier agents resolved to `sonnet`; orchestration and planning ran on Opus.
- Sessions: not tracked.
- Notable: parallelism again unexercised — 3 of 4 plans were single-plan waves, and
  `branching_strategy: none` keeps all work on one branch. Wave machinery cost nothing and bought
  nothing at this milestone's size.

---

## Cross-Milestone Trends

### Process Evolution

| Milestone | Phases | Plans | Key Change |
|-----------|--------|-------|------------|
| v1.0 | 5 | 23 | Baseline — ADR-first planning from 58 locked decisions; gap-closure plans introduced after BC-01 verification |
| v1.1 | 3 | 4 | Documentation-only phase given a full verification gate; human verification split into its own plan; milestone audit skipped |

### Cumulative Quality

| Milestone | Tests | App LOC | Test LOC | Requirements |
|-----------|-------|---------|----------|--------------|
| v1.0 | 405 | ~2,500 | ~2,000 | 15/15 validated |
| v1.1 | 405 | ~2,500 | ~2,000 | 4/4 validated (19/19 cumulative) |

### Top Lessons (Verified Across Milestones)

1. **Records drift silently; code drifts loudly.** — *Confirmed in v1.0 and v1.1.* Found at both closes,
   in different files each time, caught by no test either time. This is now the project's most
   reproducible failure mode. v1.0 called it a lesson; v1.1 proves it needs a mechanism, not a reminder.
2. **Writing the hardest thing into the definition of done prevents its deferral.** — *v1.0 (live sync);
   partially re-confirmed in v1.1*, where human verification was made a plan rather than a hope, and
   duly happened.
3. **Investment in pure, framework-free primitives compounds.** — *v1.0 built the geometry core; v1.1
   resized the entire canvas by changing constants.* No logic changed because there was no logic
   outside `Geometry/` to change.
