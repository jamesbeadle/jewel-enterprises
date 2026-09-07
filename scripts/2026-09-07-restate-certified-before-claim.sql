-- =====================================================================================
-- Re-state every LOCKED claim's certified / deposit / payment-due figures by the one rule
-- (CertifiedBeforeClaim): "Certified to date" is the gross certification (Amount +
-- DepositCredited) of the Issued/Paid valuation invoices that came BEFORE the claim — invoices
-- drawn against earlier claims, plus historic entries linked to no claim. The claim's own
-- invoice, and any later claim's, never count against it.
--
-- Why (2026-09-07, Jeremy): a locked claim's totals were re-frozen from EVERY issued invoice
-- whenever one was issued, so the moment Abbot Road's VI-0005 was issued Valuation 14 read
-- Certified to date £232,112.02 / Payment due £0.00 where the statement said £207,235.16 /
-- £24,876.86 — and every confirmed claim reads the same way once its invoice is out. The api now
-- applies the rule at every re-freeze; this corrects what is already written on the Preapproved
-- (1) and Confirmed (2) claims. Frozen report snapshots are NOT touched — captured at raise,
-- before the invoice was issued, they already say the right thing.
-- Only rows whose certified figure moves are rewritten (CertifiedToDate, DepositReleased,
-- PaymentDueExVat — the deposit recomputed exactly as ValuationClaimSummary does); works complete,
-- retention and the contract sums are untouched. The rule can only take invoices OUT of a
-- certified figure, so one that would go UP means something else is going on: the script stops
-- with nothing written and the rows listed show what to look at. Idempotent — a second run is 0.
-- Run:  sqlcmd -S sql-jpms-prod-54cf9e.database.windows.net -d jpms -U jpmsadmin \
--         -i scripts/2026-09-07-restate-certified-before-claim.sql -b
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
SELECT c.ValuationClaimId, k.CertifiedToDate, d.DepositReleased,
       c.TotalWorksComplete - c.RetentionHeld + c.RetentionReleased - d.DepositReleased - k.CertifiedToDate AS PaymentDueExVat
INTO #Restated
FROM ValuationClaims c
JOIN #Certified k ON k.ValuationClaimId = c.ValuationClaimId
LEFT JOIN #ContractWorks w ON w.ValuationClaimId = c.ValuationClaimId
CROSS APPLY (SELECT
    CASE WHEN ISNULL(w.NonVariationWorks, 0) * c.DepositPercent / 100 < c.ContractSum * c.DepositPercent / 100
         THEN ISNULL(w.NonVariationWorks, 0) * c.DepositPercent / 100
         ELSE c.ContractSum * c.DepositPercent / 100 END
    - c.DepositReleasedOpening - k.DepositCreditedToDate AS Released) r
CROSS APPLY (SELECT ROUND(CASE WHEN r.Released > 0 THEN r.Released ELSE 0 END, 4) AS DepositReleased) d
WHERE c.CertifiedToDate <> k.CertifiedToDate;

-- What is about to change, claim by claim (nothing listed = nothing to do).
SELECT p.Reference AS Project, c.ClaimNumber, c.Name,
       CASE c.Status WHEN 1 THEN 'Preapproved' WHEN 2 THEN 'Confirmed' END AS Status,
       c.CertifiedToDate AS CertifiedBefore,  r.CertifiedToDate AS CertifiedAfter,
       c.PaymentDueExVat AS PaymentDueBefore, r.PaymentDueExVat AS PaymentDueAfter,
       c.DepositReleased AS DepositBefore,    r.DepositReleased AS DepositAfter
FROM ValuationClaims c
JOIN #Restated r ON r.ValuationClaimId = c.ValuationClaimId
JOIN Projects p ON p.ProjectId = c.ProjectId
ORDER BY p.Reference, c.ClaimNumber;

IF EXISTS (SELECT 1 FROM ValuationClaims c JOIN #Restated r ON r.ValuationClaimId = c.ValuationClaimId
           WHERE r.CertifiedToDate > c.CertifiedToDate)
    THROW 50001, 'A claim''s certified figure would go UP, which this rule cannot cause — nothing written; review the rows above.', 1;

UPDATE c
SET CertifiedToDate = r.CertifiedToDate,
    DepositReleased = r.DepositReleased,
    PaymentDueExVat = r.PaymentDueExVat
FROM ValuationClaims c
JOIN #Restated r ON r.ValuationClaimId = c.ValuationClaimId;
PRINT CONCAT('Locked claims re-stated: ', @@ROWCOUNT);

DROP TABLE #Restated;
DROP TABLE #ContractWorks;
DROP TABLE #Certified;

COMMIT TRANSACTION;
