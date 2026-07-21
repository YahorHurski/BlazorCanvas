# Phase BC-08: Architecture Constraint Cleanup - Research

**Researched:** 2026-07-21  
**Domain:** Repository policy-document reconciliation and documentation-only change control  
**Confidence:** LOW (the configured confidence seam classifies the direct codebase provider as LOW; the findings below are still directly reproducible from the named local artifacts.)

<phase_requirements>
## Phase Requirements

| ID | Description | Research Support |
|----|-------------|------------------|
| ARCH-01 | Retire the no-hand-authored-JavaScript constraint; recast D-06/D-18/D-33/D-37/D-57 as MVP-simplicity decisions; make no runtime or JS change. | [VERIFIED: repository audit] The authoritative ADR and PROJECT summary already contain the v1.1 permissive amendment; the audit identifies one still-active derived constraint that must be reconciled and a reproducible no-runtime-change gate. |
</phase_requirements>

## Summary

[VERIFIED: `docs/DECISIONS.md`, `.planning/PROJECT.md`, `.planning/ROADMAP.md`, `.planning/REQUIREMENTS.md`] ARCH-01 is a policy/documentation reconciliation, not a framework migration. The authoritative decision record says the former restriction is removed and each of D-06, D-18, D-33, D-37, and D-57 already has a v1.1 correction. The product decisions themselves remain: SVG is chosen for less code and DOM hit-testing; fixed 1:1 sizing is chosen for MVP simplicity while aspect ratio is geometry; the toolbar Delete, drag termination, and no draw-abort are current MVP/behaviour choices rather than technology prohibitions.

[VERIFIED: `.planning/intel/constraints.md`] One material contradiction remains outside the two files named by the pre-existing plan: `CONSTRAINT-env` calls “NO JAVASCRIPT ANYWHERE” a hard, load-bearing constraint and attributes D-18/D-33/D-37/D-57 to it. Its opening v1.1 amendment says the opposite, so the same document currently gives downstream planning both policies. Treat this as an in-scope derived-document correction; otherwise a future planner can reintroduce the retired rule while following an artifact labelled executable/normative.

[VERIFIED: tracked source and test audit] No tracked production or test file contains `no hand-authored JavaScript`, `no JS`, `hand-authored`, or the longer retired-policy variants. The source tree has one tracked JS file, `src/BlazorCanvas/Components/Layout/ReconnectModal.razor.js`, and one unused `@using Microsoft.JSInterop`; neither is evidence of new interop or a policy claim. No packages, database migration, code, Razor, static asset, or runtime service change belongs in this phase.

**Primary recommendation:** Reconcile the three current policy artifacts (`docs/DECISIONS.md`, `.planning/PROJECT.md`, and `.planning/intel/constraints.md`), explicitly label retained historical references, then prove the resulting diff is documentation/planning only before running the existing solution build and test gates.

## Architectural Responsibility Map

| Capability | Primary Tier | Secondary Tier | Rationale |
|------------|-------------|----------------|-----------|
| Retired-rule wording and D-06/D-18/D-33/D-37/D-57 rationale | Documentation / planning artifacts | — | [VERIFIED: `docs/DECISIONS.md`] The ADR is the declared source of truth and the phase changes policy text only. |
| Project-facing constraint summary | Documentation / planning artifacts | — | [VERIFIED: `.planning/PROJECT.md`] PROJECT.md mirrors the ADR for future work. |
| Derived executable/normative constraints | Documentation / planning artifacts | — | [VERIFIED: `.planning/intel/constraints.md`] The stale `CONSTRAINT-env` entry is consumed as a project constraint and must not contradict the authoritative amendment. |
| Runtime preservation evidence | Build/test and Git diff gates | Application runtime | [VERIFIED: `.planning/config.json`, `08-01-PLAN.md`] Existing build/test commands and path-scoped diff checks detect an accidental expansion beyond the documentation surface. |

## Standard Stack

### Core

| Tool / artifact | Version | Purpose | Why standard here |
|-----------------|---------|---------|-------------------|
| `docs/DECISIONS.md` | repository current | Authoritative policy record | [VERIFIED: `.planning/PROJECT.md`] PROJECT.md identifies it as the source of truth. |
| `git grep` / `rg` | locally installed | Exact, case-insensitive policy-family audit | [VERIFIED: repository audit] They distinguish audited source/docs from generated `bin`, `obj`, `.git`, and `.vs` material. |
| `git diff --name-only` | locally installed | Enforce documentation-only scope | [VERIFIED: `08-01-PLAN.md`] The phase explicitly requires a path allowlist before runtime validation. |
| `dotnet build BlazorCanvas.sln` and `dotnet test BlazorCanvas.sln --nologo` | project-configured | Regression gate | [VERIFIED: `.planning/config.json`] These are the configured project build/test commands. |

### Supporting

| Tool / artifact | Purpose | When to use |
|-----------------|---------|-------------|
| `.planning/PROJECT.md` | Current project constraint summary | [VERIFIED: `.planning/PROJECT.md`] Reconcile every current-policy statement with the ADR. |
| `.planning/intel/constraints.md` | Derived normative constraints | [VERIFIED: `.planning/intel/constraints.md`] Update the stale `CONSTRAINT-env` language in the same change set. |
| `.planning/milestones/` and completed phase artifacts | Historical evidence | [VERIFIED: repository audit] Preserve unchanged when the artifact identifies its milestone/phase as historical; do not treat it as current policy. |

### Alternatives Considered

| Instead of | Could use | Tradeoff |
|------------|-----------|----------|
| Targeted tracked-file audit | Blind repository-wide replace | [VERIFIED: repository audit] A replacement would corrupt legitimate historical evidence and unrelated “no JS required” implementation notes. |
| Existing shell gates | New policy-linter script | [VERIFIED: phase scope] A new script is unnecessary runtime/project surface for a one-off wording cleanup. |

**Installation:** None. [VERIFIED: phase scope] This phase adds no package or external dependency.

## Package Legitimacy Audit

Not applicable. [VERIFIED: `.planning/ROADMAP.md`, `.planning/REQUIREMENTS.md`] ARCH-01 is documentation-only and installs no packages.

## Architecture Patterns

### System Architecture Diagram

```text
Authoritative ADR (docs/DECISIONS.md)
             |
             v
Current project summary (.planning/PROJECT.md) -----> future planning/execution
             |
             v
Derived constraints (.planning/intel/constraints.md)
             |
             v
Policy audit + diff allowlist ---> build/test gates ---> documented phase evidence
                                      |
                                      +--> unchanged runtime/source/JS surface
```

### Recommended Project Structure

```text
docs/DECISIONS.md                         # authoritative wording
.planning/PROJECT.md                      # current project summary
.planning/intel/constraints.md            # derived normative constraint; reconcile stale entry
.planning/phases/BC-08-.../08-RESEARCH.md # research evidence
.planning/phases/BC-08-.../08-01-PLAN.md  # executable change and verification steps
```

### Pattern 1: Authoritative-source reconciliation

**What:** Edit the ADR first, then mirror its final wording into current and derived planning documents.  
**When to use:** [VERIFIED: `.planning/PROJECT.md`, `.planning/intel/constraints.md`] Whenever an amended locked decision has summaries/extractions that can be read independently.  
**Example:**

```powershell
# Audit tracked source and test files for active policy wording.
git grep -n -i -E 'no hand-authored JavaScript|no JS|hand-authored' -- src tests

# Fail if the phase diff touches any runtime path.
$allowed = @('docs/DECISIONS.md', '.planning/PROJECT.md', '.planning/intel/constraints.md')
$unexpected = git diff --name-only | Where-Object { $_ -and $_ -notin $allowed }
if ($unexpected) { $unexpected; exit 1 }
```

### Pattern 2: Classify, do not erase, historical references

**What:** Keep a legacy phrase only if adjacent wording explicitly says it is removed, superseded, historical, or a past milestone fact.  
**When to use:** [VERIFIED: `.planning/ROADMAP.md`, `.planning/RETROSPECTIVE.md`, `.planning/milestones/`] The repository intentionally retains v1.0 plans, summaries, verification, and retrospective material.  
**Example classification:**

| Match category | Action |
|----------------|--------|
| Current active statement | Correct it in a current artifact. |
| Current statement that says JS is permitted / rule removed | Retain. |
| Past-milestone artifact that identifies v1.0 or a completed phase | Retain as history. |
| Future backlog that says JS is permitted | Retain as future-context evidence. |
| “No JS is required” implementation observation | Retain only when it does not assert prohibition. |

### Anti-Patterns to Avoid

- **Two-file-only scope:** [VERIFIED: `.planning/intel/constraints.md`] Do not leave the active `CONSTRAINT-env` prohibition merely because the existing plan lists only the ADR and PROJECT.md.
- **Literal zero-hit requirement:** [VERIFIED: repository audit] Do not fail on historical/milestone evidence or permissive statements; review context for every hit.
- **Policy cleanup as a feature opportunity:** [VERIFIED: `.planning/REQUIREMENTS.md`] Do not add Delete-key, `setPointerCapture`, Escape-to-cancel, a JS file, or interop. Each is a future decision.
- **Generic `InvokeAsync` scan interpreted as JS interop:** [VERIFIED: source audit] Component callback `InvokeAsync` calls are not `IJSRuntime` calls; scan exact interop types separately.

## Don't Hand-Roll

| Problem | Don't build | Use instead | Why |
|---------|-------------|-------------|-----|
| One-off policy enforcement | New custom analyzer/linter or runtime test fixture | [VERIFIED: project tooling] `git grep`, a diff allowlist, and existing build/test commands | The phase’s acceptance criteria are textual plus unchanged-runtime evidence; new tooling enlarges scope. |
| Historical-reference handling | Blind string deletion | [VERIFIED: repository audit] Context classification with explicit history/supersession labels | Past plans and milestone evidence are intentionally retained. |

**Key insight:** [VERIFIED: `.planning/intel/constraints.md`] The risk is not absence of a JavaScript capability; it is a contradictory derived constraint being treated as active by future planning.

## Runtime State Inventory

| Category | Items Found | Action Required |
|----------|-------------|-----------------|
| Stored data | [VERIFIED: phase scope and source audit] None. The retired phrase is not a database key, schema name, persisted user value, or application record identifier. | No data migration; documentation edit only. |
| Live service config | [ASSUMED] No external dashboard, CI variable, hosted service setting, or workflow configuration is in scope or evidenced in this repository. | No service update. If an operator has copied the old rule into a non-repository service description, correct it separately; it is not discoverable from Git. |
| OS-registered state | [VERIFIED: repository audit] None. No task, service, launch registration, or process name uses this policy phrase. | No re-registration. |
| Secrets and env vars | [VERIFIED: tracked source/config audit] None. The policy is not an environment-variable or secret-key contract. | No secret/env rename or rotation. |
| Build artifacts / installed packages | [VERIFIED: tracked source audit] No package or product rename is involved. Existing `bin`/`obj` outputs are excluded from the policy audit because they are generated and will be regenerated by build. | No reinstall or artifact migration; do not edit generated output. |

**Canonical runtime-state conclusion:** [VERIFIED: repository audit] After the documentation files are reconciled, no runtime system has an old identifier, key, or registration to migrate. The only residual uncertainty is an out-of-repository copy of prose, recorded above as an assumption rather than silently treated as audited.

## Common Pitfalls

### Pitfall 1: A derived document contradicts the source of truth

**What goes wrong:** A future plan reads `.planning/intel/constraints.md` and restores the rule as if it were load-bearing.  
**Why it happens:** [VERIFIED: `.planning/intel/constraints.md`] Its top v1.1 amendment permits hand-authored JS, while `CONSTRAINT-env` still says “NO JAVASCRIPT ANYWHERE.”  
**How to avoid:** Update the `CONSTRAINT-env` entry to state the former rule is retired, the five rationale changes, and the no-runtime-change fence.  
**Warning signs:** A search returns both “removed/permitted” and “hard constraint/load-bearing” in current planning artifacts.

### Pitfall 2: Historical evidence is mistaken for an active prohibition

**What goes wrong:** A historical plan or verification report is edited or deleted to force a zero-hit search result.  
**Why it happens:** [VERIFIED: `.planning/milestones/` and `.planning/RETROSPECTIVE.md`] v1.0 evidence truthfully records its past no-JS implementation state.  
**How to avoid:** Require an explicit historical, superseded, or permissive context around retained matches; use tracked-file paths and context inspection.  
**Warning signs:** The proposed diff expands into milestone archives or completed phase records.

### Pitfall 3: Verification itself changes the product surface

**What goes wrong:** The phase adds an interop test, JS file, or lint utility to prove no new JS was added.  
**Why it happens:** Policy requirements can be mistaken for runtime requirements.  
**How to avoid:** [VERIFIED: `.planning/REQUIREMENTS.md`] Use the existing build/test commands and Git diff allowlist only.  
**Warning signs:** Any `src/`, `tests/`, `wwwroot/`, `.csproj`, `.razor`, or `.js` path appears in `git diff --name-only`.

## Code Examples

### Full tracked-policy audit

```powershell
# Review all policy-family hits, but exclude Git metadata and generated build outputs.
rg -n -i --hidden --no-ignore \
  --glob '!.git/**' --glob '!**/bin/**' --glob '!**/obj/**' --glob '!.vs/**' \
  'no hand-authored JavaScript|no JS|hand-authored|no application-authored JavaScript|no JavaScript anywhere' .

# A source/test-only check must print no policy-family matches.
git grep -n -i -E 'no hand-authored JavaScript|no JS|hand-authored' -- src tests
if ($LASTEXITCODE -eq 1) { 'PASS: no runtime-source or test policy text' }
```

### Documentation-only scope gate

```powershell
$allowed = @(
  'docs/DECISIONS.md',
  '.planning/PROJECT.md',
  '.planning/intel/constraints.md'
)
$unexpected = git diff --name-only | Where-Object { $_ -and $_ -notin $allowed }
if ($unexpected) { $unexpected; exit 1 }

dotnet build BlazorCanvas.sln
dotnet test BlazorCanvas.sln --nologo
```

## State of the Art

| Old approach | Current approach | Impact |
|--------------|------------------|--------|
| A project-wide no-hand-authored-JS prohibition described as load-bearing | [VERIFIED: `docs/DECISIONS.md`, `.planning/PROJECT.md`] A permissive policy: JS/interop may be used if it earns its place, while new gesture/keyboard features require their own decision | Future work may evaluate JS/interop on its merits; this phase does not introduce it. |

## Assumptions Log

| # | Claim | Section | Risk if Wrong |
|---|-------|---------|---------------|
| A1 | [ASSUMED] No external CI/service/dashboard/wiki contains an active copy of the retired policy. | Runtime State Inventory | An external description could remain stale even after repository cleanup. |

## Open Questions

1. **Are any policy copies maintained outside this repository?**
   - What we know: [VERIFIED: repository audit] No such artifact is tracked here.
   - What's unclear: [ASSUMED] External service and team-wiki state cannot be inspected from this repository.
   - Recommendation: Treat repository close-out as complete after the scoped audit; separately update any known external project description if one exists.

## Environment Availability

Step 2.6: SKIPPED. [VERIFIED: phase scope] This is a documentation/planning refactor with no new external dependency, service, package, or runtime requirement.

## Validation Architecture

Not included. [VERIFIED: `.planning/config.json`] `workflow.nyquist_validation` is explicitly `false`. The executable plan should nevertheless run the configured full build and test commands because ARCH-01’s acceptance criteria require unchanged runtime behaviour.

## Security Domain

[VERIFIED: `.planning/config.json`] `security_enforcement` is not disabled, so this documentation-only phase retains a security review. No authentication, session, authorization, input parser, cryptography, storage, or network endpoint is changed.

### Applicable ASVS Categories

| ASVS Category | Applies | Standard Control |
|---------------|---------|------------------|
| V2 Authentication | No runtime change | [VERIFIED: phase scope] Keep auth code and configuration out of the diff. |
| V3 Session Management | No runtime change | [VERIFIED: phase scope] Keep cookie/circuit code out of the diff. |
| V4 Access Control | No runtime change | [VERIFIED: phase scope] Keep authorization and routes out of the diff. |
| V5 Input Validation | Documentation input only | [VERIFIED: phase scope] Do not add a parser or external input surface; review literal wording in tracked artifacts. |
| V6 Cryptography | No | [VERIFIED: phase scope] No secrets or cryptographic behavior is changed. |

### Known Threat Patterns

| Pattern | STRIDE | Standard Mitigation |
|---------|--------|---------------------|
| Stale derived constraint silently reimposes a retired capability restriction | Tampering | [VERIFIED: `.planning/intel/constraints.md`] Reconcile current/derived text with the ADR and review every retained policy-family hit. |
| Source or static asset is changed under a documentation-only label | Tampering | [VERIFIED: `08-01-PLAN.md`] Enforce a path allowlist, inspect `git diff`, then build and test. |
| Close-out cannot show what was audited | Repudiation | [VERIFIED: `08-01-PLAN.md`] Record exact search, diff, build, and test commands/results in the phase summary. |

## Sources

### Primary (direct repository evidence)

- [VERIFIED: `docs/DECISIONS.md`] v1.1 milestone note and D-06/D-18/D-33/D-37/D-57 wording.
- [VERIFIED: `.planning/PROJECT.md`] Current constraint summary and decision-table mirrors.
- [VERIFIED: `.planning/intel/constraints.md`] Conflicting active `CONSTRAINT-env` statement despite the v1.1 amendment.
- [VERIFIED: `.planning/ROADMAP.md` and `.planning/REQUIREMENTS.md`] ARCH-01 scope, out-of-scope fences, and build/test success criteria.
- [VERIFIED: tracked source/test audit] No retired-policy phrase in `src` or `tests`; only scaffolded `ReconnectModal.razor.js` is a tracked JS asset.

### Secondary

- [VERIFIED: `.planning/phases/BC-07-selection-lifecycle-restyle/07-01-SUMMARY.md`] Phase 7 completed without a JS/interop change and Phase 8 is its documentation close-out.

### Tertiary

- [ASSUMED] External systems not represented in Git have no active copy of the retired policy.

## Metadata

**Confidence breakdown:**
- Standard stack: LOW - the confidence seam classifies direct codebase evidence as LOW; no external library research is relevant.
- Architecture: LOW - derived from repository structure and declared artifact precedence.
- Pitfalls: LOW - based on the observed internal contradiction and scope analysis.

**Research date:** 2026-07-21  
**Valid until:** The next change to `docs/DECISIONS.md`, `.planning/PROJECT.md`, or `.planning/intel/constraints.md`.
