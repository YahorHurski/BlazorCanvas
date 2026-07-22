---
phase: BC-12-regression-verification
verified: 2026-07-22T17:20:32+02:00
status: gaps_found
score: 2/5 must-haves verified
behavior_unverified: 2
overrides_applied: 0
gaps:
  - truth: "A human confirms every visible canvas behavior is indistinguishable from v1.1."
    status: failed
    reason: "The documented human acceptance run was not approved: while creating a figure, the initiating tab showed no in-progress preview; the figure first appeared after mouse release. This is a visible regression from the required v1.1 behavior."
    artifacts:
      - path: "src/BlazorCanvas/Components/Pages/Home.razor"
        issue: "The source contains local preview state and render wiring, but the running application did not visibly render that state during the draw gesture; no behavioral test exercises the rendered preview."
      - path: "src/BlazorCanvas/Components/Canvas/FigureShape.razor"
        issue: "The preview renderer accepts preview geometry, but runtime visual output was not established by automated evidence and failed the human observation."
    missing:
      - "A working initiating-tab-only in-progress drawing preview throughout the pointer-down draw gesture."
      - "Behavioral coverage proving preview visibility locally while ensuring no preview is sent to the second tab before committed creation."
behavior_unverified_items:
  - truth: "A human exercises selection and confirms it behaves identically to v1.1, including the blue-and-white dashed trace."
    test: "Run the documented selection and deselection routes in a normal browser window after the preview gap is corrected."
    expected: "Exactly one selected figure shows the topmost blue-and-white dashed trace, and every documented deselect route removes it."
    why_human: "The failed run stopped at the first drawing-preview divergence; source inspection and the existing render-contract test do not prove interactive visual behavior."
  - truth: "A human opens two same-account browser windows and confirms a drag glides live in the second window."
    test: "After correcting the preview gap, drag a committed figure slowly for two to three seconds in window A while watching window B."
    expected: "Window B visibly glides through intermediate positions and only receives newly drawn figures after they are committed."
    why_human: "Existing tests exercise persistence and sync protocol, but not the full two-window visual acceptance run; the failed run stopped before this observation."
---

# Phase BC-12: Regression Verification Report

**Phase Goal:** A human confirms, on the running application, that the storage model rewrite is invisible — every user-facing behavior is indistinguishable from v1.1.

**Verified:** 2026-07-22

**Status:** gaps_found

**Re-verification:** No — initial verification

## Goal Achievement

### Observable Truths

| # | Truth | Status | Evidence |
| --- | --- | --- | --- |
| 1 | A human draws all four shapes with edge clamping, drags each, deletes them, and confirms behavior matches v1.1. | ✗ FAILED | The human did not approve the run: no in-progress drawing preview appeared in the initiating tab; the new figure appeared only after mouse release. |
| 2 | A human confirms selection and all documented deselect routes, including the blue-and-white dashed trace, match v1.1. | ⚠️ PRESENT_BEHAVIOR_UNVERIFIED | The acceptance script stopped at its first failure. Source and static render-contract checks cannot establish the visual interaction outcome. |
| 3 | A human confirms a drag visibly glides in a second same-account window in real time. | ⚠️ PRESENT_BEHAVIOR_UNVERIFIED | The acceptance script stopped before this two-window observation. Phase 11 automated protocol evidence is not a substitute for this human acceptance criterion. |
| 4 | Build, full test suite, Docker, HTTPS certificate, and one app process were healthy before acceptance. | ✓ VERIFIED | `12-01-SUMMARY.md` records healthy Docker, clean build, 296 passing tests, trusted HTTPS, and one host returning HTTP 200. |
| 5 | A failed run preserves evidence and introduces no implementation work. | ✓ VERIFIED | The summary records the first divergence and retained log paths; the phase scope remained acceptance-only. |

**Score:** 2/5 must-haves verified (2 present but behavior-unverified)

## Required Artifacts

| Artifact | Expected | Status | Details |
| --- | --- | --- | --- |
| `.planning/phases/BC-12-regression-verification/12-01-SUMMARY.md` | REG-01 outcome, checklist, and evidence locations | ✓ VERIFIED | Substantive failed-run record: exact observed/required behavior, preflight results, and retained log locations. |
| `src/BlazorCanvas/Components/Pages/Home.razor` | Local state and render path for drawing preview | ⚠️ PRESENT, RUNTIME FAILURE | `drawing`, `previewType`, and `previewPlacement` are populated by pointer handlers and rendered through `FigureShape`, but the documented running-app observation shows no preview. |
| `src/BlazorCanvas/Components/Canvas/FigureShape.razor` | Render supplied preview placement without selecting or syncing it | ✓ WIRED IN SOURCE | Accepts `PreviewPlacement`/`PreviewType` and derives geometry and local transform. It has no notifier dependency; source inspection supports the required local-only boundary but does not override the failed runtime visual result. |

## Key Link Verification

| From | To | Via | Status | Details |
| --- | --- | --- | --- | --- |
| `Home.razor` pointer-down/move state | `FigureShape` preview renderer | `drawing && previewPlacement is not null` conditional render | ⚠️ PRESENT, NOT BEHAVIORALLY PROVEN | The source link exists, but the human observed no visible preview during the gesture and no targeted behavioral test was found. |
| Local preview state | Cross-window notifier | No draw-preview notifier message | ✓ VERIFIED IN SOURCE | Preview construction occurs in `Home`; committed creation occurs only in `CommitDrawAsync` through `coordinator.DrawAsync`. The preview component does not invoke sync. The required follow-up must retain this boundary. |
| One local process | Two same-profile browser windows | Shared session and process-local notifier | ⚠️ HUMAN CHECK REMAINS | Preflight established one host; the failed run did not complete the visual glide check. |

## Behavioral Spot-Checks

| Behavior | Command | Result | Status |
| --- | --- | --- | --- |
| Preview-specific automated coverage | `dotnet test BlazorCanvas.sln --list-tests --nologo` and source inventory | No test named or scoped to in-progress drawing preview was found; `V11RenderContractTests` checks static renderer tokens only. | ⚠️ NOT COVERED |
| Live application acceptance | Not run by verifier | The verifier was directed not to run the application; the recorded human result is authoritative for this gate. | ✗ FAILED / INCOMPLETE |

## Requirements Coverage

| Requirement | Source Plan | Description | Status | Evidence |
| --- | --- | --- | --- | --- |
| REG-01 | `12-01-PLAN.md` | Human confirmation that drawing, edge clamp, dragging, deletion, selection, and two-window glide are indistinguishable from v1.1. | ✗ BLOCKED | Human acceptance was explicitly **not approved** at the local drawing-preview step. |

## Anti-Patterns Found

| File | Line | Pattern | Severity | Impact |
| --- | --- | --- | --- | --- |
| `src/BlazorCanvas/Components/Pages/Home.razor` | 29-31, 94-128 | Preview code is present but has no behavioral test validating rendered pointer-move output. | BLOCKER (for REG-01) | Presence and wiring did not deliver the human-visible requirement. |

## Human Verification Required

The failed gap must be corrected first. Then rerun the entire seven-step two-window REG-01 script, not only the preview check. In particular, preserve this boundary:

1. While the initiating tab is drawing, it visibly renders the evolving local preview.
2. The preview is never broadcast or rendered in the second tab.
3. The second tab receives the figure only after committed creation, and committed-figure drag still visibly glides there.

## Gaps Summary

REG-01 is not achieved. Automated preflight passed, but it cannot replace a human-observed visual requirement. The current source contains an apparent local-preview render path, yet the documented acceptance run observed the exact opposite runtime behavior. The corrective work must make the preview visible in the initiating tab during the draw gesture, retain it as local-only state, and avoid broadcasting it; the other tab must continue to show the figure only after creation commits. No later roadmap phase explicitly schedules this correction, so it remains an actionable blocking gap.

---

_Verified: 2026-07-22T17:20:32+02:00_
_Verifier: generic-agent workaround acting as independent GSD verifier_

## Debug-Fix Reverification Addendum

**Recorded:** 2026-07-22
**Scope:** The blocker that caused the original run to stop.

The preceding failed evidence is preserved. The preview implementation was corrected after diagnosis: the temporary SVG is now drawn directly in the initiating browser with pointer capture, and it never enters shared state, persistence, or `CanvasSyncNotifier`.

The reporter approved the two-tab retest:

1. The initiating tab visibly renders the preview during drawing.
2. The second tab remains unchanged while the pointer is held.
3. The created figure appears in the second tab only after release/commit.

`dotnet test BlazorCanvas.sln --nologo -c Release --no-restore` also passed **303/303**, and JavaScript syntax/diff checks passed. This closes the preview-specific blocking gap. Phase-level REG-01 remains **partially verified** until the remaining documented human checks (four-shape clamp, selection/deselection, drag/delete, and slow committed-drag glide) are rerun and recorded.
