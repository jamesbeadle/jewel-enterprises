using Jewel.JPMS.Models;

namespace Jewel.JPMS.Commercial;

// The valuation report's summary footer as one value, computed identically for every
// consumer (the Valuation tab footer and the Cashflow tab's retention figures) so the
// two tabs can never disagree. Mirrors the By France workbook summary block.
//
// A Draft claim is computed live from its per-line % complete (certified tracks the
// issued/paid valuation invoices); a locked claim (Preapproved/Confirmed) reads its
// frozen totals; no claim means nothing is being claimed, but certified still reads.
//
// Certification runs GROSS of the deposit: CertifiedToDate is the sum of gross
// certificates (each invoice's cash amount plus the deposit credit embedded in it), the
// way a QS certifies. The deposit block (trailing, defaulted so pre-deposit
// constructions still compile): DepositReceived is always computed live (deposit % of
// the contract sum — the cash the client paid up front). DepositReleased is the credit
// STILL TO BE TAKEN on this claim's next invoice: the release earned against
// contract-side works to date, less the opening balance settled before tracking
// (DepositReleasedOpening), less credits already embedded in issued/paid invoices — so
// it returns to zero once the period's invoice is issued. PaymentDueExVat is the cash
// the client is asked to pay; PaymentDueBeforeDepositExVat is the figure above the
// deduction line — the workbook's "Total Payment Due Excluding VAT".
public sealed record ValuationSummaryFigures(
    decimal ContractSum,
    decimal NetVariations,
    decimal RevisedContractSum,
    decimal TotalWorksComplete,
    decimal RetentionPercent,
    decimal RetentionHeld,
    decimal RetentionReleasePercent,
    decimal RetentionReleased,
    decimal CertifiedToDate,
    decimal PaymentDueExVat,
    decimal DepositPercent = 0m,
    decimal DepositReceived = 0m,
    decimal DepositReleased = 0m,
    decimal DepositReleasedOpening = 0m,
    decimal DepositCreditedToDate = 0m)
{
    // Retention currently withheld by the client — held less what has been released.
    public decimal RetentionOutstanding => RetentionHeld - RetentionReleased;

    // Everything handed back so far: credits taken on issued/paid invoices, releases
    // settled before tracking began, and the credit pending on the next invoice.
    public decimal DepositReleasedToDate => DepositReleased + DepositReleasedOpening + DepositCreditedToDate;

    // Deposit still to be released back to the client over the remaining works.
    public decimal DepositOutstanding => DepositReceived - DepositReleasedToDate;

    // The workbook's "Total Payment Due Excluding VAT" — before the deposit comes off.
    public decimal PaymentDueBeforeDepositExVat => PaymentDueExVat + DepositReleased;

    public static ValuationSummaryFigures For(
        IReadOnlyList<ValuationLineItem> lines,
        IReadOnlyList<ClaimLine> entries,
        ValuationClaim? claim,
        decimal certifiedToDate,
        decimal depositCreditedToDate = 0m)
    {
        var contractSum = ValuationCalculations.ContractSum(lines);
        var netVariations = ValuationCalculations.NetVariations(lines);
        var revisedContractSum = ValuationCalculations.RevisedContractSum(contractSum, netVariations);

        var retentionPercent = claim?.RetentionPercent ?? 0m;
        var retentionReleasePercent = claim?.RetentionReleasePercent ?? 0m;
        var depositPercent = claim?.DepositPercent ?? 0m;
        var depositOpening = claim?.DepositReleasedOpening ?? 0m;
        var depositReceived = ValuationCalculations.DepositReceived(contractSum, depositPercent);

        if (claim is { Status: ValuationClaimStatus.Draft })
        {
            // Live preview for an editable draft. Certified to date tracks the valuation
            // invoices issued so far, so adding/issuing an invoice updates the payment due.
            var totalWorksComplete = lines
                .Where(line => line.CountsTowardTotals)
                .Sum(line => ValuationCalculations.CumulativeClaimed(PercentFor(entries, line), line.LineAmount));
            var retentionHeld = ValuationCalculations.RetentionHeld(totalWorksComplete, retentionPercent);
            // Retention release adds back into the payment due ONLY once the claim carries a
            // release % — which StartValuationClaim/SetProjectRetention stamp solely when the
            // claim date has reached practical completion. Pre-completion claims carry 0% and
            // show the workbook's £- here; post-completion (the Albany Mews final-account
            // model, and PLG's interim certificates) the client is due works less net
            // retention: held % less release % of works complete. Mirrors ValuationClaimSummary.
            var retentionReleased = ValuationCalculations.RetentionReleased(totalWorksComplete, retentionReleasePercent);
            // The deposit, by contrast, releases automatically with the works: deposit % of
            // the contract-side works complete (variations excluded), capped at what was
            // paid, less the opening balance settled before tracking, less credits already
            // taken on issued/paid invoices — leaving what the NEXT invoice should credit.
            var nonVariationWorksComplete = lines
                .Where(line => line.ElementType != ValuationElementType.Variation && line.CountsTowardTotals)
                .Sum(line => ValuationCalculations.CumulativeClaimed(PercentFor(entries, line), line.LineAmount));
            var depositReleasedToDate = ValuationCalculations.DepositReleased(
                nonVariationWorksComplete, depositPercent, depositReceived);
            var depositDeduction = ValuationCalculations.DepositDeduction(
                depositReleasedToDate, depositOpening, depositCreditedToDate);
            return new(
                contractSum, netVariations, revisedContractSum, totalWorksComplete,
                retentionPercent, retentionHeld, retentionReleasePercent, retentionReleased,
                certifiedToDate,
                ValuationCalculations.PaymentDueExVat(
                    totalWorksComplete, retentionHeld, retentionReleased, depositDeduction, certifiedToDate),
                depositPercent, depositReceived, depositDeduction, depositOpening, depositCreditedToDate);
        }

        if (claim is not null)
        {
            // Frozen totals from the locked claim. Its CertifiedToDate is the certification
            // BEFORE the claim — earlier claims' issued invoices, never its own — so a claim
            // reads the same after its invoice is issued and paid as the statement said
            // (the api's CertifiedBeforeClaim rule).
            return new(
                contractSum, netVariations, revisedContractSum, claim.TotalWorksComplete,
                retentionPercent, claim.RetentionHeld, retentionReleasePercent, claim.RetentionReleased,
                claim.CertifiedToDate, claim.PaymentDueExVat,
                depositPercent, depositReceived, claim.DepositReleased, depositOpening, depositCreditedToDate);
        }

        // No claim: nothing is being claimed, but what's been certified still reads.
        return new(
            contractSum, netVariations, revisedContractSum, 0m,
            retentionPercent, 0m, retentionReleasePercent, 0m,
            certifiedToDate, 0m,
            depositPercent, depositReceived, 0m, depositOpening, depositCreditedToDate);
    }

    private static decimal PercentFor(IReadOnlyList<ClaimLine> entries, ValuationLineItem line) =>
        entries.FirstOrDefault(e => e.ValuationLineItemId == line.ValuationLineItemId)?.PercentComplete ?? 0m;
}
