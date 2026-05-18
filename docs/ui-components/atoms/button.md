# Button (Atom)

**Status:** Draft — worked example

## Purpose

A single tappable / clickable action. The most-used atom in the platform — every form, dialog, and toolbar uses buttons.

## Variants

| Variant | Use |
|---|---|
| `primary` | The main action of a screen or dialog (e.g. _Save_, _Submit_, _Create Lead_). One per context. |
| `secondary` | Alternative actions (e.g. _Cancel_, _Back_). |
| `tertiary` (text) | Low-emphasis actions inline with text (e.g. _Edit_ next to a field). |
| `destructive` | Irreversible or risky actions (e.g. _Delete project_). Requires confirmation dialog. |
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
| Disabled | Action not currently available. Tooltip explains why where possible. |
| Loading | Async action in flight. Shows spinner, button is non-interactive, retains width to avoid layout shift. |

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
- Minimum 44×44 px touch target on mobile (use padding around smaller visual sizes).
- Icon-only buttons must have `aria-label`.
- Loading state should announce via `aria-busy="true"` and not change the accessible name.
- Disabled state uses the `disabled` attribute (not just visual styling) so assistive tech announces it.

## Example usage in journeys

- _Save Lead_ button on step 1 of [`01-onboarding-sales-lead.md`](../../user-journeys/01-onboarding-sales-lead.md) → `primary`, `md`.
- _Cancel_ on the same form → `secondary`, `md`.
- _Delete project_ on the project detail page → `destructive`, `md`, opens confirmation dialog.

## Open questions

- [ ] Confirm primary brand colour for the primary variant.
- [ ] Do we need a `link` variant that looks like an anchor but behaves like a button?
- [ ] What's the policy for buttons in tight table rows — `sm` or icon-only?
