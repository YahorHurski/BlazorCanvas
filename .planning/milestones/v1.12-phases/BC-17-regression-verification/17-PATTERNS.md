# Phase BC-17: regression-verification - Pattern Map

**Mapped:** 2026-07-23
**Files analyzed:** 4 likely planning/verification artifacts
**Analogs found:** 4 / 4

## File Classification

| New/Modified File | Role | Data Flow | Closest Analog | Match Quality |
|-------------------|------|-----------|----------------|---------------|
| `.planning/phases/BC-17-regression-verification/17-01-PLAN.md` | runbook plan | request-response + human checkpoint | `.planning/milestones/v1.11-phases/BC-12-regression-verification/12-01-PLAN.md` | exact |
| `.planning/phases/BC-17-regression-verification/17-UAT.md` | UAT evidence record | human event-driven checklist | `.planning/milestones/v1.11-phases/BC-12-regression-verification/12-UAT.md` | exact |
| `.planning/phases/BC-17-regression-verification/17-01-SUMMARY.md` | execution summary | batch verification summary | `.planning/milestones/v1.11-phases/BC-12-regression-verification/12-01-SUMMARY.md` | exact |
| `.planning/phases/BC-17-regression-verification/17-VERIFICATION.md` | verifier report | batch verification summary | `.planning/milestones/v1.11-phases/BC-12-regression-verification/12-VERIFICATION.md` | exact |

## Pattern Assignments

### `.planning/phases/BC-17-regression-verification/17-01-PLAN.md` (runbook plan, request-response + human checkpoint)

**Analog:** `.planning/milestones/v1.11-phases/BC-12-regression-verification/12-01-PLAN.md`

**Frontmatter and must-haves pattern** (lines 1-25):
```markdown
---
phase: BC-12-regression-verification
plan: 01
type: execute
wave: 1
depends_on: []
files_modified: []
autonomous: false
requirements: [REG-01]

must_haves:
  truths:
    - "Human acceptance confirms every visible canvas behavior is indistinguishable from v1.1."
    - "A slow drag visibly glides through intermediate positions in a second normal same-profile browser window."
```

**Copy guidance for BC-17:** use the same `type: execute`, `wave: 1`, `files_modified: []`, and `autonomous: false` shape. Replace `REG-01` with `REG-02`, and update truths to the star-specific gate: arm Star, live preview, edge-clamp, refresh persistence, select, drag, delete, and second-window live star glide.

**Objective pattern** (lines 27-35):
```markdown
<objective>
Run the final acceptance-only REG-01 regression gate: prove on the running application that the
v1.11 storage rewrite changed no user-facing behavior.

Purpose: Phase 10 and Phase 11 automated evidence proves persistence, renderer, and protocol
contracts. This plan supplies the separate human visual and two-window acceptance those tests cannot infer.
Output: 12-01-SUMMARY.md recording approval or the exact failed observation and its preserved evidence.
No product code, schema, migration, test framework, browser automation, or feature work is created.
</objective>
```

**Copy guidance for BC-17:** keep acceptance-only wording. Cite Phase 16 automated evidence as prerequisite proof, and state that Phase 17 supplies the human visual/browser verdict those tests cannot infer.

**Preflight/start-host pattern** (lines 59-83):
```markdown
<task type="auto">
  <name>Task 1: Run automated preflight and start one local acceptance host</name>
  <files>none; this is verification-only, with process logs placed under the system temporary directory</files>
  <action>
    Run these PowerShell commands from the repository root in order: docker compose up -d --wait;
    dotnet build BlazorCanvas.sln --nologo -v q; dotnet test BlazorCanvas.sln --nologo; and
    dotnet dev-certs https --check --trust. Stop on a non-zero exit, record the output as failed
    preflight in the summary, and do not begin human verification. Do not install packages, use browser
    automation, start a second server, alter PostgreSQL directly, or modify code, migrations, schemas,
    or tests.
```

**Copy guidance for BC-17:** use the Phase 17 research command preference: `docker compose up -d --wait`, `dotnet build BlazorCanvas.sln --nologo`, then focused star guards:
```powershell
dotnet test tests\BlazorCanvas.Tests\BlazorCanvas.Tests.csproj --no-build --nologo --filter "FullyQualifiedName~CanvasInteractionCoordinatorTests|FullyQualifiedName~FinalPublicCanvasSyncIntegrationTests|FullyQualifiedName~PreviewRenderSmokeTests|FullyQualifiedName~HomePreviewSourceTests"
```
Start exactly one app process with `dotnet run --project src\BlazorCanvas --launch-profile http` unless the planner elects HTTPS and includes certificate preflight.

**Human checkpoint pattern** (lines 86-109):
```markdown
<task type="checkpoint:human-verify" gate="blocking">
  <name>Task 2: Human-verify the full four-shape and two-window regression script</name>
  <files>none; verification only, with the result written to 12-01-SUMMARY.md</files>
  <read_first>
    .planning/phases/BC-12-regression-verification/12-RESEARCH.md
    .planning/ROADMAP.md
    .planning/REQUIREMENTS.md
    .planning/phases/BC-11-renderer-sync-cutover/11-VERIFICATION.md
  </read_first>
  <action>
    Use Task 1's one host. In normal same-profile window A only, open https://localhost:7281/login and
    create a fresh disposable account named bc12-regression-YYYYMMDD-HHMMSS using a non-sensitive
    disposable password.
```

**Copy guidance for BC-17:** keep the blocking human gate. Read `17-RESEARCH.md`, `16-VERIFICATION.md`, requirements, roadmap, and the prior BC-12 precedent. Use a fresh `bc17-regression-YYYYMMDD-HHMMSS` account, two normal same-profile windows, one app process, no private/incognito window, and no production edits.

**Scripted verification pattern** (lines 116-149):
```markdown
<how-to-verify>
  1. Session baseline: Create the disposable account in window A then open the same URL in B. Expected:
     both show the same empty white 1472 by 828 canvas, six tools, no reconnect or error UI, and no
     second login prompt.

  6. Live glide: Leave one visible figure. Drag it slowly in A for two to three seconds through several
     intermediate positions while watching B. Expected: B visibly glides through intermediate locations
     before pointer release and ends at A's final position, without duplicate or jump-only update.
     Capture before, during, and after evidence including a mid-drag frame.
</how-to-verify>
```

**Copy guidance for BC-17:** replace the four-shape script with the star-only REG-02 script: arm Star; draw with live preview; release beyond canvas edge to prove clamp; refresh; select star and verify blue/white star trace; edge-drag and delete; then use two windows for a slow committed star glide.

**Threat model pattern** (lines 177-197):
```markdown
| T-12-01 | Tampering | Acceptance data | medium | mitigate | Fresh disposable account, UI-only setup and cleanup, no SQL, volume reset, or real-account reuse. |
| T-12-02 | Spoofing | Second window/session | medium | mitigate | Require normal same-profile windows, confirm shared session/canvas, and prohibit private windows. |
| T-12-03 | Repudiation | Acceptance outcome | low | accept | Summary records checklist, account name, preflight output, and evidence paths; password is excluded. |
```

**Copy guidance for BC-17:** keep these three threats and update IDs to `T-17-*`. Add one BC-17-specific tampering/process threat: editing code during the acceptance gate invalidates the milestone verdict.

---

### `.planning/phases/BC-17-regression-verification/17-UAT.md` (UAT evidence record, human event-driven checklist)

**Analog:** `.planning/milestones/v1.11-phases/BC-12-regression-verification/12-UAT.md`

**Frontmatter pattern** (lines 1-7):
```markdown
---
status: complete
phase: BC-12-regression-verification
source: [12-VERIFICATION.md]
started: 2026-07-22T15:30:00Z
updated: 2026-07-22T15:45:00Z
---
```

**Copy guidance for BC-17:** preserve `status`, `phase`, `source`, `started`, and `updated`. Use `source: [17-VERIFICATION.md]` if the verifier consumes the UAT file, or `source: [17-01-PLAN.md]` during initial execution.

**Test result pattern** (lines 13-25):
```markdown
## Tests

### 1. Four shapes, persistence/order, edge clamp/slide
expected: All four shapes match v1.1 in both windows and after refresh; edge clamping and vertical edge-slide remain correct.
result: pass

### 2. Selection, deselection, drag, delete
expected: The blue-and-white selection trace, every deselection route, persisted drags, synced deletion, and disabled Delete state match v1.1.
result: pass

### 3. Slow committed-drag glide
expected: During a slow committed drag in window A, window B visibly moves through intermediate positions before pointer release and ends at the same final position.
result: pass
```

**Copy guidance for BC-17:** use three to five star-specific tests. Recommended rows:
```markdown
### 1. Star arm, live preview, edge clamp, and refresh persistence
expected: Star tool arms, window A shows a five-point live preview during draw, edge release clamps inside the 1472 x 828 canvas, and refresh reloads the same star.
result: pass/fail

### 2. Star selection, edge-clamped drag, and delete
expected: Pointer selection shows the blue-and-white star trace; drag clamps/slides at edges; Delete removes the star and the removal persists after refresh.
result: pass/fail

### 3. Second-window live star glide
expected: During a slow committed star drag in window A, same-profile window B visibly glides through intermediate positions before release and ends at the same final position.
result: pass/fail
```

**Summary counter pattern** (lines 27-36):
```markdown
## Summary

total: 3
passed: 3
issues: 0
pending: 0
skipped: 0
blocked: 0

## Gaps
```

**Copy guidance for BC-17:** keep the same machine-readable counter block so `17-VERIFICATION.md` can summarize pass/fail state cleanly.

---

### `.planning/phases/BC-17-regression-verification/17-01-SUMMARY.md` (execution summary, batch verification summary)

**Analog:** `.planning/milestones/v1.11-phases/BC-12-regression-verification/12-01-SUMMARY.md`

**Use this analog through the plan contract:** `12-01-PLAN.md` requires the summary to record approval or the precise failed observation and evidence locations (lines 31-34), process URL/PID/log paths (lines 70-77), and UAT outcomes without passwords (lines 150-154).

**Summary content to copy for BC-17:**
```markdown
# Plan 17-01 Summary

## Outcome

status: passed/failed
requirement: REG-02

## Preflight

- docker compose: pass/fail
- build: pass/fail
- focused star guards: pass/fail
- app process: one process, URL, PID, log paths

## Human UAT

- UAT file: `.planning/phases/BC-17-regression-verification/17-UAT.md`
- disposable username: `bc17-regression-...`
- result: approved/failed
- evidence: screenshots/video/log paths

## Failure Handling

- first failed step:
- expected:
- observed:
- preserved evidence:
- cleanup performed: none on failure / UI-only on pass
```

**Error handling pattern:** copy the BC-12 plan rule: on first failure, stop, preserve figures/account/evidence/logs, do not clean up, inspect code, or attempt a fix in the phase (12-01-PLAN.md lines 103-109).

---

### `.planning/phases/BC-17-regression-verification/17-VERIFICATION.md` (verifier report, batch verification summary)

**Analog:** `.planning/milestones/v1.11-phases/BC-12-regression-verification/12-VERIFICATION.md`

**Report frontmatter pattern** (from BC-12 verification):
```yaml
---
phase: BC-12-regression-verification
verified: 2026-07-22T18:07:25+02:00
status: passed
score: 6/6 must-haves verified
behavior_unverified: 0
overrides_applied: 0
human_verification:
  - test: "Four shapes, persistence/order, and edge clamp/slide"
    expected: "All figures match v1.1 in both windows and after refresh; clamping and edge-slide behavior remain correct."
    result: pass
---
```

**Copy guidance for BC-17:** include `human_verification` entries for star preview/persistence, star select-drag-delete, and second-window glide. Score only passes if every REG-02 behavior has human evidence.

**Observable truths pattern** (from BC-12 verification):
```markdown
## Goal Achievement

### Observable Truths

| # | Truth | Status | Evidence |
| --- | --- | --- | --- |
| 1 | The originating tab visibly updates the in-progress figure while the primary pointer is held. | VERIFIED | Human approved the corrected retest. |
| 5 | A human confirms a committed drag visibly glides in a second same-account window in real time. | VERIFIED | `12-UAT.md` Test 3 passed. |
```

**Copy guidance for BC-17:** truth rows should reference `17-UAT.md`, preflight commands, and Phase 16 verification evidence. Suggested truths:
1. Star toolbar arms and draw preview is visible while pointer is held.
2. Star commits inside canvas edge bounds and persists after refresh.
3. Star selection trace follows the star outline.
4. Star edge-clamped drag and delete behave like existing shapes.
5. Second same-profile window shows live committed star glide.
6. No code/package/schema changes were made during the acceptance gate.

## Shared Patterns

### Runtime Launch And One-Process Constraint

**Source:** `src/BlazorCanvas/Properties/launchSettings.json` lines 4-18
**Apply to:** `17-01-PLAN.md`, `17-01-SUMMARY.md`
```json
"http": {
  "commandName": "Project",
  "dotnetRunMessages": true,
  "launchBrowser": true,
  "applicationUrl": "http://localhost:5054",
...
"https": {
  "applicationUrl": "https://localhost:7281;http://localhost:5054",
```

**Source:** `src/BlazorCanvas/Program.cs` lines 48-53
**Apply to:** all two-window UAT instructions
```csharp
// D-11's cross-tab bridge is this Singleton lifetime. Every Blazor Server tab is its own circuit
// and DI scope, so a Scoped notifier would give each tab a private bucket and sync would silently
// never cross tabs.
builder.Services.AddSingleton<CanvasSyncNotifier>();
```

**Planner rule:** start one app process only. Two local processes do not share the singleton notifier and invalidate the glide check.

### Authentication / Same-Profile Session

**Source:** `src/BlazorCanvas/Program.cs` lines 55-64
**Apply to:** `17-01-PLAN.md`, `17-UAT.md`
```csharp
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/login";
        options.ExpireTimeSpan = TimeSpan.FromDays(365);
    });
```

**Source:** `src/BlazorCanvas/Components/Pages/Login.razor` lines 79-114
**Apply to:** UAT setup and failure triage
```csharp
if (user is null)
{
    user = new User { Username = username, Password = Input.Password };
    db.Users.Add(user);
...
await HttpContext!.SignInAsync(
    CookieAuthenticationDefaults.AuthenticationScheme,
    new ClaimsPrincipal(identity));
Nav.NavigateTo("/");
```

**Planner rule:** use two normal windows from one browser profile and one disposable username. Do not use private/incognito mode for the main gate, and do not record the password.

### PostgreSQL Preflight

**Source:** `docker-compose.yml` lines 1-18
**Apply to:** `17-01-PLAN.md`
```yaml
services:
  db:
    image: postgres:17
    container_name: canvas-postgres
    environment:
      POSTGRES_DB: canvas
      POSTGRES_USER: postgres
      POSTGRES_PASSWORD: postgres
    ports:
      - "5433:5432"
    healthcheck:
      test: ["CMD-SHELL", "pg_isready -U postgres -d canvas"]
```

**Planner rule:** use `docker compose up -d --wait` before build/test/app launch.

### Sync Delivery Boundary

**Source:** `src/BlazorCanvas/Sync/CanvasSyncNotifier.cs` lines 17-24 and 31-41
**Apply to:** second-window glide UAT and failure interpretation
```csharp
public IDisposable Subscribe(int userId, Action<SyncMessage> handler)
{
    var subscriptionId = Guid.NewGuid();
    var bucket = _subscribers.GetOrAdd(userId, _ => new ConcurrentDictionary<Guid, Action<SyncMessage>>());
    bucket[subscriptionId] = handler;

    return new Subscription(() => bucket.TryRemove(subscriptionId, out _));
}

public void Publish(int userId, SyncMessage message)
{
    if (!_subscribers.TryGetValue(userId, out var bucket))
    {
        return;
    }
```

**Planner rule:** if window B does not glide, first verify same user/session and one app process before classifying it as product behavior.

### Phase 16 Star Guard Evidence

**Source:** `.planning/phases/BC-16-interaction-sync-test-guards/16-VERIFICATION.md`
**Apply to:** `17-01-PLAN.md`, `17-VERIFICATION.md`
```markdown
| 1 | User can select a star, the blue-and-white dashed trace uses the star outline, and drag/delete match the four existing shapes. | VERIFIED |
| 2 | A star appears live in another tab on draw, glides during drag, and disappears on delete under D-53. | VERIFIED |
| 18 | A bUnit render-level smoke test emits a star preview polygon with non-empty points before commit. | VERIFIED |
```

**Planner rule:** these automated guards are preflight support, not a substitute for REG-02 human acceptance.

## No Analog Found

| File | Role | Data Flow | Reason |
|------|------|-----------|--------|
| None | - | - | Prior BC-12 regression-verification artifacts provide exact analogs for the Phase 17 planning, UAT, summary, and verification artifacts. |

## Metadata

**Analog search scope:** `.planning/milestones/v1.11-phases/BC-12-regression-verification`, `.planning/phases/BC-16-interaction-sync-test-guards`, `src/BlazorCanvas`, `tests/BlazorCanvas.Tests`, root Docker/app config.
**Files scanned:** 14
**Pattern extraction date:** 2026-07-23
