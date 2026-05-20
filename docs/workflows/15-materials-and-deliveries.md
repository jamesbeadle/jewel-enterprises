# Workflow 15 — Materials & Deliveries (office/site procurement)

**Group:** Operations & comms
**Purpose:** From identifying a material or office need through quote, order, delivery, and goods-in.
**Trigger:** Site request, low-stock alert, new job requirement, or director instruction.
**Frequency:** Daily.
**Owner (target):** Office & Compliance Coordinator; site managers raise requests.
**Current monthly hours:** ~20 h/month.
**Status:** Draft
**Last reviewed:** —

---

## Current state

1. Site/team request comes in by WhatsApp / email / phone.
2. Coordinator contacts suppliers for quotes.
3. Order placed via supplier website or email.
4. Delivery tracked manually; team reminded of incoming items.
5. Goods checked on arrival; issues flagged.

---

## Target flow (post-automation)

1. Request raised in JPMS by site or office, tied to a project where applicable.
2. Approved suppliers and current pricing available in the JPMS supplier directory.
3. Order placed (manual or via supplier integration where possible); reference held in JPMS.
4. Delivery date and status tracked against the request.
5. Goods-in confirmed on the app; discrepancies raised back to the supplier.

---

## JPMS functionality required

- Procurement request module.
- Approved supplier directory with rates.
- Order tracking with status.
- Goods-in confirmation.
- Project cost allocation (auto for project orders).

---

## Integrations & adjacent systems

- **Outlook** (suppliers without portals).
- **Amazon**, **Paperstone** (office consumables).
- **AP** (workflow 09) for invoice match.

---

## Acceptance criteria — "done looks like"

- All material orders are tied to a project or office cost code.
- Deliveries are tracked without separate spreadsheets.
- Supplier invoices match to known orders (workflow 09).

---

## Entities touched

`Procurement Request` · `Supplier` · `Work Order` · `Project` · `Supplier Invoice`

---

## Roles involved (RBAC)

| Role | Involvement |
|---|---|
| Office & Compliance Coordinator | Owner — places orders, tracks |
| Site Team | Source — requests + goods-in |
| Project & Commercial Lead | Approver — above-threshold orders |
| Finance Director | Read — feeds AP match |

---

## Open questions

- [ ] Approval threshold — single value, or per-supplier?
- [ ] Stock items — does JPMS track office stock, or just orders?
- [ ] Returns / credit notes — handled here or in AP (workflow 09)?

---

## Confirmation checklist

- [ ] Walked through end-to-end with the named owner
- [ ] Current-state steps confirmed against actual practice
- [ ] Target-flow steps agreed
- [ ] JPMS functionality list confirmed as sufficient
- [ ] Integrations list confirmed
- [ ] Acceptance criteria signed off
- [ ] Signed off by: _name, role, date_
