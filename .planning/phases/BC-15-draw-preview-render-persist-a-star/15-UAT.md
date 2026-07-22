---
status: testing
phase: BC-15-draw-preview-render-persist-a-star
source:
  - 15-VERIFICATION.md
started: 2026-07-22T23:44:00+02:00
updated: 2026-07-22T23:44:00+02:00
---

## Current Test

number: 1
name: Browser Star Draw UAT
expected: |
  The toolbar arms Star, the preview follows the cursor as a five-point star,
  the shape clamps at the canvas edge, commit creates the same five-point star,
  and refresh reloads it unchanged without pressing a Save button.
awaiting: user response

## Tests

### 1. Browser Star Draw UAT

expected: The toolbar arms Star, the preview follows the cursor as a five-point star, the shape clamps at the canvas edge, commit creates the same five-point star, and refresh reloads it unchanged without pressing a Save button.
result: pending

Instructions:

1. Run the app.
2. Log in and open the canvas.
3. Arm the Star toolbar button.
4. Drag a star near and beyond a canvas edge.
5. Release the pointer.
6. Refresh the page.
7. Confirm the same star remains visible and unchanged, with no Save button involved.

## Summary

total: 1
passed: 0
issues: 0
pending: 1
skipped: 0
blocked: 0

## Gaps
