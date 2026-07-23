---
status: complete
phase: BC-17-regression-verification
source: [17-01-PLAN.md]
started: 2026-07-23T00:23:22Z
updated: 2026-07-23T00:31:00Z
---

## Current Test

Human REG-02 acceptance approved against one local `dotnet run` host.

## Preflight

status: passed
docker_compose_up: pass - `canvas-postgres` healthy on host port 5433
docker_compose_ps: pass - `canvas-postgres` running healthy
dotnet_build: pass - `dotnet build BlazorCanvas.sln --nologo`
focused_star_smoke: pass - 40 passed, 0 failed, 0 skipped
app_url: http://localhost:5054
app_pid: 34208
listener_pid: 16840
log_root: C:\Users\EGOR\AppData\Local\Temp\BlazorCanvas-BC17-6ad5a2f6-28e3-4767-abdd-1f70626015a5
stdout_log: C:\Users\EGOR\AppData\Local\Temp\BlazorCanvas-BC17-6ad5a2f6-28e3-4767-abdd-1f70626015a5\app.stdout.log
stderr_log: C:\Users\EGOR\AppData\Local\Temp\BlazorCanvas-BC17-6ad5a2f6-28e3-4767-abdd-1f70626015a5\app.stderr.log
host_ready: pass - `http://localhost:5054/login` returned HTTP 200
notes: Build/test emitted existing NU1902 warning for transitive AngleSharp 1.4.0; no package changes made.

## Disposable Account

username: not supplied in checkpoint response
password_recorded: no
guidance: Use a fresh disposable account named `bc17-regression-YYYYMMDD-HHMMSS` with a non-sensitive disposable password. Do not record the password in this file.
approval_source: User checkpoint response `approved` on 2026-07-23T00:31:00Z; every REG-02 check reported passed.

## Tests

### 1. Star draw, live preview, edge clamp, and refresh persistence

expected: Arm Star in window A, drag a clear diagonal box, see a live five-point preview, drag beyond a canvas edge, release inside the clamped canvas, refresh, and confirm the same star reappears unchanged without a Save button.
result: pass
evidence: human checkpoint approval; external screenshot/video path not supplied in checkpoint response
notes: Human reported Star arming, live five-point preview, canvas-edge clamping, and refresh persistence all passed.

### 2. Star selection, edge-clamped drag, and delete

expected: Arm Pointer, select the star, see exactly one blue-and-white dashed trace on the star outline, drag beyond a canvas edge with slide-along-edge behavior and no resize, refresh with selection/position preserved as expected, delete through the toolbar, refresh, and confirm the star remains absent with Delete disabled when nothing is selected.
result: pass
evidence: human checkpoint approval; external screenshot/video path not supplied in checkpoint response
notes: Human reported star-outline blue-and-white selection trace, edge-clamped drag, refresh state, delete, and disabled Delete behavior all passed.

### 3. Second-window live star glide

expected: With one committed star visible in two normal same-profile windows for the same disposable account, slowly drag the star in window A for two to three seconds and confirm window B shows intermediate glide positions before pointer release, with no duplicate, jump-only update, or disappearing/reappearing artifact.
result: pass
evidence: human checkpoint approval; external recording/frame path not supplied in checkpoint response
notes: Human reported same-profile second-window live star glide passed with no duplicate, jump-only update, or disappearing/reappearing artifact.

## Failure Record

first_failed_step: none
expected_behavior: none - no failed step
observed_behavior: all scripted checks reported passed by human checkpoint response
evidence: not applicable
browser_console: not supplied; no failure reported
refresh_effect: refresh checks reported passed
app_logs: C:\Users\EGOR\AppData\Local\Temp\BlazorCanvas-BC17-6ad5a2f6-28e3-4767-abdd-1f70626015a5\app.stdout.log; C:\Users\EGOR\AppData\Local\Temp\BlazorCanvas-BC17-6ad5a2f6-28e3-4767-abdd-1f70626015a5\app.stderr.log

## Summary

total: 3
passed: 3
issues: 0
pending: 0
skipped: 0
blocked: 0
