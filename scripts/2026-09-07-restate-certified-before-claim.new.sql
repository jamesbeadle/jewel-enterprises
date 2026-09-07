-- =====================================================================================
-- Re-state every LOCKED claim's certified / deposit / payment-due figures by the one rule
-- (CertifiedBeforeClaim): "Certified to date" is the gross certification (Amount +
-- DepositCredited) of the Issued/Paid valuation invoices that came BEFORE the claim — invoices
-- drawn against earlier claims, plus historic entries linked to no claim. The claim's own
-- invoice, and any later claim's, never count against it.
-- Why (2026-09-07, Jeremy): a locked claim's totals were re-frozen from EVERY issued invoice
-- whenever one was issued, so the moment Abbot Road's VI-0005 was issued Valuation 14 read
-- Certified to date £232,112.02 / Payment due £0.00 where the statement said £207,235.16 /
-- £24,876.86. The api now applies the rule at every re-freeze; this corrects what is already
-- written on the Preapproved (1) and Confirmed (2) claims. Frozen snapshots are NOT touched.
-- The rule can only take invoices OUT of a certified figure, so only claims whose figure goes
-- DOWN are written (CertifiedToDate, DepositReleased, PaymentDueExVat — the deposit recomputed
-- exactly as ValuationClaimSummary does). A claim whose figure would go UP has a stale stored
-- total for some other reason (confirmed before an earlier invoice was entered, seeded, or an
-- unlinked invoice that belongs to a later period): it is listed with the invoices behind it and
-- left alone for review. Idempotent — a second run writes 0.
-- Run:  sqlcmd -S sql-jpms-prod-54cf9e.database.windows.net -d jpms -U jpmsadmin \
--         -i scripts/2026-09-07-restate-certified-before-claim.sql -b -o restate-certified-before-claim.log
-- =====================================================================================
SET XACT_ABORT ON;
SET NOCOUNT ON;
BEGIN TRANSACTION;

-- 1. Certification BEFORE each locked claim: Issued (1) / Paid (2) invoices on the project linked to
--    an earlier claim, or to none. The condition sits in the JOIN so a claim with nothing before it
--    still gets a row (certified 0).
IF OBJECT_ID('tempdb..#Certified') IS NOT NULL DROP TABLE #Certified;
SELECT c.ValuationClaimId,
       ISNULL(SUM(inv.Amount + inv.DepositCredited), 0) AS CertifiedToDate,
       ISNULL(SUM(inv.DepositCredited), 0)              AS DepositCreditedToDate
INTO #Certified
FROM ValuationClaims c
LEFT JOIN (
    SELECT i.ProjectId, i.Amount, i.DepositCredited, linked.ClaimNumber
    FROM ValuationInvoices i
    LEFT JOIN ValuationClaims linked ON linked.ValuationClaimId = i.ValuationClaimId
    WHERE i.Status IN (1, 2)
) inv ON inv.ProjectId = c.ProjectId
     AND (inv.ClaimNumber IS NULL OR inv.ClaimNumber < c.ClaimNumber)
WHERE c.Status IN (1, 2)
GROUP BY c.ValuationClaimId;

-- 2. Contract-side works complete per claim (element type 3 = variation excluded; declined 3 /
--    TBC 4 lines never count) — the base the deposit releases against.
IF OBJECT_ID('tempdb..#ContractWorks') IS NOT NULL DROP TABLE #ContractWorks;
SELECT cl.ValuationClaimId, SUM(cl.CumulativeClaimed) AS NonVariationWorks
INTO #ContractWorks
FROM ClaimLines cl
JOIN ValuationLineItems li ON li.ValuationLineItemId = cl.ValuationLineItemId
WHERE li.ElementType <> 3 AND li.LineType NOT IN (3, 4)
GROUP BY cl.ValuationClaimId;

-- 3. What each locked claim should say. DepositReleased = min(deposit % of contract-side works,
--    deposit received) − opening balance − credits on the invoices that count, floored at 0.
IF OBJECT_ID('tempdb..#Restated') IS NOT NULL DROP TABLE #Restated;
SELECT c.ValuationClaimId, p.Reference AS Project, c.ClaimNumber, c.Name, c.ProjectId,
       c.CertifiedToDate AS CertifiedBefore, k.CertifiedToDate, c.PaymentDueExVat AS PaymentDueBefore,
       d.DepositReleased,
       c.TotalWorksComplete - c.RetentionHeld + c.RetentionReleased - d.DepositReleased - k.CertifiedToDate AS PaymentDueExVat
INTO #Restated
FROM ValuationClaims c
JOIN Projects p ON p.ProjectId = c.ProjectId
JOIN #Certified k ON k.ValuationClaimId = c.ValuationClaimId
LEFT JOIN #ContractWorks w ON w.ValuationClaimId = c.ValuationClaimId
CROSS APPLY (SELECT
    CASE WHEN ISNULL(w.NonVariationWorks, 0) * c.DepositPercent / 100 < c.ContractSum * c.DepositPercent / 100
         THEN ISNULL(w.NonVariationWorks, 0) * c.DepositPercent / 100
         ELSE c.ContractSum * c.DepositPercent / 100 END
    - c.DepositReleasedOpening - k.DepositCreditedToDate AS Released) r
CROSS APPLY (SELECT ROUND(CASE WHEN r.Released > 0 THEN r.Released ELSE 0 END, 4) AS DepositReleased) d
WHERE c.CertifiedToDate <> k.CertifiedToDate;

PRINT '--- Written (certified goes DOWN: own or later invoice taken out) ---';
SELECT Project, ClaimNumber, Name, CertifiedBefore, CertifiedToDate AS CertifiedAfter,
       PaymentDueBefore, PaymentDueExVat AS PaymentDueAfter
FROM #Restated WHERE CertifiedToDate < CertifiedBefore ORDER BY Project, ClaimNumber;

UPDATE c
SET CertifiedToDate = r.CertifiedToDate, DepositReleased = r.DepositReleased, PaymentDueExVat = r.PaymentDueExVat
FROM ValuationClaims c
JOIN #Restated r ON r.ValuationClaimId = c.ValuationClaimId
WHERE r.CertifiedToDate < r.CertifiedBefore;
PRINT CONCAT('Locked claims re-stated: ', @@ROWCOUNT);

PRINT '--- Left for review (certified would go UP — stale stored total, not this rule) ---';
SELECT r.Project, r.ClaimNumber, r.Name, r.CertifiedBefore, r.CertifiedToDate AS CertifiedWouldBe,
       i.Reference AS Invoice, i.Status AS InvoiceStatus, CONVERT(date, i.PeriodMonth) AS Period,
       i.Amount + i.DepositCredited AS Gross, linked.ClaimNumber AS LinkedClaim, i.IsManual
FROM #Restated r
JOIN ValuationInvoices i ON i.ProjectId = r.ProjectId AND i.Status IN (1, 2)
LEFT JOIN ValuationClaims linked ON linked.ValuationClaimId = i.ValuationClaimId
WHERE r.CertifiedToDate > r.CertifiedBefore
ORDER BY r.Project, r.ClaimNumber, i.Number;

DROP TABLE #Restated;
DROP TABLE #ContractWorks;
DROP TABLE #Certified;

COMMIT TRANSACTION;
