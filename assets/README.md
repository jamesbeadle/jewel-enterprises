# Assets

Static files referenced from journeys, component docs, and prototypes.

## Folder layout

- `/screenshots/` — UI screenshots referenced from journey Markdown files. Name them after the journey: `01-lead-capture.png`, `01-lead-qualified.png`.
- `/icons/` — icon assets. Prefer SVG.
- `/branding/` — logos, colour swatches, typography references.

## Conventions

- Always reference assets via **relative paths** from the file using them, e.g.:
  `![Lead Form](../../assets/screenshots/01-lead-capture.png)`
- Keep file sizes reasonable. Compress screenshots before committing.
- Don't commit large binaries — if a file is over ~2 MB consider linking out instead.
