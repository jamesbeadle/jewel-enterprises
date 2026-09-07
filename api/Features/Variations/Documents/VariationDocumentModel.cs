
namespace Jewel.JPMS.Api.Features.Variations.Documents;

/// <summary>
/// Everything needed to render a variation order's official document, collated from the SQL source
/// of truth. A flat, self-contained snapshot — the renderer has no database dependency, and the
/// bytes are a pure function of the current record (bar <see cref="GeneratedAt"/>), so regeneration
/// on download, attach and resend is idempotent. Same arrangement as RequestDocumentModel.
/// </summary>
public sealed record VariationDocumentModel(
    string VariationOrderId,
    string DisplayNumber,          // "V31" — the one number the client knows the document by
    string Reference,              // "VOQ-0031" — the persisted quoting reference
    string Title,
    string Description,
    string StatusLabel,
    string ProjectName,
    string ProjectReference,
    string ClientName,
    string CreatedByEmail,
    DateTimeOffset CreatedAt,
    DateTimeOffset? IssuedAt,      // stamped when the order enters Issued; null while quoting
    DateTimeOffset? ApprovedAt,
    string? VariationRef,          // minted at approval; null until then
    decimal? EstimatedValue,       // the quoting-stage estimate
    decimal ApprovedValue,         // the agreed (contract) value; 0 until approved
    bool IsApproved,
    // ---- Narrative sections: all optional, rendered only when present -------------------------
    string? CommercialBasis,
    string? ProgrammeImpact,
    string? Exclusions,
    // The line build-up: the priced lines as they stand on the valuation report once approved,
    // otherwise the staged (agreed / quoted) lines held on the record — the document shows the
    // line detail at every stage. Empty only when nothing has been staged yet.
    IReadOnlyList<VariationDocumentLine> Lines,
    // True when Lines is the staged build-up rather than the report's lines — the sheet says so,
    // because staged lines have not been written anywhere commercial yet.
    bool LinesAreStaged,
    DateTimeOffset GeneratedAt)
{
    /// <summary>The date the document presents as its issue date: the recorded client-issue date
    /// once the order has been issued, otherwise the created date (a quoting-stage render has no
    /// better client-facing date than when the record was raised).</summary>
    public DateTimeOffset IssuedDisplayDate => IssuedAt ?? CreatedAt;

    /// <summary>The build-up's sum — the figure the cost breakdown totals to.</summary>
    public decimal LinesTotal => Lines.Sum(line => line.Amount);

    /// <summary>A safe, human file name for the PDF — "V31 - Staircase Enclosure Ply.pdf",
    /// falling back to the quoting reference before a number exists.</summary>
    public string FileName
    {
        get
        {
            var title = Title.Trim();
            if (title.Length > 60) title = title[..60].TrimEnd();
            var stem = DisplayNumber.Length > 0 ? DisplayNumber : Reference;
            if (title.Length > 0) stem = $"{stem} - {title}";
            foreach (var invalid in Path.GetInvalidFileNameChars())
                stem = stem.Replace(invalid, '-');
            return stem + ".pdf";
        }
    }

    /// <summary>The email subject line used when the document is sent or drafted.</summary>
    public string EmailSubject => $"{DisplayNumber} Variation Order: {Title} — {ProjectName}";
}

/// <summary>One priced row of the cost breakdown (Cost code / Description / Qty / Unit / Rate / Amount).</summary>
public sealed record VariationDocumentLine(
    string CostCode,
    string Description,
    string Unit,
    decimal Quantity,
    decimal Rate,
    decimal Amount);
