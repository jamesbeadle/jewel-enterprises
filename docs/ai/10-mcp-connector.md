# 10 — The MCP connector: the portal in everyone's own AI tool

*Written 2026-08-27, the day the in-portal chat was retired, and the one description of how AI
reaches JPMS. The chat's own documents (00–09), its agent registry and its AI Agents page were
removed on 2026-09-10; the skill store and the AI Skills / AI Actions pages are what remains of
that design (see 12-connector-weakness-audit.md for why).*

## 1. The decision

The in-portal chat ran every conversation through the company's Anthropic API key: the portal was
the AI product, and Jewel paid per token for it — model calls, transcript replay, reply collection,
the lot. The inversion: **the portal stops being an AI product and becomes an AI data source.**
Team members use whatever assistant they already pay for — Claude (web, desktop, mobile, Claude
Code) or Perplexity — and connect it to JPMS over the Model Context Protocol. Their subscription
covers the model; the portal serves data and records who did what. The three one-shot Procurement
helpers (tender extraction, bid-package suggestion, trade resolution) keep the API key — they are
embedded workflow steps, not chat.

What the chat's fortnight of hardening taught us carries straight over: the tool catalogue, the
role filtering, the evidence tools (sources, request context, record emails), the skill store and
the activity log all survive. What died was the conversation machinery — AiTurnRunner, transcript
budgeting, reply collection, UI actions, dialog tasks — because the conversation now lives in the
user's own AI tool, which is better at being a chat than a Blazor panel ever was.

## 2. The shape

Three pieces, all in the api project:

- **`Features/Connect`** — a minimal OAuth 2.1 authorisation server: dynamic client registration
  (RFC 7591), `GET /api/oauth/authorize` → the SPA consent page (`/connect/authorize`) →
  `POST /api/oauth/approve` (session-cookie authed; mints a single-use code bound to the signed-in
  user) → `POST /api/oauth/token` (code + PKCE S256, and refresh-token rotation). Public clients
  only — no client secrets; PKCE and the user's own portal sign-in are the security. Discovery
  documents are served under `/api/well-known/…` and surfaced at the standard `/.well-known/…`
  paths by rewrites in `staticwebapp.config.json`. Tokens follow the `SessionManager` contract:
  raw secret leaves once, SHA-256 hash at rest (`OAuthClients` / `OAuthAuthCodes` / `OAuthTokens`,
  migration `20260827190000_AddAiConnectorOAuth`). Access tokens live 7 days, refresh 90 with
  rotation; revoking a family (the AI Connections page, `/settings/ai-connections`) kills the lot.

- **`Features/Mcp`** — `POST /api/mcp`, a stateless Streamable-HTTP MCP server, hand-rolled
  JSON-RPC (`initialize`, `ping`, `tools/list`, `tools/call`; notifications → 202; GET/DELETE →
  405). Plain `application/json` responses, no SSE, no session ids — tool calls are short DB
  reads/writes and the SWA gateway allows ~45s. A missing/invalid bearer answers 401 with
  `WWW-Authenticate: Bearer resource_metadata="…/.well-known/oauth-protected-resource"`, which is
  what sends Claude and Perplexity into the OAuth flow. A valid token resolves to a
  `SignedInUser` via `SignedInUserResolver.ResolveByEmailAsync` (same directory + roles as the
  cookie path, revoked users refused), and `AuditActor` is set so every audit write inside a tool
  carries the token's user.

- **The tool catalogue** (`Features/Ai/Tools`) — unchanged in spirit. `AiToolCatalogue.ForConnector(user)`
  is the whole per-user filter: every tool whose `VisibleTo` admits one of the user's roles, and
  nothing else — the ADR-002 rule (a tool the caller could not use is never described) carried
  over verbatim. The chat-only tools (navigate_to, open_modal, update_open_modal, switch_agent,
  the Control-Centre staging tools, read_selected_email, chat attachments) are gone; the read
  surface (find_by_reference, get_request_context, the list_* family, sources, valuation/variation
  context, skills) remains, plus the connector's first **write tools** (`AiWriteTools`):
  `post_request_message`, `add_todo`, `complete_todo`, `log_todo_progress`, `save_skill`. Each
  write composes the SAME Authorisation + Validation + command handler its HTTP endpoint uses,
  actor stamped server-side — a write from Claude is indistinguishable in the record from a click.
  Financial actions, deletion and email sending are deliberately absent.

Images still work: a tool returning an `AiImageToolResult` marker is translated into an MCP
`image` content block, so the model sees drawings and photos, not base64 text.

### 2b. Why the MCP endpoint lives on its own host

Found live on launch day: Azure Static Web Apps **strips/overwrites the client's `Authorization`
header** before requests reach its managed functions (github.com/Azure/static-web-apps issues
158 & 275) — the OAuth flow worked end to end, then every bearer call answered 401. So `/api/mcp`
is served by a **standalone Function App** running the same api project
(`infra/azure-mcp-host-setup.sh` provisions it; `.github/workflows/jpms-mcp.yml` deploys it),
where the header passes through untouched. The split of responsibilities:

- **Portal domain (SWA)** — the SPA, every cookie-authed endpoint, the whole OAuth flow
  (register/authorize/approve/token) and the `/.well-known` discovery documents.
- **MCP host (Function App)** — the AI tools' JSON-RPC calls with the bearer. Its 401 points
  `resource_metadata` back at the portal's discovery documents, whose `resource` field names the
  MCP host's URL (`Mcp__PublicUrl`, set on both apps), so the client's audience check matches.

The Function App necessarily exposes the rest of the api's endpoints on its host too; they are
all session-cookie gated and simply answer 401 there — only `/api/mcp` accepts bearers.

Two Perplexity-shaped accommodations (2026-08-28, found when the first Perplexity connect
failed with "server does not support automatic registration"):

- **Discovery on the MCP host's own origin.** Claude bootstraps from the 401's
  `resource_metadata` pointer; Perplexity instead probes
  `/.well-known/oauth-authorization-server` directly on the MCP URL's origin. The MCP deploy
  workflow therefore blanks the Functions route prefix on that host only, and
  `WellKnownEndpoints` carries literal `.well-known/…` routes (inert under the portal's `api`
  prefix) plus an `api/mcp` alias in `McpEndpoint` so the published URL is unchanged. The
  root-served AS metadata names the MCP host as `issuer` (RFC 8414 origin check) with the
  endpoints still on the portal.
- **A client secret at registration.** Perplexity hard-errors when DCR returns no
  `client_secret`, even registering as a public client. Registration now issues one (hash at
  rest, `OAuthClients.SecretHash`, migration `20260828100000_AddOAuthClientSecret`); the token
  endpoint verifies it only when presented (form or Basic). PKCE remains mandatory and
  Claude's secretless clients are untouched.

### 2c. The action gateway — full write parity (2026-08-28)

The §2 "first write tools" were day one. The connector now mirrors **every command endpoint the
pattern can honour (~236 actions)** — record creation and editing, status changes, approvals,
financial actions, portal email sends — through THREE gateway tools rather than 236 first-class
ones (200+ schemas would drown every MCP client's context):

- **`list_actions`** — the catalogue, filtered to the caller's roles at listing (ADR-002 applies
  to actions exactly as to tools), grouped by area, one line each.
- **`describe_action`** — one action's full description, notes and its JSON argument schema,
  generated by reflection from the command contract (`AiActionSchema`).
- **`perform_action`** — executes it: bind arguments onto the contract with the actor stamped
  server-side (`EmailStamps`/`NameStamps` mirror exactly what the HTTP endpoint stamps — those
  parameters never appear in the schema), then the SAME Authorisation.Allows (typed-overload
  matched), Validation.Check/CheckAsync and ICommandHandler the endpoint composes, resolved from
  the same DI scope. Handler guard exceptions (InvalidOperationException) return as messages,
  mirroring the endpoints.

**Confirm-first actions (2026-08-28, accountant round).** Actions that mint a new party or
account — a subcontractor/supplier, a client, an architect practice, a portal user — and every
irreversible action (the "There is no undo" set: deletes, merges, `confirm_valuation_claim`)
declare `RequiresConfirmation: true`. `perform_action` REFUSES the first call of such an action
outright (`confirmationRequired: true`) with instructions to check for an existing record, show
the user exactly what will happen and get their explicit yes, then re-call with `confirm: true`;
`describe_action` surfaces the same protocol so the model can plan the confirm turn. The gate is
server-enforced two-step, so a model can never create or destroy one of these in a single move —
though the honest limit is that `confirm: true` is still asserted by the model: the server cannot
itself see the user's yes. `AiActionRegistry` asserts at boot that any action whose own text says
"no undo"/"irreversible"/"permanently" carries the flag, so a new destructive action cannot ship
without the gate.

Declarations live in `Features/Ai/Tools/Actions/*Actions.cs` — one data-only `AiAction` entry per
command, one file per area, discovered via `IAiActionSource` at boot. `AiActionRegistry` asserts
the lot at startup (through `AiRegistryDriftCheck`): duplicate names, stamps that aren't contract
parameters, or a gate class lacking a typed Allows/Check overload for the command all kill the
boot. Endpoints that could not be mirrored — multipart uploads, endpoints with inline role checks
and no Authorisation class, flags the server derives from roles (UpdateManualWorkOrder) — are
recorded as `// Skipped:` comments at the bottom of each area file: the written record of what
the gateway does NOT cover and why. A skipped endpoint that later grows a proper Authorisation
class can then be declared.

### 2d. Attached doctrine — skills wired to actions (2026-08-31)

The skill store invites reading (`list_skills` in the initialize instructions); this wires it to
the gateway so doctrine arrives without the model deciding to go looking. **`AiActionSkills`** (an
additive table, migration `20260831090000_AddAiActionSkills` / `add-ai-action-skills.sql`) attaches
a skill to one action or to a whole area; **`describe_action` resolves a target's attachments live
on every call and inlines the active skills' bodies** (plus their reference keys, loadable with
`load_skill_reference`) next to the argument schema, and `list_actions` marks guided actions in
their one-liner. Since `describe_action` is the unavoidable hop before `perform_action`, attached
doctrine rides in on the road the model must travel — client-agnostic, no prompt to keep in step,
and a skill edit is in force on the very next call. Curated on the **AI Actions** admin page
(`/admin/ai-actions`, same `SkillRoles.ManageSkills` gate as the skill store) through
`GET/POST /api/ai/action-skills` — area-level attachments carry discipline doctrine wholesale
(~34 areas vs ~236 actions); action-level ones handle the exceptions. Attachments name targets
loosely: a code rename orphans the row, the page surfaces orphans for detaching (an empty save is
exempt from the target-must-exist validation for exactly that cleanup). This is guidance, not
enforcement — the server cannot make a model follow prose; rules that must bind belong in the
command's Validation, exactly as over HTTP.

## 3. Audit

Every `tools/call` writes one `AgentActivity` row (`AgentTrigger.Mcp`, actor = the token's user,
the tool name, a clipped argument summary, duration, outcome) — the Agent Activity page reads
exactly as before. Write tools additionally append to the client-facing `AuditTrail`
(`NotePosted` / `TodoCreated` / `TodoCompleted`). The AI Connections page lists live connections
(per user; administrators can list everyone's) and revokes them.

## 4. Extending it

- **A new read tool** declares the RoleSet its backing query gates on — same rule as ever, checked
  by conscience and review; `AiRegistryDriftCheck` (slimmed, still boot-failing) enforces name
  uniqueness and record-type reachability. Add the record type's reach entry in the same commit.
- **A new write tool** goes in `AiWriteTools`, composing the existing Authorisation + Validation +
  handler for its command, with `AuditTrail` after success. Say plainly in the description that it
  writes, and what.
- **Scopes** are deliberately one (`portal`); per-tool permissioning is the user's roles, applied
  at execution, not OAuth scopes.
- The connector has no conversation state to budget, no prompts to keep in step — the drift
  surface is the catalogue itself.

## 5. Operations

- No new secrets. The Anthropic key stays only for the Procurement helpers.
- New tables are additive (`AddAiConnectorOAuth` — apply before/with deploy); the chat tables drop
  in `20260827200000_DropAiChatTables` (apply after the deploy). Scoped scripts only, per
  CLAUDE.md.
- The `ai-attachments` blob container is orphaned once the chat tables drop —
  `infra/run-ai-attachments-lifecycle.sh` (or container deletion) reclaims it.
- Kill switch: revoke connections per user on the AI Connections page; for everything at once,
  stop the MCP Function App (`az functionapp stop`) — the portal itself is untouched.
- Team setup instructions: `docs/Portal-AI-Connector-Team-Guide.md`.
