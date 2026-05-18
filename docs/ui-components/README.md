# UI Component Library

The component library is documented using **Atomic Design** principles. Components are grouped by composition level: atoms compose into molecules, molecules into organisms, organisms into pages.

This documentation is the spec that will eventually feed the production Blazor component library and Storybook.

---

## Folder layout

```
/atoms       Buttons, inputs, icons, badges, labels — single-purpose primitives
/molecules   Forms, cards, search bars — small groups of atoms working together
/organisms   Tables, dashboards, navigation, modals — distinct sections of a UI
/pages       Full-screen layouts assembling organisms
```

---

## Template per component

Each component lives in its own Markdown file (`button.md`, `lead-form.md`, etc.) and contains:

- **Purpose** — what this component is for and when to use it.
- **Variants** — primary/secondary/destructive, sizes (sm/md/lg), etc.
- **States** — default, hover, focus, active, disabled, loading, error, empty.
- **Props / inputs** — the parameters that drive variants and states.
- **Accessibility** — keyboard support, ARIA roles, contrast, screen-reader behaviour.
- **Example usage** — link to journeys that use this component.
- **Open questions**

See `atoms/button.md` for a fully worked example.

---

## Component index

| Type | Component | File | Status |
|---|---|---|---|
| Atom | Button | [`atoms/button.md`](atoms/button.md) | Draft (worked example) |

---

## Conventions

- Naming: `kebab-case.md` matching the component name.
- Reference Microsoft Fluent UI / Fluent 2 patterns where sensible — we're in the Microsoft ecosystem and users will recognise these.
- Note any accessibility requirement that comes from being a regulated/reporting context.
