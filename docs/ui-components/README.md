# UI Component Library

The component library is documented using **Atomic Design** principles. Components are grouped by composition level: atoms compose into molecules, molecules into organisms, organisms into pages.

This documentation is the spec that will eventually feed the production Blazor component library and Storybook.

> 📁 **`_templates/`** holds reference-only scaffolding. Nothing in `_templates/` is ever treated as confirmed design. Real components live under the appropriate `atoms/`, `molecules/`, `organisms/` or `pages/` subfolder.

---

## Folder layout

```
/atoms       Buttons, inputs, icons, badges, labels — single-purpose primitives
/molecules   Forms, cards, search bars — small groups of atoms working together
/organisms   Tables, dashboards, navigation, modals — distinct sections of a UI
/pages       Full-screen layouts assembling organisms
/_templates  Reference example(s) — never used as real spec
```

---

## Process for adding a real component

1. **Identify the component during a journey or design review.** Reference the source meeting note.
2. **Pick the right level** (atom / molecule / organism / page).
3. **Create a file** in that subfolder, `kebab-case.md`.
4. **Use [`_templates/atom-example-button.md`](_templates/atom-example-button.md) as the shape reference** — never copy its content verbatim.
5. **Fill in:** purpose · variants · states · props · accessibility · example usage in journeys · open questions.

---

## Conventions

- Naming: `kebab-case.md` matching the component name.
- Reference Microsoft Fluent UI / Fluent 2 patterns where sensible — we're in the Microsoft ecosystem and users will recognise these.
- Note any accessibility requirement that comes from being a regulated/reporting context.

---

## Component index

| Type | Component | File | Status |
|---|---|---|---|
| _No real components yet — added as journeys uncover them._ | | | |
