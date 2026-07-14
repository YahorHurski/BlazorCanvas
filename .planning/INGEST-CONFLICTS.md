## Conflict Detection Report

Mode: new (net-new bootstrap — no pre-existing `.planning/` context to contradict).
Sources: 1 document — `docs/DECISIONS.md` (ADR, locked, precedence 0).
Cross-ref graph: single node, no edges — cycle detection ran, no cycles.

Single-document ingest ⇒ **no cross-document precedence conflicts are possible.** All entries below
are *internal* contradictions within the one source. Per the ingest brief, a superseded entry
contradicting its successor is intentional, documented history and is recorded as INFO — not a
blocker.

### BLOCKERS (0)

None. Every contradiction found in the source names its own winner, either in the "READ THIS FIRST"
status index or in the superseding entry's own ⚠️ banner. No decision was left with two live,
unranked sides.

### WARNINGS (1)

[WARNING] D-11's summary of D-54 states the OPPOSITE of what D-54 actually decides
  Found: D-11 "Irreducible core" item 4 reads — "Ignore incoming broadcasts about the figure you are
    currently dragging — see D-54. (This originally read `if (_dragging) return;`, which discarded
    ALL incoming messages mid-drag, including draws and deletes of unrelated figures. Corrected by
    D-54.)" — i.e. D-11 claims D-54 NARROWED the rule to the dragged figure only.
    D-54 itself (title: "Mid-drag, a tab ignores ALL incoming broadcasts", Status: Locked) decides
    the reverse: it KEEPS `if (_dragging) return;`, discards every incoming broadcast, and
    explicitly lists "ignoring only messages about the figure currently being dragged" under
    **Rejected**. It even books the cost of the blanket rule (multi-device messages lost mid-drag).
  Impact: The two entries specify two different, incompatible receive filters. An implementer
    reading D-11's core-rules checklist — which is exactly the list the log tells you to implement
    from — will build the narrow filter that D-54 explicitly rejected. This is a behavioural
    difference (unrelated draws/deletes applied vs dropped mid-drag), not a wording nit.
  Resolution applied in synthesis: **D-54 wins** — it is the later, dedicated, locked entry on this
    exact question, the status index lists D-11 as amended by D-54, and D-54 argues the point
    directly whereas D-11 only paraphrases it. `.planning/intel/decisions.md` and
    `constraints.md` therefore record the blanket rule: mid-drag, discard ALL incoming broadcasts.
  → Confirm this is the intended rule (blanket discard). If the narrow filter was actually wanted,
    D-54 must be rewritten — do not "fix" it by editing D-11's checklist, since D-54's Rejected
    section would still contradict it.

### INFO (12)

[INFO] Auto-resolved: D-13 amends D-05 — the four shapes do not share one draw interaction
  Note: D-05's original claim ("all four are created by the same interaction — drag out a bounding
  box") is dead. Line/rectangle/triangle are corner-to-corner; the circle is drawn centre-out.
  Storage remains uniform (D-22). Synthesis extracted the amended form only.

[INFO] Auto-resolved: D-34 retracts D-07's "no HTTP code to write"
  Note: an InteractiveServer component cannot set a cookie, so login/logout are static-SSR pages and
  endpoints and the app runs two render modes. D-07's *hosting* choice (Blazor Server, no Web API)
  stands unchanged; only the boast is retracted. Synthesis records the two-render-mode reality.

[INFO] Auto-resolved: D-40/D-47/D-53/D-54 amend D-11 — the "idempotent upsert" was a BUG
  Note: an upsert inserts when the row is absent, which lets a stale tab's drag broadcasts resurrect
  a figure another tab correctly deleted. Current rule: apply is idempotent AND `move` is
  UPDATE-ONLY (unknown figure → ignore entirely); a zero-row UPDATE broadcasts a delete. The
  throttle is 50 ms with a guaranteed trailing edge (not the "~30–50 ms" range D-11 mentions), and
  the notifier is keyed by `user_id` (not username). The upsert text was NOT extracted.

[INFO] Auto-resolved: `THE SCHEMA` supersedes D-12's DDL sketch
  Note: D-12's sketch still shows the `created_at` column that D-46 dropped. Only D-12's *two-table,
  no-canvases-table* principle is normative. `constraints.md` carries the canonical DDL from
  `THE SCHEMA` verbatim, including all three CHECK constraints, the index, and the COMMENT.

[INFO] Auto-resolved: D-33 supersedes the Delete-key half of D-15
  Note: Blazor has no document-level key listener without JavaScript, so Delete is a toolbar button.
  D-15's *selection* half stands. Also superseded: D-15's note that "a line needs an invisible wider
  hit area" — D-32 explicitly declined it and D-58 substitutes a 2px stroke.

[INFO] Auto-resolved: the toolbar is SIX buttons — D-16 ("four") and D-30 ("five") are both stale
  Note: `[pointer] [line] [rectangle] [circle] [triangle] [delete]`. D-30 added the pointer, D-33
  added delete. D-30's rationale and mode semantics stand; only its button count is stale. Logout
  sits in the same strip (D-56) but is not one of the six.

[INFO] Auto-resolved: D-22 REVERSED — a circle is stored as its INSCRIBED SQUARE
  Note: the earlier "centre + rim point" encoding (`r = x2 − x1`) is dead and was NOT extracted.
  Its raw columns were not the bounding box, so a generic edge-clamp would have permitted 90 px of
  circle hanging off the canvas. Current: four integers that are ALWAYS the bounding box;
  `r = (x2−x1)/2`, `cx = x1+r`, `cy = y1+r`; enforced by `circle_is_a_circle`. The source is explicit
  that D-24 (clamping) is what forced the reversal.

[INFO] Auto-resolved: D-53 supersedes the message-payload fragments in D-11, D-22 and D-40
  Note: D-22's "one uniform sync payload `{id, type, x1, y1, x2, y2, sender}`" is stale — D-53's
  `move` carries **no `type`**. D-53 is the single authoritative contract (kinds: draw, move, delete,
  rollback; no `drop` kind). Only D-53 was extracted.

[INFO] Auto-resolved: D-50 retracts D-23's "one shared minimum-size guard"
  Note: the guard is PER-TYPE. A shared guard lets a zero-height rectangle drag through (start ≠ end)
  and the INSERT then violates `box_is_a_box`, surfacing a wrong "is the database running?" message.
  A zero-height *line* is legal (that is a horizontal line); a zero-height rectangle is not.
  D-23's guard 2 ("never clamp coordinates individually") is unaffected and stands.

[INFO] Auto-resolved: D-36 corrects D-23's stated reason for omitting bounds CHECK constraints
  Note: D-23 said bounds CHECKs were omitted because "a figure legitimately overhanging the edge
  would violate them". D-24/D-29 later established that figures live entirely inside the canvas,
  always — so that reason is stale. The decision (no bounds CHECKs) stands; only the reason changed:
  they would be belt-and-braces on a rule the app never breaks.

[INFO] Unflagged staleness: D-32's restatement of the shared guard
  Note: D-32 §1 describes "the minimum-size guard" as a single shared rule that "rejects only truly
  zero-size draws" — the same claim D-50 later retracts — but the status index does NOT flag D-32.
  Its *substance* survives (the guard was deliberately not raised to a ~5-px threshold, so a stray
  1–2 px drag still creates a tiny figure), so nothing was lost; only its "one shared guard" framing
  is superseded by D-50. Recorded because it is the one stale claim the index does not mark.

[INFO] Non-conflict: D-31's "e.g. blue" selection colour vs D-58's "red, 2px"
  Note: D-31 offers the colour illustratively ("a distinct coloured outline (e.g. blue)"); D-58 is
  the constants entry and pins it to **red, 2px**. Not a contradiction, but the two colours appear in
  the same document and D-31 is the more prominent entry — synthesis extracted **red**.
