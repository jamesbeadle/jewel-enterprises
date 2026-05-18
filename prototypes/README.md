# Prototypes

This folder will hold interactive prototypes used during on-site role-play sessions.

## Plan

The intended primary prototype is a **Blazor WebAssembly PWA** called the **Journey Index** — a "walkthrough menu" listing every user journey, where clicking through navigates a stakeholder through dummy-data screens that match the relevant journey Markdown file.

We will scaffold this in a later session, once enough journeys are drafted to make it worth building. Until then, this folder contains:

- `/demos/` — static HTML mockups linked from individual journey files.

## Conventions (once scaffolded)

- Project location: `prototypes/blazor-journey-index/`
- Standard `dotnet new blazorwasm --pwa` template.
- Hosted on Azure Static Web Apps for free, public, shareable session links.
- All data is dummy / hard-coded — no live integrations from prototypes.

## Why defer

Scoping is about clarifying intent. Building the prototype shell before we have a couple of journeys to populate it would be premature and would risk anchoring the design on framework decisions rather than business needs.
