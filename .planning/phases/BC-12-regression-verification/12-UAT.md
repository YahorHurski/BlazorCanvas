---
status: testing
phase: BC-12-regression-verification
source: [12-VERIFICATION.md]
started: 2026-07-22T15:30:00Z
updated: 2026-07-22T15:30:00Z
---

## Current Test

number: 1
name: Four shapes, persistence/order, edge clamp/slide
expected: |
  In two same-profile windows, all four shapes remain visually identical to v1.1 and persist in the same order after refresh. A near-edge shape stays within the canvas, and a rectangle clamped at the right edge slides vertically without resizing or diverging between windows.
awaiting: user response

## Tests

### 1. Four shapes, persistence/order, edge clamp/slide
expected: All four shapes match v1.1 in both windows and after refresh; edge clamping and vertical edge-slide remain correct.
result: [pending]

### 2. Selection, deselection, drag, delete
expected: The blue-and-white selection trace, every deselection route, persisted drags, synced deletion, and disabled Delete state match v1.1.
result: [pending]

### 3. Slow committed-drag glide
expected: During a slow committed drag in window A, window B visibly moves through intermediate positions before pointer release and ends at the same final position.
result: [pending]

## Summary

total: 3
passed: 0
issues: 0
pending: 3
skipped: 0
blocked: 0

## Gaps

