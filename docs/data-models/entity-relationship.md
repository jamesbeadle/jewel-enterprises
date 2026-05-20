# Entity Relationship Diagram

First-cut ERD for the JPMS platform, derived from the entities surfaced by the twenty-one workflows in the JBB audit. Schemas are not yet written — this diagram exists so the workflows and journeys can reference entities by name and so the eventual schemas have a shape to grow into.


**Status:** Draft — refined as each workflow moves Draft → In Review and each schema gets written.

---

## Diagram

The ERD is split into five sub-diagrams so each renders cleanly. They share entities (especially `Project`, `Subcontractor`, `Person`, `Cost Code`) — the splits are for legibility, not data isolation.

### 1 · Project lifecycle (workflows 01–07)

```mermaid
erDiagram
    ORGANISATION ||--o{ PROJECT : "delivers"
    ARCHITECT ||--o{ TENDER : "issues"
    TENDER ||--|| PROJECT : "becomes"
    PROJECT ||--o{ DRAWING : "has"
    DRAWING ||--o{ DRAWING_REVISION : "versions"
    PROJECT ||--|| BOQ : "has"
    BOQ ||--o{ BOQ_LINE_ITEM : "contains"
    BOQ_LINE_ITEM }o--|| RATE : "priced from"
    RATE_LIBRARY ||--o{ RATE : "holds"
    PROJECT ||--o{ BID_PACKAGE : "issues"
    BID_PACKAGE ||--o{ QUOTE : "receives"
    QUOTE ||--o| WORK_ORDER : "becomes (on award)"
    WORK_ORDER }o--|| SUBCONTRACTOR : "assigned to"
    WORK_ORDER ||--o{ VARIATION : "changed by"
    WORK_ORDER ||--o{ RFI : "raises"
    WORK_ORDER ||--o{ NOD : "raises"
    PROJECT ||--o{ PROGRAMME_TASK : "scheduled as"
    PROGRAMME_TASK }o--o{ BOQ_LINE_ITEM : "tracks progress on"
    PROJECT ||--o{ VALUATION : "produces"
    PROJECT ||--o{ CLAIM_PERIOD : "is divided into"
    VALUATION }o--|| CLAIM_PERIOD : "for"
    VALUATION }o--o{ VARIATION : "rolls up"
    PROJECT ||--o{ SITE_REPORT : "captures"
    PROJECT ||--o{ DEFECT : "carries"
    DEFECT }o--|| SUBCONTRACTOR : "assigned to"
    VARIATION ||--o| BID_PACKAGE : "may trigger"

    PROJECT {
        string id PK
        string organisationId FK
        string architectId FK
        string status
        date startDate
        date practicalCompletion
    }
    DRAWING_REVISION {
        string id PK
        string drawingId FK
        string revision
        datetime issuedAt
        string supersededBy
    }
    BOQ_LINE_ITEM {
        string id PK
        string boqId FK
        string code
        string discipline
        decimal qty
        string unit
        decimal rate
        decimal value
    }
    WORK_ORDER {
        string id PK
        string projectId FK
        string subcontractorId FK
        decimal value
        string status
    }
```

### 2 · Subcontractor & compliance (workflow 08)

```mermaid
erDiagram
    SUBCONTRACTOR ||--o{ COMPLIANCE_DOCUMENT : "holds"
    COMPLIANCE_DOCUMENT ||--|| RENEWAL_EVENT : "expires via"
    SUBCONTRACTOR ||--o{ RAMS : "produces"
    RAMS }o--|| PROJECT : "specific to"
    SUBCONTRACTOR ||--o| CIS_STATUS : "verified by"
    SUBCONTRACTOR ||--o{ WORK_ORDER : "executes"

    SUBCONTRACTOR {
        string id PK
        string companyName
        string tradeTags
        string cisStatus
    }
    COMPLIANCE_DOCUMENT {
        string id PK
        string subcontractorId FK
        string type
        date issuedAt
        date expiresAt
        string fileRef
    }
```

### 3 · Finance (workflows 09–13)

```mermaid
erDiagram
    SUPPLIER ||--o{ SUPPLIER_INVOICE : "issues"
    SUBCONTRACTOR ||--o{ SUPPLIER_INVOICE : "issues"
    SUPPLIER_INVOICE }o--|| WORK_ORDER : "matched to"
    SUPPLIER_INVOICE }o--|| PROJECT : "costed to"
    PAYMENT_RUN ||--o{ SUPPLIER_INVOICE : "pays"
    PAYMENT_RUN }o--|| FINANCE_DIRECTOR : "approved by"

    PROJECT ||--o{ SALES_INVOICE : "raises"
    SALES_INVOICE }o--|| VALUATION : "derived from"
    SALES_INVOICE }o--|| ARCHITECT : "billed to"

    CASHFLOW_FORECAST }o--o{ SUPPLIER_INVOICE : "forecasts outflow"
    CASHFLOW_FORECAST }o--o{ SALES_INVOICE : "forecasts inflow"
    CASHFLOW_FORECAST }o--o{ PAYMENT_RUN : "incorporates"
    CASHFLOW_FORECAST }o--o{ TIMESHEET : "incorporates payroll commitment"

    TIMESHEET }o--|| PERSON : "submitted by"
    TIMESHEET }o--|| PROJECT : "costed to"

    INBOX_MESSAGE ||--|| INBOX_CLASSIFICATION : "tagged with"
    INBOX_MESSAGE }o--o| SUPPLIER_INVOICE : "yields"

    SUPPLIER_INVOICE {
        string id PK
        string supplierId FK
        string workOrderId FK
        decimal gross
        decimal cis
        string status
    }
    SALES_INVOICE {
        string id PK
        string projectId FK
        string valuationId FK
        decimal amount
        string status
    }
    CASHFLOW_FORECAST {
        string id PK
        date asOf
        int horizonWeeks
    }
```

### 4 · People, ops & support (workflows 14–21)

```mermaid
erDiagram
    PERSON ||--o{ SYSTEM_ACCOUNT : "owns"
    PERSON ||--o| DRIVER_ASSIGNMENT : "drives"
    VEHICLE ||--o{ DRIVER_ASSIGNMENT : "assigned to"
    VEHICLE ||--o{ RENEWAL_EVENT : "renews via"
    VEHICLE ||--o{ FINE : "incurs"

    ONBOARDING_EVENT }o--|| PERSON : "for"
    ONBOARDING_EVENT ||--o{ SYSTEM_ACCOUNT : "provisions"
    ONBOARDING_EVENT ||--|| CONTRACT : "issues"

    ORGANISATION ||--o{ COMPLIANCE_POLICY : "holds"
    COMPLIANCE_POLICY ||--o{ RENEWAL_EVENT : "renews via"
    COMPLIANCE_POLICY ||--o{ ACCREDITATION : "evidences"

    PROJECT ||--o{ PROCUREMENT_REQUEST : "needs"
    PROCUREMENT_REQUEST }o--|| SUPPLIER : "fulfilled by"

    PROJECT ||--o{ COMMUNICATION_LOG : "logs"
    COMMUNICATION_LOG }o--|| CONTACT : "with"

    PROJECT ||--o{ CONTENT_ITEM : "eligible for"
    CONTENT_ITEM ||--o| CONSENT_RECORD : "gated by"
    CONTENT_ITEM }o--|| BRAND_ASSET : "uses"

    PROJECT ||--o{ DOCUMENT : "stores"
    DOCUMENT }o--|| FOLDER_TEMPLATE : "filed under"

    PERSON {
        string id PK
        string name
        string role
        string employmentType
        date startDate
        date leaveDate
    }
    VEHICLE {
        string id PK
        string reg
        string make
        string model
    }
    COMPLIANCE_POLICY {
        string id PK
        string organisationId FK
        string type
        date renewalDate
    }
```

### 5 · Timesheets & project settlement (workflows 22, 23)

```mermaid
erDiagram
    PROJECT ||--o{ COST_CODE : "has"
    COST_CODE ||--|| COST_CODE_BUDGET : "has"
    COST_CODE ||--o{ COST_CODE_ALLOCATION : "receives"
    WORK_ORDER }o--|| COST_CODE : "draws against"

    PERSON ||--o{ TIMESHEET : "submits"
    SUBCONTRACTOR ||--o{ TIMESHEET : "submits (day-rate)"
    TIMESHEET ||--o{ COST_CODE_ALLOCATION : "allocated via"
    TIMESHEET_APPROVAL ||--o{ TIMESHEET : "approves batch of"

    PROJECT ||--o| PRACTICAL_COMPLETION : "reaches"
    PRACTICAL_COMPLETION ||--|| SETTLEMENT_RECORD : "triggers"
    SETTLEMENT_RECORD ||--|| VAT_ANALYSIS : "includes"
    VAT_ANALYSIS }o--|| ARCHITECT : "agreed by"
    SETTLEMENT_RECORD ||--o| PAYMENT_RUN : "triggers retention release"

    COST_CODE {
        string id PK
        string projectId FK
        string clientCode
        string discipline
    }
    COST_CODE_BUDGET {
        string costCodeId PK
        decimal allocated
        decimal committed
        decimal spent
        decimal remaining
    }
    TIMESHEET {
        string id PK
        string projectId FK
        string personId FK
        date date
        decimal hours
        string status
    }
    SETTLEMENT_RECORD {
        string id PK
        string projectId FK
        date pcDate
        date settledDate
        string status
    }
    VAT_ANALYSIS {
        string id PK
        string settlementId FK
        decimal zeroRatedValue
        decimal standardRatedValue
        string clientAgreement
        datetime agreedAt
    }
```

---

## Entity index

Sourced workflows shown in brackets. Schemas remain `to be created`.

### Project lifecycle

| Entity | First surfaced in | Notes |
|---|---|---|
| `Organisation` | All | The JBB / Jewel entity (BB, PS, PFP). Multi-entity flag on most other records. |
| `Project` | All | Central organising concept. |
| `Architect` | 2026-05-18 | External; issues tenders. |
| `Tender` | 2026-05-18 | Becomes a project on award. |
| `Cost Code` | 2026-05-18 | Architect's reference; threads through the project. |
| `Drawing` | 01, 02 | A drawing per scope. |
| `Drawing Revision` | 01 | Versioned with supersede logic. |
| `BoQ` | 02 | One per project (replaces standalone Excel). |
| `BoQ Line Item` | 02, 04, 05 | Discrete unit of priced and tracked work. |
| `Rate` | 02 | Held in the rate library. |
| `Rate Library` | 02 | Versioned, with supplier links. |
| `Bid Package` | 03 | Issued to subcontractors per trade. |
| `Quote` | 03 | Returned by subcontractors into JPMS. |
| `Work Order` | 03, 07, 09 | The contract artefact; matching key for AP. |
| `Variation` | 04, 05 | Updates BoQ line items, rolls up to valuation. |
| `RFI` | 04 | Question to architect, response attaches automatically. |
| `NoD` (Notice of Delay) | 04 | Formal delay notice. |
| `Programme Task` | 05 | Tied to BoQ line items. |
| `Valuation` | 05, 10 | Monthly; feeds sales invoice draft. |
| `Site Report` | 06 | Daily capture from site app. |
| `Defect` | 07 | Snag register per project. |
| `Claim Period` | 05 | Contractual cycle for valuation reporting (typically monthly, per-contract overridable). |

### Timesheets, cost codes & settlement

| Entity | First surfaced in | Notes |
|---|---|---|
| `Cost Code` | 2026-05-18 (revisited 22) | Architect's client-facing code; threads through project / WO / timesheet / valuation. |
| `Cost Code Budget` | 22 | Per-cost-code budget (allocated / committed / spent / remaining). The arbiter of the workflow 22 hard-block rule. |
| `Cost Code Allocation` | 22 | Each timesheet entry's allocation against a cost code. |
| `Timesheet Approval` | 22 | Weekly batch approval record. |
| `Practical Completion` | 07, 23 | The PC event on a project. Triggers workflow 07 defects and workflow 23 settlement in parallel. |
| `Settlement Record` | 23 | Final audit-grade summary of project commercial settlement. Triggers retention release. |
| `VAT Analysis` | 23 | Zero-rated vs standard-rated breakdown; carries client agreement. |

### Subcontractor & compliance

| Entity | First surfaced in | Notes |
|---|---|---|
| `Subcontractor` | 03, 08 | Master record with trade tags. |
| `Compliance Document` | 08 | Insurance, certs, tickets — with expiry. |
| `Renewal Event` | 08, 18, 19 | Generic — used by compliance, fleet, insurance. |
| `RAMS` | 08 | Project-specific risk & method statement. |
| `CIS Status` | 08, 09 | Verified against HMRC. |

### Finance

| Entity | First surfaced in | Notes |
|---|---|---|
| `Supplier` | 09, 15 | Materials suppliers. |
| `Supplier Invoice` | 09 | Captured via Dext, matched to WO. |
| `Sales Invoice` | 10 | Drafted from valuation/milestone in Xero. |
| `Payment Run` | 09 | Weekly approval bundle. |
| `Cashflow Forecast` | 11, 2026-05-18 | Primary pain point; consumer of nearly all finance data. |
| `Timesheet` | 06, 12, 2026-05-18 | Site app + office check-in. |
| `Inbox Message` | 01, 13, 14 | Generic inbound email/comm record. |
| `Inbox Classification` | 13 | Tag assigned by AI classifier. |
| `Statement` | 09, 13 | Supplier statement for reconciliation. |

### People, ops & support

| Entity | First surfaced in | Notes |
|---|---|---|
| `Person` | 12, 16, 19 | Internal staff. |
| `Role` | 16 | Maps to permission matrix. |
| `Contract` | 16 | Generated from role template. |
| `System Account` | 16, 17 | Cross-system audit. |
| `Onboarding Event` | 16 | Triggers the full orchestration. |
| `Procurement Request` | 15 | Project or office. |
| `Communication Log` | 14 | Call/email log against project + contact. |
| `Contact` | 14 | Lightweight CRM. |
| `Compliance Policy` | 18 | Insurance, accreditation. |
| `Accreditation` | 18 | Tender evidence asset. |
| `Vehicle` | 19 | Fleet register. |
| `Driver Assignment` | 19 | Person ↔ vehicle. |
| `Fine` | 19 | TfL / council. |
| `Content Item` | 20 | Marketing post or asset draft. |
| `Consent Record` | 20 | Client consent to publish project content. |
| `Brand Asset` | 20 | Version-controlled. |
| `Document` | 21 | Generic project/corporate doc. |
| `Folder Template` | 21 | Auto-creates project folders. |

---

## Open questions on the model

- [ ] Multi-entity (BB / PS / PFP) modelling — separate `Organisation` records with cross-charge flag on transactions? Or a single tenant with entity tag?
- [ ] Retention money — first-class entity or attribute on `Work Order`?
- [ ] CIS — does `CIS Status` belong on `Subcontractor` only, or also at the `Supplier Invoice` level for audit?
- [ ] Cost Code — independent entity, or attribute on `Tender` / `BoQ Line Item`?
- [ ] External party model — is `Architect` a special case of `Contact`, or its own entity?
- [ ] Cashflow forecast persistence — snapshot table for each weekly run, or always derived?

---

## Process for refining

1. When a workflow moves Draft → In Review, write the JSON Schemas for the entities it touches in `/docs/data-models/{entity}.schema.json`.
2. Update the ERD here as relationships are confirmed in role-play.
3. Update root [`README.md`](../../README.md#7-business-entities) Section 7 entities table to point at the new schema.
4. When all four sub-diagrams are confirmed, root README Section 4.4 "Entity-relationship diagram drawn" can be ticked as Confirmed.
