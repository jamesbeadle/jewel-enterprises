# Journey 08a — Subcontractor: upload renewed compliance document

> Persona slice through [Workflow 08 — Subcontractor Compliance & Onboarding](../workflows/08-subcontractor-compliance-and-onboarding.md). The subcontractor self-service side.

**Actors:** P02 Subcontractor (primary, external). Reviewer: P04 Office & Compliance Coordinator. Read: P03 Project & Commercial Lead, P07 Finance Director.
**Goal:** Subcontractor uploads a renewed insurance certificate / RAMS / ticket and the system recognises the expiry update without anyone chasing them.
**Frequency:** As required — typically tied to renewal cycles.
**Success metric:** Zero subcontractors working with expired documents at the point of payment.
**Status:** Draft
**Last reviewed:** —

---

## Trigger

JPMS sends the subcontractor an expiry reminder (60/30/7 days before), or the subcontractor proactively visits the portal.

---

## Pre-conditions

- Subcontractor has a JPMS portal account (or accesses via secure link).
- The compliance document type and expected fields are configured.

---

## Steps

### 1. Land on the compliance home page
- List of all required documents with status: current / expiring soon / expired / missing.

### 2. Upload a new version
- Drag-and-drop or pick file. JPMS OCRs the document for issue/expiry dates and pre-fills the form.

### 3. Confirm details
- Subcontractor confirms or corrects the pre-filled dates and any reference numbers.

### 4. Submit
- New version replaces the prior one (with full audit trail). Status updates immediately.
- Office & Compliance Coordinator gets a notification if review/sign-off is required for this document type.

### 5. (Optional) Refresh CIS status
- For UK subcontractors, a one-click "Refresh CIS" calls HMRC and updates the status (workflow 08 dependency).

---

## Edge cases & exceptions

- Wrong document uploaded — subcontractor can replace before submit.
- Expiry date unreadable — falls back to manual entry, document still attached.
- Subcontractor has multiple trading entities — entity selector on upload.
- Subcontractor tries to win an award while a document is expired — workflow 03 award is blocked with a clear message.

---

## Data structures (referenced)

- `Subcontractor`, `ComplianceDocument`, `RenewalEvent`, `CISStatus`. See [`/docs/data-models/entity-relationship.md`](../data-models/entity-relationship.md).

---

## Permissions

| Step | Role | Can do |
|---|---|---|
| 1–5 | P02 Subcontractor | Self-service upload of own documents |
| 4 | P04 Office & Compliance Coordinator | Review and sign-off where required |
| All | P03 Project & Commercial Lead | Read before award |
| All | P07 Finance Director | Read at point of payment |

See [`/docs/requirements/permission-matrix.md`](../requirements/permission-matrix.md).

---

## Open questions

- [ ] OCR accuracy threshold — at what confidence do we accept vs ask?
- [ ] Document types that require Coordinator approval vs auto-accept.
- [ ] Hard block vs soft warn at expiry — defer to workflow 08 decision.

---

## Confirmation checklist

- [ ] Walked through with a real subcontractor
- [ ] OCR fallback confirmed
- [ ] Block-on-expired-document behaviour confirmed
- [ ] Permissions confirmed
- [ ] Signed off by: _name, role, date_
