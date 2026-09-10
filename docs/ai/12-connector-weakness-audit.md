# 12 — Connector weakness audit (2026-09-10)

*Why Jeremy's Claude-over-MCP sessions went wrong this week, traced to the exact skills and code, with the fix order. Written from the repo at 81a5407 plus the live skill store and action catalogue as the connector serves them today.*

## The short version

Three of his six complaints come from one thing: Nigel's three personal commercial skills (`commercial-director`, `commercial-director-mistake-prevention`, `nigel-commercial-doctrine` — 70 KB between them, written for Nigel's own Cowork/Claude Code environment and for dispute correspondence) are attached at *area* level to Procurement, Correspondence, Commercial, Valuation invoices, Variations and Requests. Every `describe_action` on those areas inlines them in full. They tell the model, in so many words, "never load a drafted reply into Outlook — write every draft to a file and hand it to the user", "play the minimum evidence, leave the counter-party guessing", and "ask Nigel via `ask_user_question` before drafting". That is why the invite came back as markdown drafts instead of a mailbox draft, why the reply to Keeley at Fulcrum asked her three questions and answered none of hers, and why the model keeps behaving as if it is in a dispute when it is asking a utility company for a quote. The one skill that actually covers the task (`jpms-tender-award`, 1.9 KB) is under 4% of what the model reads before it acts.

The other three complaints are real code defects: the invite draft BCCs everyone on the tender list regardless of status or whether they were already sent the invite, and its result never lists the attachments (so the model says "no attachments" while Outlook shows 13); and the Xero raise does all four things Jeremy said it does (name-matches the contact and *creates* one on a miss, stamps today's date, numbers the line from the claim, and has no way to record a hand-raised Xero number).

None of this is Jeremy being unclear. He was clear. The system was carrying the wrong instructions and had gaps he could not have worked around.

## His six complaints, traced

| What he saw | Root cause | Kind |
|---|---|---|
| "Why not the bid package route… I held off… markdown drafts" and "What the hell is a bid package?" | `commercial-director` D7 + `mistake-prevention` M12 ("Never build a pipe that sends. Every draft is written to a file… and handed to the user") inlined on every Procurement and Correspondence action; the model obeyed. The jargon is because `jbb-second-brain` has no map from the words people use ("tender", "enquiry", "send it out") to the portal's names. | Skills |
| Fulcrum: reply asked Keeley three questions, didn't answer hers | `nigel-commercial-doctrine` (24 KB, "hold ammunition in reserve", "the sent letter should be shorter than the evidence base supports") is attached to Correspondence and Requests & RFIs, so a quote enquiry was drafted in dispute mode. `jpms-email-triage` says nothing about answering the questions the sender asked. | Skills |
| Sent the same invite twice a week apart | `BidPackageInviteMailAssembler.DefaultBccAsync` BCCs every recipient with an email — no status filter, no "already sent" fact exists on the recipient (`InvitedAt` means *added to list*), `read_record_emails` omits To/Bcc so the model cannot see who got the first one, and the action schema has no way to name recipients. | Code |
| "Draft came back with no attachments" while Outlook shows 13 | The result contract `BidPackageInviteDraft` has no attachment list; its only file field is `LinkedFiles` = overflow download links, normally `[]`. `get_bid_package_context` shows linked documents with blank `drawingCode`/`title` (no file-name fallback). The model joined those two dots and got it wrong. | Code |
| Ravenswood Valuation 05: four Xero raise defects, keyed by hand as INV-0227 | All four confirmed in `ValuationInvoiceXeroRaisePlanner` / `XeroClient.SalesInvoice`: exact name match then `Contact.Name` creation with no blocker; `DateTime.UtcNow.Date`, due date only from certificate + contract; description from `claim.ClaimNumber`; `IssueValuationInvoice` takes only the id and nothing else can write `XeroInvoiceNumber`. | Code |
| "All I wanted to do was raise an invoice" — it wrote a spec instead | The preview said "Not in Xero — created with the invoice" with `canRaise: true`, so the model correctly refused to proceed, but had no path to finish (Issue cannot take the Xero number), and the commercial doctrine's "paired internal file / reserve register" habit turned the dead end into a memo to James. | Code + skills |

## 1. The doctrine problem (do this first — it is a data change, no deploy)

What the live store attaches today (from `describe_action` on the real server):

| Action | Skills inlined | Size | Relevant to the task |
|---|---|---|---|
| `prepare_bid_package_invite_draft` | commercial-director, mistake-prevention, jbb-second-brain, connector-mechanics, jpms-tender-award | 57.7 KB | tender-award (1.9 KB) |
| `send_mailbox_email` | commercial-director, jbb-second-brain, connector-mechanics, jpms-email-triage, nigel-commercial-doctrine | 54.1 KB | email-triage (4.7 KB) |
| `raise_valuation_invoice_in_xero` | commercial-director, mistake-prevention, jbb-second-brain, connector-mechanics, jpms-valuation-cycle, nigel-commercial-doctrine | 85.4 KB | valuation-cycle (3.2 KB) |

`commercial-director-mistake-prevention` is 33 KB live against 12.9 KB in `docs/ai/skills` — someone's Claude has been appending lessons through `save_skill`, and the seed files no longer describe what is running. The three commercial skills also cite `search_files_v2`, `browser_task`, SharePoint, Reportlab, openpyxl, a "project workspace", a "Brain wiki" and a `commercial-director-intake` skill — none of which exist in the connector. A model reading "Ask Nigel — via `ask_user_question` — what the counter-party has said… Never draft without this" while helping Jeremy draft an invite has been handed someone else's job description.

The abstraction: **doctrine must be scoped by situation, not by area.** An area is a bag of actions; a situation is "this email is a dispute with the CA" versus "this email asks a supplier for a quote". Attaching dispute doctrine to an area makes every quote enquiry a dispute. The connector has no situation detector, so the honest design is: attach only the *procedural* jpms-* skills to areas (they describe how the portal works, which is true in every situation), keep the commercial skills loadable by name, and put one line in `jbb-second-brain` that tells the model when to go and load them.

Concrete changes on the AI Actions admin page (`/admin/ai-actions`), or as a SQL patch against `AiActionSkills`:

1. Detach `commercial-director`, `commercial-director-mistake-prevention` and `nigel-commercial-doctrine` from every area. They stay in `list_skills`; `jbb-second-brain` gains: "Load `nigel-commercial-doctrine` only when the user says the correspondence is a dispute, a notice, a pay-less position or a claim against Jewel — never for tenders, quotes, suppliers or routine project mail."
2. Keep the jpms-* attachments as they are (they are short and correct in spirit), after the rewrites in §5.
3. Then re-seed `docs/ai/skills` from the live rows so the repo matches production (a one-off `scripts/export-ai-skills.sql`, or just paste), because today the repo lies about what is deployed.

Expected effect: `describe_action` for the invite drops from 57.7 KB to ~6 KB; for the Xero raise from 85 KB to ~8 KB; and every remaining sentence the model reads is about the portal it is operating.

## 2. The invite path (code, no migration for the first half)

Files: `api/Features/Procurement/Commands/BidPackageInviteMailAssembler.cs`, `PrepareBidPackageInviteDraftHandler.cs`, `SendBidPackageInviteHandler.cs`, `contracts/Procurement/PrepareBidPackageInviteDraft.cs`, `contracts/Models/BidPackageInviteDraft.cs`, `api/Features/Ai/Tools/BidPackageContextReads.cs`, `api/Features/Ai/Tools/Actions/ProcurementActions.Invites.cs`, `api/Features/Ai/Tools/AiRecordTools.Correspondence.cs`.

- `DefaultBccAsync` excludes `Declined` (and `Won`); the recipient statuses are `Invited=0, Responded=1, Declined=2, Won=3` and `Invited` is the *added* state, which the UI already renders as "On list" for exactly this reason.
- `PrepareBidPackageInviteDraft` gains optional `recipientIds`; when given, only those are BCC'd. Make the action `RequiresConfirmation: true`, with the refusal listing exactly who will be BCC'd and which files attach — this is the server-side fix for "eleven people got it again" and it costs nothing.
- `BidPackageInviteDraft` and `BidPackageInviteSendOutcome` gain `attachedFiles` (names from `plan.Attach`); rename or annotate `linkedFiles` as "overflow download links". The description text for the action should say the draft attaches the pricing schedule, the company T&Cs, the package's tender documents and the linked drawings — today it says "linked drawings" only.
- `BidPackageContextReads.LinkedDocumentsOf` falls back to the current revision's file name when code and title are blank, and `TenderListOf` renames `invitedAt` to `addedAt`.
- `read_record_emails` includes `to`/`cc`/`bcc` on outbound copies, so a model that reads the record before drafting can see who already received the invite.
- Second half, with a migration: `BidPackageRecipients.InviteSentAt` stamped by `SendBidPackageInviteHandler` for every matched Bcc address (the portal composer path), shown in the tender list and in `get_bid_package_context`. The Outlook-draft path cannot know when a human presses Send, so for that path the sent copy's tag remains the evidence — which is why the `read_record_emails` change above matters.

## 3. The Xero raise (code, one additive migration)

Files: `api/Features/ValuationInvoices/XeroRaise/ValuationInvoiceXeroRaisePlanner.cs`, `ValuationInvoiceXeroRaiseSources.cs`, `api/Features/Xero/XeroClient.SalesInvoice.cs`, `XeroClient.SalesContact.cs`, `contracts/ValuationInvoices/RaiseValuationInvoiceInXero.cs`, `IssueValuationInvoice.cs`, `IssueValuationInvoiceHandler.cs`, `api/Data/Entities/ProjectEntity.cs`, `jpms/Components/ProjectDetailsEditor.razor`, `jpms/Features/ValuationInvoices/ValuationInvoiceXeroRaiseModal.razor`, the two Ai files for the action and the preview tool, `tests/…/RaiseValuationInvoiceInXeroTests.cs`.

1. **Contact**: `Projects.XeroContactId` + `XeroContactName` (migration), set from a picker over `ListXeroSuppliers` (it already returns customers) in Project settings, beside `XeroSiteName` which is the same kind of mapping. Planner reads it; `ContactId == null` after lookup becomes a blocker ("No Xero contact is mapped on this project — set it in Project settings"); the `Contact.Name` creation branch in `SalesInvoicePayload` goes. Keep the directory-link hop as a fallback only if you want it — I would drop it, it is a coincidence of exact name matching.
2. **Dates**: `InvoiceDate` and `DueDate` on both the preview query and the command (per-call, no columns needed; persist them on the invoice only if Jeremy wants them shown later). Modal gets two date fields that re-preview; certificate + contract stays the default when blank.
3. **Numbering**: Reference `Valuation {invoice.Number:00}`, description `Valuation {invoice.Number:00} - Payment due as per {PeriodMonth:MMMM yyyy} valuation report (ex VAT)`. Two lines in the planner. Ask Jeremy whether the certificate clause stays.
4. **Issue records the Xero number**: `IssueValuationInvoice` gains `xeroInvoiceNumber` (and resolves the Xero `InvoiceID` by number so the "already raised" blocker keeps working); the "Issue without raising in Xero" step in the modal gets the field; a narrow `record_valuation_invoice_xero_number` action (allowed at any status) back-fills VI-0002 to VI-0005 — `update_valuation_invoice` refuses Issued rows, so do not widen that one.

The action and preview text must then say plainly: *if the preview reports no mapped contact, stop, tell the user to map it in Project settings (or do it for them with `update_project_details`), and do not raise.* Today the preview normalises creation ("matched, or created with the invoice") and `canRaise` stays true — the model was told creation was fine.

## 4. Context economics and guidance gaps (why it feels like "one of those days")

- `tools/list` is 85 tools ≈ 80 KB (≈20k tokens) before the first message. `list_actions` is 291 actions across 36 areas, unpaged, ≈ 75 KB. `describe_action` is 55–85 KB each. A normal Jeremy conversation spends its first 150–200k tokens on catalogue and doctrine, and the model's attention is spread across seventy pages of prose to find the three sentences that matter. This is the single biggest reason the assistant "should have known" things it had already been told.
- Argument schemas are bare `{name: type}`. The contracts' `///` comments never reach the model, so `htmlBody`, `recipientIds`, `xeroInvoiceNumber` will all be guessed at unless `Description`/`Notes` prose carries them. Cheap fix: `AiActionSchema` reads `[Description]` attributes (or the XML doc file) onto properties.
- `get_current_context` returns `project: null` over MCP; 21 tools say "defaults to the project in view" and none ever does. `load_page_guide` asks for "a route from the site map" that the connector never sends (the map lived in the retired chat's system prompt). Either send a compact site map in `initialize.instructions`, or make `load_page_guide` accept a page *name* and list what exists.
- `jbb-second-brain` (2.3 KB, pinned everywhere) has terminology rules for the *portal's* words but no map from the *user's* words, no "do the task through the portal, not in prose" rule, no "answer what they asked before asking your own" rule, and its "nothing is sent by an agent — phrase as I've prepared…" sits one line away from being read as "don't create drafts either".
- Sales and KPI areas have no doctrine at all; `Leads & CRM` and `Tender enquiries` seed rows are orphans.

Recommended, in order of leverage: (a) `list_actions` requires `area` or `search` unless the caller passes `all: true`, and returns names only by default; (b) `describe_action` inlines only action-level attachments in full and area-level ones as one-paragraph summaries with a "load_skill for the rest" pointer, capped at ~12 KB; (c) property descriptions in the schema; (d) a 30-line site map in `initialize.instructions`.

## 5. Skills to rewrite

`jbb-second-brain` (pinned on every turn — the one that matters most):

- A vocabulary map, user words → portal words: "tender / enquiry / send out for prices / get quotes" → bid package (BPI-…) and its tender list; "the sub / the firm / the supplier" → directory record; "chase" → reply on the record's thread; "raise the invoice" → valuation invoice, raised in Xero from the claim card; "the valuation" → the frozen snapshot behind the invoice.
- Do the task, don't describe it: when the portal has an action for what the user asked, perform the read, show what will happen, get the yes, perform it. Prose drafts, markdown files and specs are the last resort, never the first, and never a substitute for a mailbox draft the portal can create.
- Replies answer first: before drafting any reply, list the questions the incoming email asks; every one is answered or explicitly parked ("we will confirm X by Friday") before the reply asks anything of its own. The reply's first paragraph is the answer, not a question.
- Before re-doing anything external, check the record's emails (`read_record_emails`) for the same thing already sent; say so if it was.
- Explain in the user's words: name the outcome ("your invite is in Drafts, BCC'd to the eight who have not had it"), not the mechanism ("the bid package route"). Never name a tool or action in a sentence to the user.
- When a step cannot complete (a missing mapping, a blocker), say the one thing the user can do right now to unblock it, then finish the job. A change request for James is a separate offer at the end, never the deliverable.
- The dispute-doctrine loading rule from §1.

`jpms-tender-award` (Procurement): the invite procedure — read `get_bid_package_context` and `read_record_emails` first; recipients to invite are those with no sent invite on record and status not Declined; draft with `recipientIds`; the draft attaches schedule + T&Cs + tender documents + linked drawings, and the result's `attachedFiles` is the truth about attachments; a person sends from Outlook; after sending, replies file themselves under the tag. Keep "add to tender list, never 'invite'" wording.

`jpms-email-triage` (Correspondence): add the reply doctrine — read the whole thread, answer every open question or park it, keep the sender's terms, one ask of your own at most, confirm envelope + body before `send_mailbox_email`. Remove nothing else.

`jpms-valuation-cycle` (Commercial, Valuation invoices): a "Raise in Xero" section — preview first; a missing contact mapping is a stop, fixed in Project settings; invoice date and due date come from the user or the certificate, never from "today" by assumption; reference and description number from the *invoice*; a hand-raised Xero invoice is recorded on Issue with its number.

`jpms-connector-mechanics`: one line — "confirm-first means confirm, then *do*; it never means hand the user a document to do it themselves."

The commercial trio: leave the bodies alone (they are Nigel's), detach them, and fix the drift so `docs/ai/skills` matches the store.

## 6. Order of work

| # | Change | Where | Effort | Migration | Deploy |
|---|---|---|---|---|---|
| 1 | Detach the three commercial skills from all areas; add the loading rule to jbb-second-brain | AI Actions admin page + `save_skill` | 20 min | no | none |
| 2 | Rewrite jbb-second-brain, jpms-tender-award, jpms-email-triage, jpms-valuation-cycle, connector-mechanics as in §5 | `save_skill` (live) + docs/ai/skills | 1–2 h | no | none |
| 3 | Invite path: BCC filter, `recipientIds`, confirm-first, `attachedFiles`, file-name fallback, `read_record_emails` recipients, action text | api + contracts | half a day | no | api |
| 4 | Xero raise: contact mapping + blocker, dates, numbering, Issue takes the number, back-fill action | api + contracts + jpms | a day | yes, one additive | api + jpms |
| 5 | `InviteSentAt` on recipients | api + jpms | 2 h | yes, additive | api + jpms |
| 6 | Context economics: list_actions filter default, describe_action cap/summary, schema property descriptions, site map in initialize | api | half a day | no | api |
| 7 | Export live skills back to docs/ai/skills; delete orphan seed rows; doctrine for Sales/KPI | scripts | 1 h | no | none |

Items 1 and 2 alone remove the behaviour Jeremy complained about most; they can be live in an hour with nothing to deploy. Item 4 is the one he asked for timing on. Do 3 before he sends the next tender.

## 7. Is this the right path?

Yes for 1–5 and 7. They are corrections to things that are plainly wrong, and each is small.

Item 6 is the one to think about, because it is architectural. The connector was designed as "the portal is a data source, the user's own Claude is the brain" (doc 10), and that is still right. But it was built when the catalogue was 40 tools and no doctrine; it is now 85 tools, 291 actions and 100+ KB of prose per action, served to a model the team cannot choose or prompt. The fix is not to send more guidance — it is to send less, in the right place: procedural doctrine at the action, hard rules in Validation (the invite BCC filter and the contact blocker are exactly this — a rule the server enforces does not depend on the model reading a paragraph), and situational doctrine loaded by name when the user names the situation. Rules that must bind belong in code; prose is for judgement.

Two things not to do: do not write more project instructions on Jeremy's side to compensate (his instruction is competing with 70 KB of doctrine he cannot see — fix the source), and do not add a fourth "assistant behaviour" skill on top of the existing ones; fold the behaviour rules into `jbb-second-brain`, which is already pinned everywhere.

One thing to add afterwards: a replay check. Keep his three scenarios (invite an existing tender list, reply to a supplier's question, raise a valuation invoice with a mismatched contact) as a written test script, and run them through a fresh Claude connection after every skill or catalogue change. That is what would have caught all of this before he did.

## 7b. Amendment after Nigel's reply (same evening)

Nigel: "My skills are only client and contract related — mainly architect/CA. Never for tender,
suppliers, staff or anyone outside my role. Need deleting for his role entirely." So the scoping
rule is **role**, not situation, and it belongs in code, not prose. Done tonight (data only):
the three commercial skills detached from all 16 areas that carried them; `jbb-second-brain`
added to the four areas that lacked it; the five jpms-* skills rewritten and saved live (the
second-brain now says plainly that Nigel's skills are Nigel's). Still to build: **skill
visibility by role** — a `VisibleTo` on `SkillEntity` filtered in `list_skills`, `load_skill` and
the `describe_action` guidance exactly as `AiToolCatalogue.ForConnector` filters tools; one
additive column; Nigel's three get his role only. Also to decide with Nigel: who may WRITE skills
(`SkillRoles.ManageSkills`) — mistake-prevention grew from 13 KB to 33 KB through `save_skill`.

## 8. What to tell Jeremy

It was not him. Three of the six things came from a set of commercial-dispute instructions that were attached to the whole connector by mistake, so his Claude was told to hold information back and to write files instead of using the portal. The other three are real defects in two features, and all four of his Ravenswood points are correct and will be built as he described them (with a date on it). His project instruction is welcome; ask him to send it so it can be checked against the new second-brain rather than guessed at — most likely it is fine and was fighting the doctrine, not causing it.
