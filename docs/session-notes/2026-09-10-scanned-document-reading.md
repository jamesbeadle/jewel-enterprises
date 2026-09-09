# 2026-09-10 — Reading scanned PDFs (Jeremy's change request of 9 Sep, 15:28)

Branch `feature/scanned-document-reading` off `main` (`fb7c07e`). Not compiled.

## The ask (`JPMS_scanned_document_reading_change_request_2026-09-09.md`)
A. OCR fallback when text extraction is empty, flagged as OCR with a confidence, cached.
B. The page as an image when OCR is poor or the page is a signature / stamp / mark-up.
C. Refusals that name the format, the reason and the route that works.

## Built
- `AiSourceReader.LoadPdf` keeps a scan (`ScanBytes`) instead of throwing.
- `api/Features/Ai/Scans`: `ScannedPdfPages` (pdfium via Docnet.Core → PNG through the
  hand-rolled `PngEncoder`), `IDocumentOcr` + `AzureVisionOcr` (Image Analysis 4.0 Read,
  `DocumentOcr__Endpoint` / `__ApiKey`) + `NullDocumentOcr`, `ScannedPdfReading.FillAsync`
  (OCR every page, cache by SHA-256 in `DocumentOcrResults`, flag as OCR with confidence).
- `read_source`: `text_source` / `ocr_confidence` on every result and a note when OCR; new
  `as_image` flag; a scan page is shown as a picture when asked, when OCR is missing or below
  0.6, or (no OCR service) for the first page by default. `read_email_attachment` inherits it.
  `find_in_source` explains a scan with no OCR.
- `AiSourceReader.RefusalFor`: .xls / .doc / .msg / .zip wording (item C).
- Migration `20260910100000_AddDocumentOcrResults` (additive).
- Tests: `ScannedPdfReadingTests` (scan kept; page renders to PNG; OCR lines flagged; refusal
  wording).

## Needs from James / Jeremy
- An Azure AI Vision resource (any region, S0 or free F0) and its endpoint + key in the SWA
  app settings as `DocumentOcr__Endpoint` and `DocumentOcr__ApiKey`. Without them scans still
  read — as page pictures the assistant looks at — but item A's flagged text does not exist.
- pdfium's native library ships inside the Docnet package for linux-x64; first deploy is the
  proof it loads on SWA managed functions.

## Also answered: "raise the invoice from the portal"
Not built, and never was: `docs/06-backlog/coverage-audit.md` row 41 scoped "Raise sales
invoices for Jewel BB" OUT — JPMS publishes the approved valuation report; Xero raises the
invoice. The portal's "Raise" is the VI record and snapshot. Building it (ACCREC in Xero with
Sites/Cost Code tracking and the report PDF attached) is a new feature, not a fix.
