# Workflow 03 — Subcontractor Procurement (Bid → Award)

**Group:** Project lifecycle
**Purpose:** From issuing a bid package to awarding a work order: managing the tender process for subcontractors.
**Trigger:** BoQ ready, new work package identified, or variation requiring subcontractor pricing.
**Frequency:** Weekly while a project is in pre-construction or experiencing variations.
**Owner (target):** Project & Commercial Lead (sign-off); JPMS for data and document handling.
**Current monthly hours:** ~35 h/month.
**Status:** Draft
**Last reviewed:** —

---

## Current state

1. Bid package compiled manually from BoQ extracts + drawings + scope notes.
2. Tender document folders created manually in SharePoint per trade.
3. Subcontractors emailed bid packages with instructions and deadline.
4. Returned quotes arrive by email; assembled into an Excel comparison sheet manually.
5. Award decision made; work order contract drafted in Buildertrend / Planyard.
6. Subcontractor onboarded into systems separately (Monday, RAMsApp, compliance docs).

---

## Target flow (post-automation)

1. Bid package auto-assembled from JPMS: BoQ items for the trade + drawings + standard T&Cs.
2. Subcontractors invited via JPMS portal (or email-with-secure-link if they aren't users).
3. Quotes returned directly into JPMS comparison view; no manual transcription.
4. Side-by-side comparison auto-generated; Project Lead reviews and awards.
5. On award, a work order is auto-generated with agreed scope, price, and T&Cs.
6. Subcontractor compliance check (workflow 08) runs automatically as part of award.

---

## JPMS functionality required

- Bid package builder (BoQ extract + drawings + T&Cs templates).
- Subcontractor directory with contact details and trade tags.
- Tender portal — or email-with-link — for non-system users.
- Quote return mechanism (form or upload).
- Comparison and award workflow with approval routing.
- Work order generation from approved quote.
- Tender history per project (who quoted, who won, at what price).

---

## Integrations & adjacent systems

- **Email** (for subcontractors not on the system).
- **Xero** (for payment terms reference).
- **Compliance workflow 08** (gated by compliance status).

---

## Acceptance criteria — "done looks like"

- Bid package is built in one action, not assembled across SharePoint / Outlook / Excel.
- Subcontractor quotes appear in JPMS without manual transcription.
- Awarded work orders flow directly into AP matching (workflow 09).

---

## Entities touched

`Project` · `BoQ Line Item` · `Bid Package` · `Subcontractor` · `Quote` · `Work Order` · `Compliance Document`

---

## Roles involved (RBAC)

| Role | Involvement |
|---|---|
| Project & Commercial Lead | Owner — award decision |
| Office & Compliance Coordinator | Contributor — supplier directory upkeep |
| Subcontractor (external) | Source — submits quotes |
| Finance Director | Read — visibility on awarded value |

---

## Open questions

- [ ] Approval threshold for award sign-off — is FD/Director sign-off required above a value?
- [ ] How are tied bids resolved — pure price, or weighted scorecard?
- [ ] Standard T&Cs — single template or per-trade variants?

---

## Confirmation checklist

- [ ] Walked through end-to-end with the named owner
- [ ] Current-state steps confirmed against actual practice
- [ ] Target-flow steps agreed
- [ ] JPMS functionality list confirmed as sufficient
- [ ] Integrations list confirmed
- [ ] Acceptance criteria signed off
- [ ] Signed off by: _name, role, date_
