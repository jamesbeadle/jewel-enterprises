---
name: jpms-document-filing
description: "How documents move from email to their registers — Document Triage and the project Documents register's conventions (the register was called Drawings until 2026-09-03 — it holds drawings, party-wall awards, building-control letters and reports). Load before filing attachments to a project's Documents, Payment Certificates or subcontractor compliance, registering documents, or reasoning about revisions. Encodes revision inheritance, folder-first filing, the current-revision rule, date-only UTC certificate dates, and discard-never-delete."
---

# JPMS — Document filing

## Document Triage

- Every item is ONE email attachment copy, waiting to be filed to exactly one home: a project document
  (file_document_to_project_documents — the Documents register, drawings included), a payment certificate (file_document_as_payment_certificate), or a
  subcontractor's compliance documents (file_document_to_subcontractor).
- Filing as a document REVISION inherits code and title from the target document — the item's own
  name may be junk; the register's identity wins. Filing as a NEW document resolves (or creates)
  its folder FIRST, then files into it.
- Certificate dates and compliance expiry dates are date-only, pinned to UTC — the stored day
  must never drift with anyone's timezone. Send plain yyyy-MM-dd.
- Discard is restorable and filed rows keep their where-it-went history — nothing in this queue
  is ever deleted. When unsure where something files, leave it Pending and ask; a wrongly filed
  certificate misstates what the client certified.

## The project Documents register (formerly Drawings)

- A document's CURRENT revision is its approved one, else its newest — trust the register's
  hasApprovedRevision flag, not label text.
- Registering a document (metadata — register_document) and adding a revision (the file) are separate acts; revision
  files only arrive by upload or from Document Triage, never invented.
- Approval is evidential — it records who approved and supersedes the previous approved revision.
  Never mark approval on anyone's behalf without their explicit say-so in this conversation.
- Deleting a REVISION and deleting the DOCUMENT are different destructive acts; both need the
  user's confirmed intent, named by document code.
- "Extract data" runs on ANY PDF revision in the register — a report or an award as much as a
  drawing; it is never automatic on upload. Since 2026-09-07 it is the portal's OWN read of the
  PDF: title block, revision table, a scale the sheet PROVES (figured dimensions matched to drawn
  lines), every dimension with the line it measures, notes/callouts with positions, closed
  shapes with real sizes — all in real-world millimetres. Read it with get_document_extraction
  (revisionId, or drawingId for the newest extracted revision). For a take-off use the FIGURED
  dimension, never a scaled distance; check scaleVerified first; a scanned sheet has no text
  layer and yields nothing measurable — say so. Bluebeam/Revu markups are an optional extra that
  only exist when someone measured in Revu AND the connection is set up — never a prerequisite.
- The tool/action parameters still say drawingId / drawingFolderId / drawingCode — the
  register's old name — and list_documents returns rows under `drawings`. Same records, new label.
