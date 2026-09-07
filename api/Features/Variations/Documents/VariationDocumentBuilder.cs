
namespace Jewel.JPMS.Api.Features.Variations.Documents;

/// <summary>
/// Collates a <see cref="VariationDocumentModel"/> from the SQL source of truth. Pure read — calling
/// it on download, on attach or on every resend always reflects the variation exactly as it stands
/// (idempotent regeneration; nothing is persisted). Same arrangement as RequestDocumentBuilder.
/// </summary>
public static class VariationDocumentBuilder
{
    public static async Task<VariationDocumentModel?> BuildAsync(
        JpmsContext context, string variationOrderId, CancellationToken cancellationToken)
    {
        var order = await context.VariationOrders.AsNoTracking()
            .FirstOrDefaultAsync(v => v.VariationOrderId == variationOrderId, cancellationToken);
        if (order is null)
            return null;

        var project = await context.Projects.AsNoTracking()
            .FirstOrDefaultAsync(p => p.ProjectId == order.ProjectId, cancellationToken);

        // The priced build-up lives on the valuation report under the minted V-ref once the order
        // is approved. Before then the line detail is the STAGED build-up on the record (the
        // agreed / quoted lines captured ahead of approval) — and the document shows it at every
        // stage, because the line items are what the reader needs to see even while quoting
        // (James, 2026-09-07). The sheet labels which of the two it is printing.
        var lines = new List<VariationDocumentLine>();
        var linesAreStaged = false;
        if (order.VariationRef is { Length: > 0 } variationRef)
            lines = await context.ValuationLineItems.AsNoTracking()
                .Where(line => line.ProjectId == order.ProjectId
                    && line.ElementType == (int)ValuationElementType.Variation
                    && line.VariationRef == variationRef)
                .OrderBy(line => line.DisplayOrder)
                .Select(line => new VariationDocumentLine(
                    line.CostCode, line.Description, line.Unit, line.Quantity, line.Rate, line.LineAmount))
                .ToListAsync(cancellationToken);

        if (lines.Count == 0 && VariationDraftLines.Parse(order.DraftLinesJson) is { Count: > 0 } staged)
        {
            // Same shape approval writes to the report ("item" lines, amount = qty × rate), so the
            // quoting-stage sheet and the approved sheet read as one document.
            lines = staged
                .Select(line => new VariationDocumentLine(
                    line.CostCode, line.Description, "item", line.Quantity, line.Rate, line.Quantity * line.Rate))
                .ToList();
            linesAreStaged = true;
        }

        var status = (VariationOrderStatus)order.Status;

        return new VariationDocumentModel(
            VariationOrderId: order.VariationOrderId,
            DisplayNumber: order.Number > 0 ? $"V{order.Number}" : "",
            Reference: order.Reference,
            Title: order.Title,
            Description: order.Description,
            StatusLabel: status.DisplayName(),
            ProjectName: project?.Name ?? "(unknown project)",
            ProjectReference: project?.Reference ?? order.ProjectId,
            ClientName: project?.ClientName ?? "",
            CreatedByEmail: order.CreatedByEmail,
            CreatedAt: order.CreatedAt,
            IssuedAt: order.IssuedAt,
            ApprovedAt: order.ApprovedAt,
            VariationRef: order.VariationRef,
            EstimatedValue: order.EstimatedValue,
            ApprovedValue: order.Value,
            IsApproved: status == VariationOrderStatus.Approved,
            CommercialBasis: order.CommercialBasis,
            ProgrammeImpact: order.ProgrammeImpact,
            Exclusions: order.Exclusions,
            Lines: lines,
            LinesAreStaged: linesAreStaged,
            GeneratedAt: DateTimeOffset.UtcNow);
    }
}
