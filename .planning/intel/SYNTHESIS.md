# Synthesis

Entry point for downstream consumers (`gsd-roadmapper`). Produced by `gsd-doc-synthesizer`.

Mode: **new** (net-new bootstrap — no pre-existing `.planning/` context).
Precedence: `ADR > SPEC > PRD > DOC` (unused — single-document ingest).

> ⚠️ **v1.1 AMENDMENTS (2026-07-20) — this v1.0-synthesized intel is partly superseded.**
> Authoritative now: `docs/DECISIONS.md` (inline `⚠️ v1.1` notes) and `.planning/PROJECT.md`.
> Changed: **canvas 1280×720 → 1472×828** (size may GROW, never SHRINK); **selection indicator =
> thin blue+white dashed trace on the figure's own outline, topmost** (was red 2px) + a **selection
> lifecycle** (tool stays armed after a draw, one figure selected at a time, deselect on
> canvas-outside-figure / arm-tool / toolbar-press-except-Delete); **the "no hand-authored
> JavaScript" rule is REMOVED** (never load-bearing — MVP simplicity was the real motivation;
> D-06/18/33/37/57 re-worded). Next milestone **v1.2** (new figures + dynamic toolbar) is scoped in
> `.planning/backlog/v1.2-figures-and-toolbar.md`.

---

## Input

| | |
|---|---|
| Documents ingested | **1** |
| By type | ADR ×1 (`docs/DECISIONS.md`) |
| Locked | yes (manifest-declared, precedence 0) |
| Cross-refs | none — the document declares itself the only specification |
| Cycle detection | ran; single-node graph, **no cycles** |
| Unknown / low-confidence docs | 0 |

The source is a **consolidated ADR set**: 58 numbered decisions (D-01…D-58) in one file, plus three
authoritative artifact sections. It carries its own internal history, including one full reversal.

---

## Extracted

| Artifact | Count | File |
|---|---|---|
| Decisions (locked, current) | **58** (D-01…D-58; 9 carry superseded text that was excluded) | `.planning/intel/decisions.md` |
| Requirements (derived) | **15** | `.planning/intel/requirements.md` |
| Constraints | **11** (1 schema, 5 invariant, 1 protocol, 4 nfr) | `.planning/intel/constraints.md` |
| Context topics | **8** | `.planning/intel/context.md` |

**Requirements are DERIVED, not ingested** — the set contains no PRD. Every REQ traces to specific
D-numbers, and no acceptance criterion was invented. Consequently there are **no competing
acceptance variants**.

Requirement IDs: `REQ-login`, `REQ-session`, `REQ-logout`, `REQ-one-canvas-per-user`,
`REQ-canvas-surface`, `REQ-toolbar`, `REQ-draw-figure`, `REQ-select-figure`, `REQ-drag-figure`,
`REQ-delete-figure`, `REQ-persistence`, `REQ-live-sync`, `REQ-staleness-guard`, `REQ-save-failure`,
`REQ-tests`.

---

## The three authoritative artifacts

The source document's own "READ THIS FIRST" index names three things to trust over everything else.
All three are extracted verbatim into `constraints.md`:

1. **`THE SCHEMA`** — the canonical DDL. Two tables (`users`, `figures`), four integer coordinates,
   three per-type CHECK constraints, one index, one `COMMENT ON TABLE`. Supersedes D-12's sketch.
2. **`D-22` (revised)** — geometry storage. Every figure is four integers that are **always its
   bounding box**; a **circle is stored as its inscribed square**. The earlier "centre + rim point"
   encoding was REVERSED and is dead.
3. **`D-53`** — the canonical broadcast message contract. Kinds: `draw`, `move`, `delete`,
   `rollback`. `move` is UPDATE-ONLY. No `drop` kind.

---

## Staleness handling

The source contains **9 entries whose text is partly dead**, each flagged in-source. Superseded
content was **excluded** from the extracted decisions and is preserved only as labelled history in
`decisions.md` § "Superseded history", so it can never be re-introduced:

D-05 (amended by D-13) · D-07 (retracted by D-34) · D-11 (amended by D-40/D-47/D-53/D-54 — its
"idempotent upsert" is a **bug**) · D-12 (DDL sketch stale) · D-15 (Delete key → D-33) · D-16
("four" buttons → six) · **D-22 (REVERSED — inscribed square, not centre+rim)** · D-23 ("one shared
guard" → per-type, D-50) · D-30 ("five" buttons → six).

Plus one stale claim the source's own index does NOT flag: **D-32 §1** restates D-23's shared-guard
framing (see INFO in the conflicts report).

---

## Conflicts

| Bucket | Count |
|---|---|
| BLOCKERS | **0** |
| WARNINGS | **1** |
| INFO (auto-resolved / recorded) | **12** |

Full report: `.planning/INGEST-CONFLICTS.md`

**The single WARNING — needs a user decision before routing:**
D-11's "irreducible core" item 4 claims D-54 narrowed the mid-drag receive filter to *only the
figure being dragged*. **D-54 decides the opposite** — it discards ALL incoming broadcasts mid-drag
and explicitly lists the narrow filter under *Rejected*. Synthesis applied **D-54 wins** (later,
dedicated, locked entry; the index lists D-11 as amended by D-54). Confirm before implementation —
an implementer following D-11's checklist would build the rejected filter.

No BLOCKERs: every other contradiction in the source names its own winner.

---

## Signal for planning

The source's own audit note is worth carrying forward: this log is **airtight wherever the human was
in the room** (data model, sync semantics, geometry, scope) and was **silent wherever the framework
was in the room** (cookies, HTTP, pointer capture, keyboard focus, schema creation, layout constants,
error paths) until D-33…D-58 closed those gaps.

Risk is therefore concentrated in **framework seams** and **silent geometric bugs** — which is
exactly where the three mandated tests point (D-49): the clamp maths, the circle inscribed-square
round-trip, and the line-normalisation landmine.

Hard project-wide constraints that shape every phase:
- ~~No JavaScript anywhere~~ **REMOVED in v1.1** — hand-authored JS/interop is now permitted; the
  rule was never load-bearing (D-06/18/33/37/57 re-worded to MVP-simplicity motivations)
- **No Save button; every operation persists immediately** (D-09)
- **Plaintext passwords, locked and deliberate** (D-08) — do not "fix" without a new decision
- **1472 × 828** *(v1.1; was 1280 × 720)***, inclusive bounds, 48px toolbar** — `canvasY = PageY − 48`
  is the constant every coordinate flows through (D-19, D-36, D-43)
