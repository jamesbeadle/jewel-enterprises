# Integrations & System Landscape

Single source of truth for how JPMS relates to every other system named across the 23 workflows. Each system falls into one of three buckets:

- **Kept — core or peripheral.** JPMS integrates with it; it stays after rollout.
- **Replaced by JPMS.** The system is currently in use, but JPMS takes over its role. May stay as archive only.
- **Adjacent / context.** Mentioned in workflows for context but JPMS doesn't integrate directly.

**Status:** Draft — refined per workflow as each one moves Draft → In Review.

---

## Legend

- **Direction:** `→ JPMS` (JPMS consumes from it) · `JPMS →` (JPMS writes to it) · `↔` (both).

---

## 1. Systems JPMS keeps as integrations

### Finance & accounting

| System | Direction | Workflows | Role under JPMS |
|---|---|---|---|
| **Xero** | ↔ | 09, 10, 11, 23 | Core. AP posting, AR invoice creation, AP/AR balances feeding cashflow, final VAT and retention postings at settlement. |
| **Dext** | → JPMS (capture) · JPMS → (route) | 09, 13 | Core. Invoice OCR capture; inbox triage routes invoices here automatically. |
| **Brightpay** | JPMS → | 12, 22 | Core. Payroll engine. Fed from JPMS timesheets + starter/leaver events. |
| **Chaser HQ** | JPMS → | 09, 10 | Peripheral. Collections sequence on AR; supplier disputes on AP. |
| **HMRC CIS** | ↔ | 08, 09 | Core. CIS verification on subcontractors; status held against record; gates payment. |
| **Online banking** | (manual) | 09 | Peripheral. Payment execution. No JPMS integration planned in phase one. |
| **HMRC (non-CIS)** | (manual) | 12, 18 | Peripheral. General HMRC interactions. |

### Identity & IT infrastructure

| System | Direction | Workflows | Role under JPMS |
|---|---|---|---|
| **Google sign-in (OAuth)** | → JPMS | Auth | Core. One of three accepted sign-in methods for invited users. |
| **Microsoft sign-in (OAuth)** | → JPMS | Auth | Core. One of three accepted sign-in methods for invited users. |
| **Email + password** | → JPMS | Auth | Core. One of three accepted sign-in methods. JPMS is the identity provider for this path. |
| **M365 Admin / Entra / Intune** | JPMS → | 16, 17 | Core — for **IT provisioning** (workflow 16): group membership, device enrolment. Not the auth backbone for end-user sign-in (see above). |
| **Defender** | (peripheral) | 13, 17 | Peripheral. Spam / phishing handled here, not in JPMS classifier. |
| **1Password** | (peripheral) | 16, 17 | Peripheral. Vault provisioning on starter. |

### Drawings, mark-up & take-off

| System | Direction | Workflows | Role under JPMS |
|---|---|---|---|
| **Bluebeam** | → JPMS | 01, 02 | Peripheral. Take-off quantities import into BoQ; drawing mark-up handoff. |
| **Onetrace** | → JPMS | 06 | TBD. Kept only if it offers something the JPMS site app doesn't. |

### Email & comms

| System | Direction | Workflows | Role under JPMS |
|---|---|---|---|
| **Outlook** | → JPMS (inbox monitor) | 01, 04, 09, 13, 14, 15, 18 | Core. Email remains the inbound channel; JPMS monitors specific inboxes. |
| **Microsoft Teams** | (peripheral) | 16 | Peripheral. Internal comms only. JPMS does not push notifications into Teams in phase one. |
| **Phone system** | → JPMS (caller-ID lookup) | 14 | Peripheral. Provider TBC. |

### External client / supplier portals

| System | Direction | Workflows | Role under JPMS |
|---|---|---|---|
| **Dwellant** | JPMS → | 08, 10 | Peripheral. Client-side portal upload for invoices / RAMS where required. |
| **Vantify** | JPMS → | 08, 10 | Peripheral. As Dwellant. |
| **Insurance broker portals** | (manual) | 18, 19 | TBD. Broker-by-broker decision. |
| **TfL / council portals** | (manual) | 19 | Peripheral. Fine payment / appeals. Manual workflow. |
| **Amazon** | (manual) | 15 | Peripheral. Office consumables ordering. |
| **Paperstone** | (manual) | 15 | Peripheral. Office consumables ordering. |

### Marketing & brand tooling

| System | Direction | Workflows | Role under JPMS |
|---|---|---|---|
| **Canva** | (peripheral) | 20 | Peripheral. Design tooling. JPMS surfaces the consent flag and asset-library link only. |
| **Meta Business** | (peripheral) | 20 | Peripheral. Publishing. |
| **LinkedIn** | (peripheral) | 20 | Peripheral. Publishing. |
| **Marketing scheduling tool** | (peripheral) | 20 | TBD. Provider not selected. |

### Compliance partners (target / new)

| System | Direction | Workflows | Role under JPMS |
|---|---|---|---|
| **Outsourced IT helpdesk vendor** | ↔ (audit reports + tickets) | 16, 17 | TBD — vendor not selected. Target owner of workflow 17. |
| **E-signature provider** | JPMS → | 16 | TBD. DocuSign / Adobe Sign / M365 native — decision pending. |

### Archive only

| System | Direction | Workflows | Role under JPMS |
|---|---|---|---|
| **SharePoint** | — | 21 | Archive. Project folders move into JPMS; SharePoint reduces to corporate and archived documents only. |
| **OneDrive** | — | 21 | Archive. Personal / draft work only. |

---

## 2. Systems JPMS replaces

Each row names a system currently in use and what JPMS does instead. After rollout these systems are either decommissioned or kept read-only for historical data.

| System (today) | Today's role | Replaced by, in JPMS |
|---|---|---|
| **MS Project** | Programme tracking; updated manually by the PM. | The programme module in JPMS (workflow 05) — Gantt-style view tied to BoQ line items, updated automatically from site reporting. |
| **Buildertrend** | Drawing distribution; work-order contracts. | Workflows 01 (drawing register with revisions and notifications) and 03 (work-order generation on award). |
| **Planyard** | Work-order contracting. | Workflow 03 — work orders are generated from the comparison-and-award flow inside JPMS. |
| **Monday.com** | Subcontractor directory, attendance, fleet renewals. | Workflow 08 (subcontractor master record + compliance register), workflow 06 (attendance), workflow 19 (fleet register). |
| **Dashpivot** | Site capture (photos, attendance, snags). | Workflow 06 — site app captures progress, photos, attendance, and snags against BoQ sections directly into JPMS. |
| **RAMsApp** | RAMS drafting. | Workflow 08 — RAMS template engine populated from project + subcontractor data and issued from JPMS. |
| **WhatsApp** (operational use) | Site photos and ad-hoc material requests. | Workflow 06 (site capture) and workflow 15 (procurement requests) — both are first-class capture surfaces in JPMS. |
| **Excel — BoQ workbook** | Standalone priced BoQ per project. | Workflow 02 — BoQ exists as a JPMS record per project, with hierarchical line items, units, rates, and version history. |
| **Excel — programme tracker** | Manual programme tracking. | Workflow 05 — programme module in JPMS. |
| **Excel — valuation sheet** | Monthly valuation per project, built by hand. | Workflow 05 — auto-generated Programme Valuation Report per Claim Period. |
| **Excel — variations / RFI / NoD logs** | Three separate logs per project. | Workflow 04 — unified project change register. |
| **Excel — cashflow tracker** | Weekly rebuild in the FD's hands. | Workflow 11 — live cashflow dashboard fed from Xero, Brightpay, and JPMS commitments. |
| **Excel — subcontractor attendance tracker** | Per-project Excel/calendar. | Workflow 06 — attendance check-in via QR or geofence inside the site app. |
| **Excel — timesheet allocation tracker** | Manual cost-code allocation per period. | Workflow 22 — cost-code allocation enforced inline at timesheet entry with the budget hard-block rule. |
| **Excel — settlement / VAT workbook** | Manual at project close. | Workflow 23 — settlement workspace + auto-generated zero-rated VAT analysis with in-system client agreement. |
| **Word — RAMS template** | Per-project drafted RAMS. | Workflow 08 — RAMS template engine inside JPMS. |
| **Word — neighbour letter template** | Drafted per address. | Workflow 14 — neighbour letter generator from project address and works data. |
| **Word — contract drafts** | Drafted from template by hand. | Workflow 16 — contract auto-drafted from role template, sent for e-signature. |
| **Excel — compliance tracker** | Insurance / certs / accreditation expiry tracking. | Workflow 18 — compliance register with multi-stage reminders. |
| **Excel — systems / architecture map** | Maintained ad-hoc by the FD. | Replaced by this scoping repo + workflow 17. No JPMS feature needed. |

---

## 3. Phase-1 integration shortlist

Integrations that must be live for the phase-1 finance ROI bucket (workflows 09, 10, 11, 13) and the auth foundation:

1. **OAuth sign-in** — Google, Microsoft, email/password (auth foundation).
2. **Xero** (workflows 09, 10, 11).
3. **Dext** (workflows 09, 13).
4. **Brightpay** (workflow 12).
5. **HMRC CIS** (workflows 08, 09).
6. **Outlook inbox monitoring** (workflows 09, 13, 14).
7. **Chaser HQ** (workflows 09, 10).
8. **M365 Admin / Entra / Intune** — for IT provisioning hooks from workflow 16.
9. **Bluebeam** (workflow 02 — needed once project-lifecycle phase 2 begins).

Everything else can land in phase 2 or stay out of scope per the workflow files.
