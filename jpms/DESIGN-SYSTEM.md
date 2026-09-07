# JPMS Design System

The look of the portal is the **Open Book** Figma (file `g7fm7dfuSYX81z1sdKAoyD`). This document is
the short form; `docs/ui/open-book-design-rules.md` is the long form — every value below was read
out of that file's local styles and Properties panels on 2026-09-03, not eyeballed from a
screenshot. The goal is that the look lives in **one place** (tokens + a small set of reusable
classes and primitives), so a page built from them matches the design by construction.

**Standing decision (James, 2026-09-03): where this document, the Figma and an earlier jpms rule
disagree, the Figma wins.** The earlier rules were stop-gaps for lower-level inconsistencies.

## 1. Tokens (single source of truth)

All colours are named semantically in `tailwind.config.js`. Never write a raw hex in a component —
change a token there and it propagates everywhere. Alpha Figma styles are flattened over the
surface they sit on in the file.

| Token | Hex | Figma style | Used for |
|---|---|---|---|
| `canvas` | `#101111` | BG/Dark | Page background; **table header rows**; the modal |
| `surface` | `#19191C` | Panels/Dark @90% | Chrome (nav, top bar), cards, panels, table bodies |
| `surface-raised` | `#282A31` | Panels/Table Highlight @70% | Hover / selected rows |
| `surface-field` | `#282A31` | Panels/Table Highlight @70% | Input fills (same style as raised — on purpose) |
| `line` | `#2E323A` | Boarders/Outline | Structural borders: nav, top bar, cards, **small** buttons |
| `line-strong` | `#3C414A` | Boarders/Table Seperator @80% | Anything row- or field-shaped: table cells, inputs, radios, checkboxes, the modal, **large** buttons |
| `content` | `#FFFFFF` | Text/White | Titles, values, button labels, header cells |
| `content-muted` | `#DDDDDD` | Text/G4 | Table body text |
| `content-subtle` | `#D1D1D1` | Text/G5 | Labels and captions (a label is content, not a hint) |
| `content-faint` | `#8C8C8C` | Text/G6 | Inactive nav, placeholders, muted / disabled actions |
| `accent` / `accent-hover` / `accent-ink` | `#66E094` / `#5ACF85` / `#101111` | Status/Positive | The ONE solid-green primary control per view; `accent-ink` is the text on it. `accent-hover` is a jpms assumption (Figma hover state not yet read) |
| `positive` | `#66E094` | Status/Positive | Positive figures and deltas — the same green as `accent` |
| `negative` | `#FF403C` | Status/Negative | Negative figures, errors. Never a button fill |
| `info` | `#3CA1FF` | Status/Neutral | Neutral status **and links inside tables**. Never an action |
| `brand` | `#4CDBEE` | Brand/Main | The logo / brand mark only |

`negative-strong` (`#8E1616`) and `negative-ink` (`#FFF1F1`) are jpms's own: the error toast's fill
and its text.

**Type: Poppins** (400/500/600/700), loaded in `index.html`, set as `font-sans`. The named sizes
carry the Figma's fixed leadings (`tailwind.config.js` `fontSize`): `text-xs` 12/14 · `text-sm` 14/16
· `text-base` 16/20 · `text-lg` 18/22 · `text-xl` 20/20 · `text-2xl` 24/24 · `text-4xl` 40/40 ·
`text-5xl` 48/48. Letter-spacing is 0 everywhere.

## 2. Reusable classes (`Styles/app.tailwind.css`)

For the common repeated idioms, use the class instead of re-typing utilities:

| Class | What it is |
|---|---|
| `eyebrow` | A label: 14/Med sentence case in `content-subtle`. (The name is historical — there is no uppercase, no tracking, nothing under 12px in the design.) |
| `panel` / `panel-header` | A square card: `border-line bg-surface`; header `px-6 py-4 border-b`. Body padding is 24 (`p-6`). |
| `field` | The Figma input: `h-11 rounded border-line-strong bg-surface-field px-4 py-3 text-base`. `textarea.field` frees the height. |
| `btn-primary` | Small solid-green button — the view's one primary act |
| `btn-secondary` | The Figma "Minimal" button — every other action: 1px `line` border, no fill, white label. `btn-neutral` is an alias for one round. |
| `btn-lg` | Add to either for the Large size (52px / 16px, `line-strong` border) — dialog and form submits |
| `btn-ghost` | Quiet text-only dismissals |
| `btn-icon` | A Toolbar's square icon button (32px, `line` border, no fill, disabled styling built in) |
| `data-table` | The Figma table: header on `canvas` in white SemiBold, body on `surface` in `content-muted`, `line-strong` cell borders, 40/48px rows, 24/16 cell padding |
| `modal-overlay` / `modal-panel` | Full-screen overlay + the dialog: 600 wide, on `canvas`, `rounded-lg`, `line-strong` border, the design's one shadow |

**Buttons come in two sizes and two looks, and that is all.** Small (`btn-*`, 32px, 14/Med) for
every in-view action; Large (`btn-lg`, 52px, 16/Med) for dialog and form submits — a `Modal`'s
footer upsizes its buttons itself (`[data-modal-footer]`). **Green means "do it", once**: one
`btn-primary` per view or per dialog footer; everything else is `btn-secondary` — grey-outlined,
white text, no green. Action vs dismiss is carried by position and label (green on the right,
dismiss to its left), exactly as in the design. Destructive acts add `text-negative` to a
`btn-secondary`/`btn-ghost`. The 2026-08-14 "every action button is green" rule is retired.

## 3. Usage rules (the doctrine that ships with each component)

1. **Three fills, in order of depth**: page = `canvas`; chrome, cards and table bodies = `surface`;
   fields and highlighted rows = `surface-field` / `surface-raised`. A table **header** drops back to
   `canvas`. Nothing is lighter than `surface-raised` except a green button.
2. **Two borders**: `line` around chrome and cards; `line-strong` around anything row- or
   field-shaped (tables, inputs, radios, checkboxes, modal, large buttons).
3. **Four text tones, monotonic**: `content` for titles, values, button labels, header cells;
   `content-muted` for table body text; `content-subtle` for labels and captions; `content-faint`
   for inactive nav, placeholders and muted actions. Never a raw grey.
4. **Green means "do it", once.** Red is for figures and errors, never a button fill. Blue (`info`)
   is for links in data and neutral status, never for actions.
5. **Type: size by role, hierarchy by weight**: 20/Med page title · 18/Semi section title · 24/Semi
   modal title · 24/Med headline figure · 20/Med card figure · 16/Med nav & inputs · 14/Med labels &
   buttons · 14/Reg body · 12/Med captions. No 11px, no uppercase, no tracking.
6. **Corners: 4px on controls (`rounded`), 8px on the modal (`rounded-lg`), 0 on everything else.**
   No `rounded-xl`, no `rounded-md`, no pills.
7. **No shadows except the modal.** No gradients; hierarchy is fill and border.
8. **Spacing on the 4px grid**: cell padding 16/24, button padding 8/16 (small) 16/32 (large), card
   padding 24, modal padding 32, nav padding 24/32, content gutter 32.

## 4. Reusable primitives (`Components/`)

The rule for each lives in `CLAUDE.md` → *Shared components*. Build order and the evidence
behind each: `docs/ui/stage-1-components.md`.

| Component | Purpose |
|---|---|
| `Page` | Every routed view: the approved-session gate + the one content gutter (`CanAccess`, `MaxWidth`, `Bare`) |
| `PageHeader` | The page's one header — subtitle/count, `Primary` (the one green button), `Actions`; `Title`/`Eyebrow` only on a record page (the top bar names registers) |
| `SectionHeader` / `Panel` | Un-boxed section title + actions / the boxed card with `Title`, `HeaderActions`, `IsLoading` |
| `Notice` | The inline message box, by `Tone`; `Dismissible` |
| `Pill` | Every status badge, by `Tone` (`StatusTones.cs` maps each enum); `OnClick` makes it a menu trigger |
| `FormField` / `Checkbox` | Every labelled control (label, `field`, `Hint`, `Error`, `Required`) / the labelled tick |
| `RecordsTable` | A list of records: panel + scroll box + `IsLoading` + `IsEmpty`, the `<thead>`/`<tbody>` inside; `SortableColumnHeader` for sortable columns |
| `TabRow` / `FilterChips` | Route/pane tabs (underline) / local filters (pills) — `TabItem` rows |
| `SearchInput` | The debounced search box |
| `StatTile` / `MetricStat` | The labelled figure (Figma stat card) / the un-boxed headline figure with delta |
| `EmptyState` | "Nothing here yet", with room for the one action |
| `ConfirmDialog` / `InlineConfirm` | The confirm modal (`Danger`) / the two-click armed button |
| `Modal` | The Figma dialog: 24/Semi title, hairline under the header, Large Cancel/Action footer |
| `Toolbar` / `ToolbarButton` / `ToolbarDivider` / `ExportToExcelButton` | THE in-view menu of icon buttons with hover text |
| `RecordTabBar` / `WorkspaceSectionNav` / `ProjectPageShell` | The request chain tabs / a section's tab row / the project page frame (breadcrumb + project `PageHeader` + tab nav) |
| `JewelIcon` / `NavIcon` / `ActionIcon` | The brand mark / rail icons / action glyphs |
| `LoadGate` | THE loading mark: `IsLoading` covers a region with nothing to show yet, `Overlay="true"` veils content being refreshed. A gate silences every gate nested inside it, so a screen shows one jewel (`CLAUDE.md` → *Loading states*). The whole-page mark is the boot screen in `index.html`; `JewelSpinner` is the gate's own part and is never written in a view |
| `DropdownMenu` | The row/record actions menu |
| `DateText` / `DateTimeText` / `Money` / `WholeMoney` | Not components — the global helpers every date and figure renders through |

## 5. Keeping future work consistent

New views compose the components in §4 rather than hand-rolling utilities. The lint grep
doubles as a CI check — any hit in `Pages`, `Components`, `Features` or `Layout` means the change
drifted from the system (`PurchaseOrderSheet` — a printed sheet with its own CSS — the
light email-body surfaces `bg-white text-gray-900`, and `LoadGate.razor`, the one file allowed to
render a `JewelSpinner`, are the known exceptions):

```
grep -rnE "slate-|amber-|emerald-|rose-|red-[0-9]|text-\[1?[0-9]px\]|rounded-(xl|2xl|lg|md)|uppercase|tracking-|shadow-(sm|md|lg|xl|2xl)|bg-negative/10 border|btn-primary text-xs|<section class=\"px-|RequestAccessView Email|<label class=\"block eyebrow|<h1 |<thead class=|<tbody class=\"divide|ToString\(\"d{1,2} MMM yyyy|confirming[A-Z]|Armed\b|(Chip|Tab)Class\(|<JewelSpinner" Pages Components Features Layout
```

(`rounded-lg` is allowed on the modal panel and `DropdownMenu`; `<h1` on the auth pages outside
the shell.)

Still to read from the Figma (`docs/ui/open-book-design-rules.md` §8): row hover/select fill,
the row-action dropdown, tabs vs pills, toggle switch, date picker, login page, button
hover/pressed/disabled variants, status pills, and the mobile frames for `/site/*`.
