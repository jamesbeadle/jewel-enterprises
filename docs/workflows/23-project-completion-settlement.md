# Workflow 23 — Project Completion Settlement & Zero-Rated VAT Analysis

**Group:** Project lifecycle
**Purpose:** On project completion, close out all open commercial items — cost-code allocations, timesheet approvals, valuation tail, retention release — and perform the zero-rated VAT analysis agreed with the client.
**Trigger:** Practical Completion declared on the project (the same upstream event as workflow 07 defects).
**Frequency:** Per project.
**Owner (target):** Finance Director (settlement and VAT); Project & Commercial Lead (commercial sign-off); Directors (final client agreement on VAT outcome).
**Current monthly hours:** _to be confirmed_ — not in the original audit; surfaced 2026-05-20.
**Status:** Draft
**Last reviewed:** —

---

## Relationship to workflow 07

Workflow 07 handles the **defects** side of close-out (snag register, subcontractor assignment, final defect sign-off, retention release trigger). This workflow (23) handles the **commercial settlement** side. They share the Practical Completion trigger and the final retention-release dependency; otherwise the streams are independent and run in parallel.

---

## Current state

1. At PC, the FD pulls together a manual settlement view in Excel: open cost codes, untimed labour, outstanding subcontractor amendments, final valuation, retention.
2. Zero-rated VAT analysis is done in Excel against the contract, then circulated for agreement with the client by email.
3. Retention release is requested separately, often after the defect-period clock has already started.
4. Final settlement record is whatever the FD's email trail captured.

---

## Target flow (post-automation)

1. **PC trigger** raised on the project. JPMS opens the settlement workspace (alongside the defect register from workflow 07).
2. **Open-items dashboard** — JPMS lists everything still open at PC: unapproved timesheets (workflow 22), open cost-code allocations against zero budget, subcontractor invoice amendments (workflow 09), outstanding valuation items (workflow 05), unbilled work-order tail (workflow 03).
3. **Settle each item** — the workspace routes the FD through resolutions: approve or reject open timesheets; re-allocate or write off zero-budget items; finalise subcontractor amendments; agree the final valuation.
4. **Zero-rated VAT analysis** — JPMS produces the draft analysis from contract metadata + the settled cost ledger (which BoQ items / cost codes qualify for zero-rating; which fall under standard rate). FD reviews and adjusts.
5. **Client agreement** — analysis is issued to the client via the portal or email-with-link. Client confirms agreement (or comments). Disagreements route back to FD for revision.
6. **Final settlement record** — once VAT is agreed and all open items are closed, JPMS produces the final settlement record. This is the trigger for the retention-release transaction in AP (workflow 09) and the close-out pack in workflow 07.
7. **Project archived** — project status moves to "closed". Read access remains for audit; write access locked behind a director override.

---

## JPMS functionality required

- Settlement workspace per project (opened at PC).
- Open-items dashboard combining feeds from workflows 03, 05, 09, 22.
- Inline resolution actions per item (approve / re-allocate / write off / amend).
- Zero-rated VAT analysis generator from contract + settled cost ledger.
- Client portal flow for VAT analysis review and agreement.
- Final settlement record generator (audit-grade summary).
- Retention release trigger into workflow 09.
- Project archive state with read-only audit access.

---

## Integrations & adjacent systems

- **Xero** — final retention transaction; final VAT posting.
- **JPMS workflows 03, 05, 07, 09, 11, 22** — the settlement workspace consumes open items from each.
- **Client portal** (Dwellant / Vantify / direct email-with-link) — VAT analysis issuance.
- **HMRC** — VAT reporting downstream (no direct integration in phase one; data is correct in Xero).

---

## Acceptance criteria — "done looks like"

- Every project that reaches PC has a single settlement workspace; nothing is settled in email or Excel.
- Zero-rated VAT analysis is produced from data the system already holds; FD reviews instead of building from scratch.
- Client agreement on the VAT outcome is captured **in-system** with an audit trail, not in an inbox.
- Retention release happens on settlement completion, not on a separate request.
- The closed project's final settlement record stands as the audit-grade summary for that contract.

---

## Entities touched

`Project` · `Practical Completion` · `Cost Code` · `Cost Code Allocation` · `Cost Code Budget` · `Timesheet Approval` · `Work Order` · `Valuation` · `Variation` · `Supplier Invoice` · `Sales Invoice` · `Payment Run` · `VAT Analysis` · `Settlement Record`

See [`/docs/data-models/entity-relationship.md`](../data-models/entity-relationship.md).

---

## Roles involved (RBAC)

| Role | Involvement |
|---|---|
| P01 Architect / Client | **Approver** — agrees the zero-rated VAT analysis |
| P03 Project & Commercial Lead | Approver — commercial sign-off on open commercial items |
| P07 Finance Director | **Owner** — runs settlement; produces and adjusts VAT analysis; triggers retention release |
| P08 Directors / MD | Approver — final sign-off on the VAT outcome with the client |

See [`/docs/requirements/permission-matrix.md`](../requirements/permission-matrix.md).

---

## Open questions

- [ ] Who triggers Practical Completion → is it the same trigger as workflow 07 defects (single PC event firing both workflows), or a separate explicit "PC declared" event?
- [ ] VAT analysis sign-off responsibility — who is the named client-side signatory, and what's the SLA for their response?
- [ ] Retention release — does it wait for **both** workflow 07 (defects signed off) and workflow 23 (settlement closed), or just settlement?
- [ ] Zero-rated VAT analysis — is this always required on every project, or only certain contract types (e.g. new build vs refurb)?
- [ ] Disputed VAT outcome — how many revision rounds with the client before escalating to Directors?
- [ ] Are there contract types where settlement happens **without** retention (e.g. fixed-price design-only)? If so, retention-release trigger needs to be optional.

---

## Confirmation checklist

- [ ] Walked through end-to-end with the Finance Director on a recent completed project
- [ ] Walked the VAT analysis flow with the Directors and a real client
- [ ] Open-items dashboard confirmed as exhaustive (no settlement item lives only in someone's email)
- [ ] Retention release trigger confirmed against workflow 09 AP
- [ ] Project-archive state confirmed (read access vs locked write)
- [ ] Permissions confirmed
- [ ] Signed off by: _name, role, date_
