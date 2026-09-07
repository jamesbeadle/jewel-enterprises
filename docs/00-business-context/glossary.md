# Glossary

Construction and Jewel-Enterprises-specific terms used across this scoping repository. Add to this file whenever a discovery session surfaces a new term. Definitions should come from the people who use the term — capture in a meeting note first, then add here.

---

**Cashflow Forecast**
A projection of incoming and outgoing cash across the business, produced by the Accountant for the Managing Director. Depends on real-time-ish completion data per project and the timing of valuation invoices.

**Cost Code**
A reference defined by a client architect, used to categorise work for the architect's own billing and reporting. Cost codes must be referenceable from line items, completion records, and valuation-invoice documentation tied to that architect's tender.

**Drawing**
A construction drawing attached to a tender (CAD export, PDF, etc.) defining what needs to be built.

**Line Item / Tender Line Item**
A discrete unit of priced work within a tender (e.g. "install kitchen worktop"). Used for both pricing and completion tracking. Updated by VOs when scope changes.

**MD (Managing Director)**
The business owner — executive decisions across all projects.

**Programme / Project Programme / Programme of Work**
The plan of work to be delivered against a tender, tracked against its baseline. The platform's central organising concept. Always the UK spelling "Programme" — never "Schedule" or "Program" — in UI copy, code identifiers, and docs. Working name for the initial software: **JPMS**.

**Project**
A unit of work delivered for an architect, originating from an accepted tender.

**QS (Quantity Surveyor)**
The role responsible for pricing tenders into line items and capturing site measurements.

**RFI (Request for Information)**
A formal question raised by a Subcontractor (or other party on site) seeking clarification on scope, specification, or drawing when work cannot proceed without an answer. RFI resolutions often produce VOs.

**Subcontractor**
A field worker delivering work against tender line items. External to Jewel Bespoke Build.

**Tender**
A package of drawings and specifications sent by an Architect to Jewel Bespoke Build, defining the work to be delivered. Tenders are priced by the QS into line items.

**Timesheet**
A record of time spent on a project by a Subcontractor, used for cost tracking and payment.

**Valuation Invoice** *(formerly "Cash Call")*
The canonical term for an amount of money Jewel has claimed for the client to pay, raised against the current valuation based on the percentage of work completed. Lifecycle (one move per material stage, driven from the claim card): Raised (accounts raise it once the project team has valued & locked the claim — a draft; the report snapshot freezes; nothing is sent) → Submitted (the claim recorded as sent to the architect/client for approval — the portal emails nothing) → Approved → Issued (counts toward certified/invoiced to date) → Paid (rolls into the project's paid total). Rejected returns an invoice to draft for amendment; projects with no formal approval loop issue directly. Valuation-invoice accuracy depends on accurate line-item completion data — this is the central data-flow concern of the initial platform. Use "valuation invoice" everywhere; "cash call" survives only in historical meeting notes and old migration files.

**VO (Variation Order)**
A formal update to a tender's line items, typically arising from an RFI or scope change. Once approved, line items are added, changed, or removed. Completed VO work is billable to the client via a valuation invoice.
