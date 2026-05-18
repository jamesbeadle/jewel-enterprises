> ⚠️ **REFERENCE EXAMPLE — not project content.**
> Generic illustration of what a filled-in component spec looks like. Properties, variants and accessibility notes here are placeholder material, **not** confirmed design decisions for Jewel Enterprises. Use only as scaffolding when documenting real components.

---

# Button (Atom) _(example)_

## Purpose

A single tappable / clickable action. The most-used atom in most platforms — every form, dialog, and toolbar uses buttons.

## Variants

| Variant | Use |
|---|---|
| `primary` | The main action of a screen or dialog (e.g. _Save_, _Submit_). One per context. |
| `secondary` | Alternative actions (e.g. _Cancel_, _Back_). |
| `tertiary` (text) | Low-emphasis actions inline with text. |
| `destructive` | Irreversible or risky actions. Requires confirmation dialog. |
| `icon` | Icon-only button. **MUST** have an `aria-label`. |

## Sizes

`sm` (24px) · `md` (32px, default) · `lg` (40px)

## States

| State | Description |
|---|---|
| Default | Resting state. |
| Hover | Cursor over the button. |
| Focus | Keyboard focus — visible focus ring (3:1 contrast minimum). |
| Active | Pressed. |
| Disabled | Action not currently available. |
| Loading | Async action in flight. Shows spinner; retains width to avoid layout shift. |

## Props

| Prop | Type | Default | Notes |
|---|---|---|---|
| `Variant` | enum | `primary` | See variants table. |
| `Size` | enum | `md` | |
| `Icon` | string \| null | null | Fluent icon name. |
| `IconPosition` | `start` \| `end` | `start` | |
| `Disabled` | bool | false | |
| `Loading` | bool | false | |
| `Type` | `button` \| `submit` | `button` | |
| `OnClick` | EventCallback | — | |
| `AriaLabel` | string | — | **Required** for icon-only variant. |

## Accessibility

- Native `<button>` element — never a `<div>` with an onclick.
- Visible focus ring at all times (no `outline: none` without an equivalent).
- Minimum 44×44 px touch target on mobile.
- Icon-only buttons must have `aria-label`.
- Loading state should announce via `aria-busy="true"`.
- Disabled state uses the `disabled` attribute (not just visual styling).

## Example usage in journeys

- _Linked from real journeys once they're written._

## Open questions

- _A real spec would capture open questions from the design review session._
