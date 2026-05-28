# JPMS Design System — dark theme migration

Adapted from the Open Book Figma. The goal of this document is that the look lives in **one place** (tokens + a small set of reusable classes and primitives), so converting the remaining pages is mechanical and future pages inherit the style for free.

## 1. Tokens (single source of truth)

All colours are named semantically in `tailwind.config.js`. Never write a raw hex in a component — change a token here and it propagates everywhere. If a colour is slightly off against the Figma, fix the one value below.

| Token | Hex | Used for |
|---|---|---|
| `canvas` | `#0B0B0C` | Page background (the darkest layer) |
| `surface` | `#161719` | Panels, cards, table containers, headers |
| `surface-raised` | `#1F2024` | Secondary buttons, hover states, raised areas |
| `surface-field` | `#2A2D31` | Input fills |
| `line` | `#232427` | Borders, dividers |
| `line-strong` | `#34373B` | Hover/focus borders |
| `content` | `#FFFFFF` | Primary text, headings, key figures |
| `content-muted` | `#C4C8CE` | Body text |
| `content-subtle` | `#8A9099` | Labels, eyebrows, account ids, inactive nav |
| `content-faint` | `#5A5F68` | Placeholders, hints |
| `accent` / `accent-hover` / `accent-ink` | `#57E08A` / `#4ECF7E` / `#0B0B0C` | Primary action (green); `accent-ink` is the text on it |
| `positive` | `#4ED07D` | Positive amounts / deltas |
| `negative` | `#FF4D4D` | Negative amounts / errors |
| `info` | `#4691F6` | Transfers / neutral status |

Type: **Poppins** (400/500/600/700), loaded in `index.html`, set as `font-sans`.

> These hex values are my best read of the Figma swatches. Please sanity-check `canvas`, `surface`, `surface-field`, `accent`, and `info` against the file — they're the ones most worth confirming.

## 2. Reusable classes (`Styles/app.tailwind.css`)

For the common repeated idioms, use the class instead of re-typing utilities:

| Class | Replaces | 
|---|---|
| `eyebrow` | `text-[11px] uppercase tracking-wider text-slate-500 font-semibold` |
| `panel` | `rounded-xl border border-slate-200 bg-white` |
| `panel-header` | `px-5 py-3 border-b border-slate-200 bg-slate-50` |
| `field` | input border + bg + placeholder |
| `btn-primary` / `btn-secondary` / `btn-ghost` | bespoke button class strings |
| `data-table` | the `<table>` + thead/tbody/td/row classes |
| `modal-overlay` / `modal-panel` | full-screen overlay + dialog surface |

## 3. Reusable primitives (`Components/`)

| Component | Purpose |
|---|---|
| `JewelIcon` | The brand mark (exact Figma path, `currentColor`) |
| `NavIcon` | Route → outline nav icon for the rail |
| `Panel` | Card with optional title + header actions |
| `Stat` | Small label/value tile |
| `MetricStat` | Big figure with positive/negative delta + caption (the Total/Pending pattern) |
| `Pagination` | Range label + prev/next pager for tables |
| `Modal` | Dark dialog with title, close, Cancel/Action footer |

## 4. Mechanical conversion map

When converting a page, find-and-replace these literals. This is the bulk of the work and is safe to apply in passes:

| Old (light) | New (dark token) |
|---|---|
| `bg-white` | `bg-surface` |
| `bg-slate-50` | `bg-surface` / `bg-surface-raised` |
| `border-slate-200` | `border-line` |
| `border-slate-300` (hover) | `border-line-strong` |
| `divide-slate-200` | `divide-line` |
| `text-slate-900` | `text-content` |
| `text-slate-700` | `text-content-muted` |
| `text-slate-600` | `text-content-muted` |
| `text-slate-500` | `text-content-subtle` |
| `text-slate-400` | `text-content-faint` |
| `hover:bg-slate-50` | `hover:bg-surface-raised` |
| `bg-slate-900 text-white` (badges/primary) | `bg-accent text-accent-ink` |

After a pass, this should return nothing in converted files:

```
grep -rnE "slate-|bg-white|text-white" Pages Components Layout
```

## 5. Phased rollout

**Phase 1 — foundation (done).** Tokens, Poppins, jewel icon + favicon, reusable classes and primitives, app shell (the three layouts + the collapsible icon-rail side nav), and reference conversions: `Dashboard`/`RoleHome`, `ProjectsTable`, `Projects`, `WhatsNextPanel`, `RoleSwitcher`, `Login`, `ProviderButton`.

**Phase 2 — shared components (~115 in `Components/`).** Convert the reused building blocks first: every `*Table`, `*Form`, `*Badge`, `*Panel`, `*Card`, the tab navs and `FormField`. Because pages compose these, most pages visually update for free. Route table-heavy components through `data-table` + `Panel` + `Pagination`.

**Phase 3 — page wrappers (68 in `Pages/`).** Mostly the `max-w-* px-* py-*` section headers and loading states. Apply the conversion map; replace header eyebrows with `eyebrow` and figures with `MetricStat` where a page shows totals.

**Phase 4 — field app + portal + dialogs.** The `/site/*` mobile pages (their bodies are still light), external portal pages, and any inline modals → the `Modal` primitive and date-picker styling from the Figma.

**Phase 5 — sweep + QA.** Run the grep lint above across the repo until clean; visually check each role's dashboard, a table page, a form page, and the mobile rail at 380px.

## 6. Keeping future work consistent

New pages should compose `Panel`, `Stat`/`MetricStat`, `data-table`, `Modal`, `btn-*`, `field`, and `eyebrow` rather than hand-rolling utilities. The Phase 5 grep doubles as a CI check — if `slate-`, `bg-white`, or `text-white` appears, the change drifted from the system.
