-- =====================================================================================
-- Re-state every FROZEN "this period" figure by the one rule (ClaimPeriodBaseline):
--   period increment = the line's cumulative on THIS claim
--                    − the line's cumulative on the claim numbered immediately before
--                      (whatever that claim's status; 0 for claim 1 / a line it never carried).
--
-- Why (2026-09-07, Jeremy): the old rule measured against the latest CONFIRMED claim at the
-- moment the % was entered, so Abbot Road claim 5 — started while claim 4 was only
-- preapproved — printed £40,611.37 "this period" against the £14,425.20 the spreadsheet and
-- the invoice said. The API now DERIVES the figure at every read, so the live working-copy
-- PDF/Excel are right the moment the api deploys, with or without this script. What this
-- script corrects is what was already written down:
--   1. ValuationReportSnapshotLines — the issued statements' frozen this-period column. THIS
--      IS THE ONE THAT MATTERS: a snapshot is what the client was sent, and re-downloading
--      one re-prints the stored figure. (Abbot Road "VI-0006 raise", Ravenswood "VI-0005
--      raise", and any other snapshot whose claim followed an unpaid one.)
--   2. ClaimLines.PeriodIncrement — the convenience copy each claim carries. Nothing renders
--      from it any more, but leaving two rules' worth of figures in one column invites the
--      next reader to trust it.
-- Cumulative, %, and every summary total are untouched: only the split of movement between
-- "previous" and "this period" moves. No schema change. Idempotent — a second run reports 0.
--
-- Run:  sqlcmd -S sql-jpms-prod-54cf9e.database.windows.net -d jpms -U jpmsadmin \
--         -i scripts/2026-09-07-restate-period-increments.sql -b
-- =====================================================================================
SET XACT_ABORT ON;
SET NOCOUNT ON;
BEGIN TRANSACTION;

-- The claim immediately before each claim, by number, on the same project.
IF OBJECT_ID('tempdb..#Previous') IS NOT NULL DROP TABLE #Previous;
SELECT c.ValuationClaimId,
       p.ValuationClaimId AS PreviousClaimId
INTO #Previous
FROM ValuationClaims c
OUTER APPLY (
    SELECT TOP 1 p.ValuationClaimId
    FROM ValuationClaims p
    WHERE p.ProjectId = c.ProjectId AND p.ClaimNumber < c.ClaimNumber
    ORDER BY p.ClaimNumber DESC
) p;

-- 1. The frozen statements.
UPDATE sl
SET PeriodIncrement = sl.CumulativeClaimed - ISNULL(b.CumulativeClaimed, 0)
FROM ValuationReportSnapshotLines sl
JOIN ValuationReportSnapshots s ON s.ValuationReportSnapshotId = sl.ValuationReportSnapshotId
JOIN #Previous v ON v.ValuationClaimId = s.ValuationClaimId
LEFT JOIN ClaimLines b
       ON b.ValuationClaimId = v.PreviousClaimId
      AND b.ValuationLineItemId = sl.SourceValuationLineItemId
WHERE s.ValuationClaimId IS NOT NULL
  AND sl.PeriodIncrement <> sl.CumulativeClaimed - ISNULL(b.CumulativeClaimed, 0);
PRINT CONCAT('Snapshot lines re-stated: ', @@ROWCOUNT);

-- 2. The claims' own copy.
UPDATE cl
SET PeriodIncrement = cl.CumulativeClaimed - ISNULL(b.CumulativeClaimed, 0)
FROM ClaimLines cl
JOIN #Previous v ON v.ValuationClaimId = cl.ValuationClaimId
LEFT JOIN ClaimLines b
       ON b.ValuationClaimId = v.PreviousClaimId
      AND b.ValuationLineItemId = cl.ValuationLineItemId
WHERE cl.PeriodIncrement <> cl.CumulativeClaimed - ISNULL(b.CumulativeClaimed, 0);
PRINT CONCAT('Claim lines re-stated: ', @@ROWCOUNT);

DROP TABLE #Previous;

-- What each live frozen statement now says this period (counting lines only — not Declined 3 / TBC 4).
SELECT p.Reference, s.Label, s.TakenAt,
       SUM(CASE WHEN sl.LineType NOT IN (3, 4) THEN sl.PeriodIncrement ELSE 0 END) AS ThisPeriod,
       s.TotalWorksComplete
FROM ValuationReportSnapshots s
JOIN Projects p ON p.ProjectId = s.ProjectId
JOIN ValuationReportSnapshotLines sl ON sl.ValuationReportSnapshotId = s.ValuationReportSnapshotId
WHERE s.IsSuperseded = 0
GROUP BY p.Reference, s.Label, s.TakenAt, s.TotalWorksComplete
ORDER BY p.Reference, s.TakenAt;

COMMIT TRANSACTION;
