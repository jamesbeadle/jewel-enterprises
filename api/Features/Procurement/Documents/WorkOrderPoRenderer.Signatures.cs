using MigraDoc.DocumentObjectModel;
using MigraDoc.DocumentObjectModel.Tables;

using static Jewel.JPMS.Api.Features.Documents.JewelDocumentStyle;

namespace Jewel.JPMS.Api.Features.Procurement.Documents;

public static partial class WorkOrderPoRenderer
{
    private static void AddAcceptanceWording(Section section)
    {
        var legal = section.AddParagraph(
            "A signature of Approval or Electronic Acceptance is required before this purchase order is "
            + "effective. This purchase order then becomes part of the existing contract and is binding "
            + "and subject to our terms and conditions detailed in our Work Orders.");
        legal.Format.Font.Size = 8;
        legal.Format.Font.Color = Muted;
        SpaceBefore(legal, 3);
        SpaceAfter(legal, 3);
    }

    private static void AddSignatures(Section section, WorkOrderPoDocumentModel model)
    {
        var table = section.AddTable();
        table.Borders.Width = 0;
        table.AddColumn(Unit.FromCentimeter(8.9));
        table.AddColumn(Unit.FromCentimeter(8.9));

        var row = table.AddRow();
        row.TopPadding = Unit.FromMillimeter(2);
        ApproverBlock(row.Cells[0], model);
        AcceptorBlock(row.Cells[1], model);
    }

    // A draft has nobody to name (same empty-line rule as the sheet), but the reply-draft flow
    // refuses drafts, so in practice this always prints the approver.
    private static void ApproverBlock(Cell cell, WorkOrderPoDocumentModel model)
    {
        if (model.Order.IsDraft) { SignatureBlock(cell, "", "Awaiting approval", ""); return; }
        var approver = string.IsNullOrWhiteSpace(model.ApprovedByName)
            ? model.Order.AwardedByEmail
            : model.ApprovedByName;
        SignatureBlock(cell, approver, "Approved by", DateTime(model.Order.AwardedAt));
    }

    private static void AcceptorBlock(Cell cell, WorkOrderPoDocumentModel model)
    {
        var order = model.Order;
        if (!order.IsAccepted) { SignatureBlock(cell, "", "Awaiting electronic acceptance", ""); return; }
        var acceptor = string.IsNullOrWhiteSpace(order.AcceptedByName) ? order.AcceptedByEmail : order.AcceptedByName;
        SignatureBlock(cell, acceptor, "Electronically accepted by", DateTime(order.AcceptedAt!.Value));
    }

    private static void SignatureBlock(Cell cell, string name, string label, string when)
    {
        var nameLine = cell.AddParagraph(string.IsNullOrWhiteSpace(name) ? " " : name);
        nameLine.Format.Font.Size = 10;
        nameLine.Format.Font.Bold = true;
        nameLine.Format.Font.Color = Navy;
        nameLine.Format.Borders.Bottom.Width = 0.75;
        nameLine.Format.Borders.Bottom.Color = Hair;
        nameLine.Format.Borders.Distance = Unit.FromMillimeter(1.5);
        nameLine.Format.RightIndent = Unit.FromCentimeter(1.5);
        SpaceAfter(nameLine, 1);

        var labelLine = cell.AddParagraph(label);
        labelLine.Format.Font.Size = 7.5;
        labelLine.Format.Font.Bold = true;
        labelLine.Format.Font.Color = Muted;

        var whenLine = cell.AddParagraph(string.IsNullOrWhiteSpace(when) ? " " : when);
        whenLine.Format.Font.Size = 8;
        whenLine.Format.Font.Color = Muted;
    }

    private static void AddFooter(Section section) =>
        HouseFooter(section, $"Generated {DateTime(DateTimeOffset.Now)} · from the JPMS register (source of truth)");
}
