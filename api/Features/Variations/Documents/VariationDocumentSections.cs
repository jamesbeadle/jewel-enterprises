using MigraDoc.DocumentObjectModel;
using MigraDoc.DocumentObjectModel.Tables;
using static Jewel.JPMS.Api.Features.Documents.JewelDocumentStyle;

namespace Jewel.JPMS.Api.Features.Variations.Documents;

/// <summary>
/// The variation document's prose sections: the subject line, the details grid, the scope of
/// works, and the three narrative panels (commercial basis, programme impact, exclusions) —
/// rendered only when the record carries them, so a simple variation stays a simple sheet.
/// </summary>
internal static class VariationDocumentSections
{
    public static void AddTitleBlock(Section section, VariationDocumentModel model)
    {
        var label = section.AddParagraph("SUBJECT");
        label.Format.Font.Size = 7.5;
        label.Format.Font.Bold = true;
        label.Format.Font.Color = Muted;
        SpaceBefore(label, 3);
        SpaceAfter(label, 1);

        var title = section.AddParagraph(model.Title);
        title.Format.Font.Size = 13;
        title.Format.Font.Bold = true;
        title.Format.Font.Color = Navy;
        SpaceAfter(title, 2);
    }

    public static void AddDetailsGrid(Section section, VariationDocumentModel model)
    {
        var table = section.AddTable();
        table.Borders.Color = Hair;
        table.Borders.Width = 0.5;
        var labelWidth = Unit.FromCentimeter(3.3);
        var valueWidth = Unit.FromCentimeter(5.6);
        table.AddColumn(labelWidth);
        table.AddColumn(valueWidth);
        table.AddColumn(labelWidth);
        table.AddColumn(valueWidth);

        AddGridRow(table,
            "Project", model.ProjectName,
            "Project reference", model.ProjectReference);
        AddGridRow(table,
            "Client", string.IsNullOrWhiteSpace(model.ClientName) ? "—" : model.ClientName,
            "Status", model.StatusLabel);
        AddGridRow(table,
            "Value", ValueLabel(model),
            "Raised by", model.CreatedByEmail);

        SpaceAfterTable(section);
    }

    // The one honest figure for the document's stage: the agreed value once approved, the
    // estimate (named as such) while quoting, and a dash before anything has been priced.
    private static string ValueLabel(VariationDocumentModel model)
    {
        if (model.IsApproved) return Money(model.ApprovedValue);
        if (model.EstimatedValue is { } estimate) return $"{Money(estimate)} (estimated)";
        return "—";
    }

    public static void AddScopeOfWorks(Section section, VariationDocumentModel model)
    {
        SectionHeading(section, "Scope of works");
        Panelled(section, string.IsNullOrWhiteSpace(model.Description) ? "—" : model.Description);
        SpaceAfterTable(section);
    }

    /// <summary>One narrative panel — heading plus free text. Absent sections simply don't render.</summary>
    public static void AddNarrative(Section section, string heading, string? narrative)
    {
        if (string.IsNullOrWhiteSpace(narrative))
            return;
        SectionHeading(section, heading);
        Panelled(section, narrative!);
        SpaceAfterTable(section);
    }

    public static void AddFooter(Section section, VariationDocumentModel model) =>
        HouseFooter(section, $"Generated {DateAndTime(model.GeneratedAt)} · from the JPMS register (source of truth)");

    private static void AddGridRow(Table table, string label1, string value1, string label2, string value2)
    {
        var row = table.AddRow();
        row.TopPadding = Unit.FromMillimeter(1.2);
        row.BottomPadding = Unit.FromMillimeter(1.2);
        LabelCell(row.Cells[0], label1);
        ValueCell(row.Cells[1], value1);
        LabelCell(row.Cells[2], label2);
        ValueCell(row.Cells[3], value2);
    }
}
