---
phase: 1
slug: database-schema-geometry-core
status: draft
shadcn_initialized: false
preset: none
created: 2026-07-15
---

# BlazorCanvas — UI Design Contract

> Visual and interaction contract for the whole app UI (login screen, 48px toolbar, drawing
> canvas, figure/selection styling, save-failure modal). Filed under Phase 1 at the user's
> explicit direction — Phase 1 itself builds no UI, but this contract is the single design
> source of truth that Phases 2–5 execute against. Every open visual decision below was made
> by Claude per the user's explicit instruction: *"ignore my 'Ask, dont decide' rule... Decide
> how it will look... UI is up to you."* Values marked **LOCKED** are transcribed verbatim from
> `docs/DECISIONS.md` and must not be changed. Everything else is a committed MVP decision.

---

## Design System

| Property | Value |
|----------|-------|
| Tool | none — shadcn/npm component registries do not apply |
| Preset | not applicable |
| Component library | none — hand-authored Razor components + one plain CSS stylesheet |
| Icon library | none — hand-authored inline SVG icons (24×24 viewBox, `stroke="currentColor"`, no icon font, no JS) |
| Font | system font stack (no web font download) |

**Why no shadcn / component registry:** this app is ASP.NET Core Blazor Server with a
load-bearing **no-JavaScript** constraint (D-06, D-18, D-33, D-37, D-57). There is no npm
toolchain, no React, no build step that a component registry could hook into. All styling is
one hand-written stylesheet (`wwwroot/css/app.css` or equivalent) using the CSS custom
properties (tokens) defined below. The shadcn initialization gate and third-party registry
vetting gate are **not applicable** to this stack; do not attempt to run `shadcn init`.

**Font stack (token `--font-sans`):**
```
-apple-system, BlinkMacSystemFont, "Segoe UI", Roboto, Helvetica, Arial, sans-serif
```
Chosen for zero network requests (matches the offline-friendly, dependency-free MVP ethos) and
because this app has no branding requirement that would justify a custom typeface.

---

## Spacing Scale

Declared values (must be multiples of 4):

| Token | Value | Usage |
|-------|-------|-------|
| xs | 4px | Icon-button internal gap, label-to-input gap |
| sm | 8px | Toolbar edge padding, gap between toolbar buttons, error-text margin |
| md | 16px | Login form field-to-field gap, toolbar cluster-to-logout gap |
| lg | 24px | Login card internal section spacing, modal padding |
| xl | 32px | Login card padding |
| 2xl | 48px | **The toolbar height (LOCKED, D-43/D-56) — this constant never changes** |
| 3xl | 64px | Not used in this MVP (reserved) |

**Exceptions:**
- **Toolbar tool buttons are 40×40px.** Not a multiple of 4×2 grid step but deliberately sized
  to fit inside the locked 48px toolbar with 4px of vertical breathing room above and below
  (`(48 − 40) / 2 = 4`). This is the one icon-only touch-target exception the template allows.
- **Login submit button and text inputs are 44px tall.** Below the WCAG 2.1 minimum
  touch-target recommendation; not on the 8pt grid, chosen deliberately for accessibility on
  the login page where there is no competing height constraint (unlike the toolbar).

---

## Typography

| Role | Size | Weight | Line Height |
|------|------|--------|-------------|
| Display | 28px | 600 (semibold) | 1.2 |
| Heading | 20px | 600 (semibold) | 1.3 |
| Body | 16px | 400 (regular) | 1.5 |
| Label | 14px | 400 (regular) | 1.4 |

Exactly **4 sizes, 2 weights** (400 regular, 600 semibold) — no other weight is used anywhere
in the app.

**Where each role is used:**
- **Display (28/600):** the "BlazorCanvas" title on the login card. Nothing else in this app
  needs a larger heading — there is no marketing page, no dashboard.
- **Heading (20/600):** reserved for the save-failure modal's title bar if one is added in
  Phase 5, and any future page-level heading. Not currently required by any locked decision but
  declared now so Phase 5 does not invent a fifth size.
- **Body (16/400):** login field values, login submit button label, save-failure modal body
  text, form labels' input text.
- **Label (14/400):** login field labels (rendered at 600 — see below), inline error text,
  Logout link text, canvas empty-state placeholder text.

**One weight exception, explicitly declared (not a third weight):** login field labels use the
**Label size (14px) at the Heading/Display weight (600)**, not a new weight — i.e. "14px/600"
is a valid combination of the two declared weights and sizes, not a violation.

---

## Color

| Role | Value | Usage |
|------|-------|-------|
| Dominant (60%) | `#FFFFFF` (white) | Canvas surface (LOCKED white, D-38/D-19), toolbar background, login card background |
| Secondary (30%) | `#E5E7EB` (light grey) | Page background (LOCKED "light grey", D-55) — visible margin around the canvas and behind the login card |
| Accent (10%) | `#2563EB` (blue) | Armed/active toolbar tool button (background), login submit button (background), focus-visible outline ring, save-failure modal "OK" button |
| Destructive | `#DC2626` (red) | Selected figure outline (LOCKED "red", D-31/D-58 — this hex is the concrete value for that locked token), enabled Delete button icon + hover tint, login inline error text |

**Supporting neutrals (not part of the 60/30/10 split, needed for structure):**

| Token | Value | Usage |
|-------|-------|-------|
| `--color-border` | `#D1D5DB` | Toolbar bottom border, input borders, card border |
| `--color-text` | `#1F2328` | Body/heading text color (UI chrome only — see figure-color note below) |
| `--color-text-muted` | `#6B7280` | Field labels' secondary tone if needed, empty-canvas placeholder text (`#9CA3AF`, see Figure Styling) |
| `--color-disabled-icon` | `#9CA3AF` | Disabled Delete button icon |
| `--color-hover-tint` | `#F3F4F6` | Toolbar button hover background |
| `--color-destructive-tint` | `#FEE2E2` | Delete button hover background (only when enabled) |

**Accent (`#2563EB`) reserved for — exactly these elements, nothing else:**
1. The currently-armed toolbar tool button's background (pointer/line/rectangle/circle/triangle
   — never more than one at a time).
2. The login page's "Log in" submit button.
3. The save-failure modal's "OK" button.
4. The `:focus-visible` keyboard outline ring on any interactive element (buttons, inputs).

**Destructive (`#DC2626`) reserved for — exactly these elements, nothing else:**
1. A **selected figure's outline** — this is the concrete hex for the locked "red 2px outline"
   requirement (D-31/D-58).
2. The **Delete toolbar button's icon**, only in its enabled state (something is selected).
3. **Login inline error text** ("Incorrect password" / "Username and password are required").

**Important note on figure colors vs. UI chrome colors:** the figure default outline is
**pure black `#000000`** (D-38/D-58 says literally "black, 2px") — this is a separate token
(`--color-figure-stroke: #000000`) from the UI chrome's near-black text color
(`--color-text: #1F2328`). Do not conflate them: SVG figure strokes use `#000000` exactly;
Razor page text uses `#1F2328` for slightly softer readability. Figure fill is
**pure white `#FFFFFF`** (`--color-figure-fill`), identical to the canvas token.

---

## Layout Constants (LOCKED — transcribe, do not invent)

| Constant | Value | Source |
|----------|-------|--------|
| Toolbar height | **48px**, full page width, `position: static` at document top, page margin 0 | D-43, D-56 |
| Canvas position | document `(0, 48)`, anchored top-left, **no `margin: auto`** | D-18, D-43 |
| Canvas size | **1280 × 720**, 1:1, does not rescale with window | D-19, D-18 |
| Canvas border | **none** — zero CSS border on the `<svg>` element | D-19, D-38, D-43 |
| Coordinate mapping | `canvasX = PageX`, `canvasY = PageY − 48`, using `PageX`/`PageY` only, never `OffsetX`/`OffsetY` | D-18, D-43 |
| Toolbar buttons | exactly six: `[pointer] [line] [rectangle] [circle] [triangle] [delete]`, left-aligned, plus a right-aligned Logout form, separated visually from the six | D-30, D-33, D-56 |
| Pointer tool | armed on page load, visibly active | D-31 |
| Delete button | greyed out / unclickable until a figure is selected | D-58 |

---

## Screens

### 1. Login (`/login`, static SSR, D-34)

**Layout:** full-viewport flex container, `align-items: center; justify-content: center;`,
background = page-background token (`#E5E7EB`). A single centered card:

- Card: `max-width: 360px`, `width: 100%` (with 16px side gutter below 392px viewport),
  background `#FFFFFF`, `border-radius: 8px`, `padding: 32px` (xl), `border: 1px solid
  var(--color-border)`, `box-shadow: 0 1px 3px rgba(0,0,0,0.1)`.
- Title "BlazorCanvas" — Display role (28px/600/1.2), `margin-bottom: 24px` (lg), color
  `var(--color-text)`.
- Form, `POST /login`:
  - Username field: label "Username" (Label size, 600 weight) + text input, `margin-bottom:
    16px` (md).
  - Password field: label "Password" (Label size, 600 weight) + `type="password"` input,
    `margin-bottom: 16px` (md, or 8px/sm to the error line if an error is present).
  - Both inputs: `height: 44px`, `width: 100%`, `padding: 0 12px`, `border: 1px solid
    var(--color-border)`, `border-radius: 6px`, Body size (16px/400). `:focus-visible` →
    `border-color: var(--color-accent)` + `box-shadow: 0 0 0 3px rgba(37,99,235,0.2)`.
  - Both fields use native HTML `required` (no JavaScript needed — this is a browser-native
    attribute, not a script) so an empty submit is caught client-side; the server re-validates
    and returns the same inline error copy if bypassed.
  - Inline error line (only rendered when present): Label size (14px/400), color
    `var(--color-destructive)` (`#DC2626`), `margin-top: 8px` (sm), positioned directly below
    the password field, above the submit button.
  - Submit button: full width, `height: 44px`, background `var(--color-accent)`, text color
    white, Body size at 600 weight, `border-radius: 6px`, `margin-top: 24px` (lg). Hover:
    background `#1D4ED8`. `:focus-visible`: 2px accent outline, 2px offset.
- **No Register link, no "forgot password" link, no third-party login buttons, no password
  strength meter, no confirm-password field** — all explicitly out of scope (D-08, D-17).

### 2. The Canvas Shell (`/`, InteractiveServer, `[Authorize]`, D-51)

**Toolbar strip** (48px, spans full page width, `background: #FFFFFF`, `border-bottom: 1px
solid var(--color-border)`, `display: flex; align-items: center;`):

- Left cluster (6 tool buttons): `padding-left: 8px` (sm), `gap: 4px` (xs) between buttons.
  Each button 40×40px, `border-radius: 6px`, icon centered, `background: transparent`.
- A visual gap (`margin-left: auto` on the logout form pushes it to the far right — see D-56's
  "visually separated" requirement) so the six tool buttons stay a tight cluster on the left
  and Logout sits alone on the right, never adjacent to the Delete button without a gap of at
  least 16px (md).
- Right side: a `<form method="post" action="/logout">` containing a single submit button
  styled as a text link — Label size (14px/400), color `var(--color-text-muted)`
  (`#6B7280`), `padding: 8px 16px` (sm/md), no background by default; hover: color
  `var(--color-text)` + `background: var(--color-hover-tint)`, `border-radius: 6px`.
  Label: **"Log out"**.

**Canvas surface:** `<svg width="1280" height="720">` positioned at document `(0, 48)` via a
wrapping `<div>` with no margin/padding, `background: #FFFFFF` (also settable directly as the
SVG's own background — either is fine, no border either way). The **light grey page
background** (`#E5E7EB`) is applied to `<body>`, so it shows in the margin around the fixed
1280×720 canvas on any viewport wider/taller than the canvas itself.

**Empty-canvas placeholder** (Claude's discretion — not required by any locked decision, added
for MVP polish, zero interaction risk): when a user's figure list is empty, render a single
non-interactive `<text>` element centered at canvas coordinates `(640, 360)`,
`text-anchor="middle"`, `pointer-events="none"` (so it can never intercept a draw click), Label
size (14px/400), fill `#9CA3AF`. Copy: **"Draw something — pick a tool above, then drag on the
canvas."** It disappears automatically the moment the figure list becomes non-empty (simply
stop rendering it — no animation, no dismiss button).

### 3. Save-Failure Modal (D-52, Phase 5)

Rendered only on final save failure after retries. Centered overlay:

- Backdrop: `position: fixed; inset: 0; background: rgba(0,0,0,0.4);`.
- Modal box: `max-width: 400px`, `width: calc(100% - 32px)`, background `#FFFFFF`,
  `border-radius: 8px`, `padding: 24px` (lg), `box-shadow: 0 4px 12px rgba(0,0,0,0.15)`.
- Body text: Body size (16px/400), color `var(--color-text)`. Copy is **LOCKED verbatim by
  D-52** — do not reword: *"The change could not be saved. The canvas will be reloaded from
  the database."*
- Single button, right-aligned, `margin-top: 24px` (lg): label **"OK"**, same visual treatment
  as the login submit button (accent background, white text, Body/600, `height: 44px`,
  `padding: 0 24px`, `border-radius: 6px`). Clicking it reloads the canvas from PostgreSQL per
  D-52 step 4.
- No dismiss-by-backdrop-click, no close (×) icon — this modal is informational and mandatory;
  the only way out is acknowledging it, matching D-52's forced (not defensive) intent.

---

## Component Specifications

### Toolbar tool buttons (pointer / line / rectangle / circle / triangle)

Icon-only, 24×24 inline SVG icon centered in a 40×40 hit area, `border-radius: 6px`.

| State | Background | Icon color | Notes |
|-------|-----------|------------|-------|
| Default (unarmed) | transparent | `#1F2328` | |
| Hover | `#F3F4F6` | `#1F2328` | |
| Armed (active) | `#2563EB` (accent) | `#FFFFFF` | Exactly one armed at a time; pointer armed on load (D-31) |
| `:focus-visible` | (state above) + 2px accent outline, 2px offset | | Keyboard accessibility, pure CSS `:focus-visible` — no JS |

**Icons (hand-authored inline SVG, `stroke="currentColor"`, `stroke-width="1.75"`, no fill,
24×24 viewBox):** pointer → cursor-arrow glyph; line → single diagonal stroke; rectangle →
outlined square; circle → outlined circle; triangle → outlined upward triangle (apex
top-centre, matching D-21's actual draw geometry). Each button carries an `aria-label`
matching its tool name — no visible text label (keeps the toolbar compact within 48px), but
screen-reader accessible.

### Delete button

Same 40×40 hit area and `border-radius: 6px` as the tool buttons, but visually distinct
because it is an **action**, not a mode toggle — it is never "armed", only enabled/disabled.

| State | Background | Icon color | Interaction |
|-------|-----------|------------|-------------|
| Disabled (nothing selected) | transparent | `#9CA3AF` | `disabled` attribute / `pointer-events: none`, `cursor: not-allowed` |
| Enabled (figure selected) | transparent | `#DC2626` (destructive) | clickable |
| Enabled + hover | `#FEE2E2` (destructive tint) | `#DC2626` | |
| Enabled + `:focus-visible` | (enabled state) + 2px accent outline, 2px offset | | |

Icon: a simple trash-can glyph, same stroke conventions as the tool icons. **No confirmation
dialog on delete** (see Copywriting Contract → Destructive confirmation, below) — clicking it
while enabled deletes immediately.

### Text inputs (login only)

`height: 44px`, `border: 1px solid #D1D5DB`, `border-radius: 6px`, `padding: 0 12px`, Body
size. `:focus-visible` → border `#2563EB` + `box-shadow: 0 0 0 3px rgba(37,99,235,0.2)`. No
other input exists anywhere in the app (no search, no text entry on the canvas).

---

## Figure & Selection Styling (LOCKED values — the app's actual "content" colors)

| Element | Value | Source |
|---------|-------|--------|
| Default figure outline | `#000000`, 2px stroke | D-38, D-58 |
| Default figure fill | `#FFFFFF` (load-bearing — SVG does not register clicks on an unfilled shape) | D-38 |
| **Selected** figure outline | `#DC2626`, 2px stroke | D-31, D-58 (this UI-SPEC supplies the concrete hex for the locked "red") |
| Selected figure fill | `#FFFFFF` (unchanged — only the stroke changes on selection) | D-31 |
| Live draw preview | same default style (`#000000` stroke / `#FFFFFF` fill) as the shape will have once committed — no separate "preview" visual treatment (e.g. no dashed outline, no reduced opacity), so the preview never lies about what will be created | D-35 |

**No hover state on figures.** No per-figure color, no style variation of any kind (D-14) — one
fixed style for unselected, one fixed style for selected, nothing else.

---

## Copywriting Contract

| Element | Copy |
|---------|------|
| Primary CTA | **"Log in"** — the single login-page submit button (functions as both sign-in and implicit sign-up per D-17; the user never sees a separate "register" verb) |
| Empty state heading | *(none — single-line placeholder only, no separate heading)* |
| Empty state body | **"Draw something — pick a tool above, then drag on the canvas."** Rendered as non-interactive SVG text centered on an empty canvas; disappears once any figure exists. |
| Error state (login) | **"Incorrect password."** (wrong password) / **"Username and password are required."** (empty field, defense-in-depth behind native `required`) — Label size, destructive red, inline below the password field |
| Error state (save failure) | **"The change could not be saved. The canvas will be reloaded from the database."** — LOCKED verbatim, D-52. Modal button: **"OK"** |
| Destructive confirmation | **Delete figure: no confirmation dialog.** Deletion is immediate on click, matching the app's established no-undo, no-Save-button, direct-manipulation philosophy (D-04, D-09) — a confirm dialog would be the app's only modal-for-safety pattern in a design that otherwise trusts every other action (draw, drag) to commit instantly and irreversibly. **Logout: no confirmation** either — same reasoning, and logging out destroys nothing (D-25). |

---

## Registry Safety

| Registry | Blocks Used | Safety Gate |
|----------|-------------|-------------|
| shadcn official | none | not applicable — no React/npm toolchain in this project |
| third-party | none | not applicable |

No component registry of any kind is used. All markup and styles are hand-authored Razor
components and one static CSS file, consistent with the project's load-bearing
no-JavaScript / no-external-dependency constraint.

---

## Checker Sign-Off

- [ ] Dimension 1 Copywriting: PASS
- [ ] Dimension 2 Visuals: PASS
- [ ] Dimension 3 Color: PASS
- [ ] Dimension 4 Typography: PASS
- [ ] Dimension 5 Spacing: PASS
- [ ] Dimension 6 Registry Safety: PASS

**Approval:** pending
