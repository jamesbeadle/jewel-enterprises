using System.Globalization;
using Jewel.JPMS.Api.Features.Requests.Documents;
using MigraDoc.DocumentObjectModel;
using MigraDoc.DocumentObjectModel.Tables;
using MigraDoc.Rendering;
using PdfSharp.Fonts;

using static Jewel.JPMS.Api.Features.Documents.JewelDocumentStyle;

namespace Jewel.JPMS.Api.Features.Progress.Documents;

/// <summary>
/// Renders a <see cref="ProgressReportDocumentModel"/> into a branded, client-facing progress
/// report (PDF bytes) using PDFsharp/MigraDoc. Pure function of the model: the same bytes come out
/// for the same input (bar the generated-at footer), so regeneration on download is idempotent.
/// Follows the JewelBB palette established by <see cref="RequestDocumentRenderer"/>.
/// </summary>
public static class ProgressReportRenderer
{



    public static byte[] Render(ProgressReportDocumentModel model)
    {
        EnsureFonts();

        var document = new Document();
        document.Info.Title = $"{model.ProjectReference} Progress Report".Trim();
        document.Info.Author = "Jewel Bespoke Build";
        document.Info.Subject = model.Title;

        var normal = document.Styles["Normal"]!;
        normal.Font.Name = FontFamily;
        normal.Font.Size = 9;
        normal.Font.Color = Ink;

        var section = A4Page(document);

        AddHeaderBand(section, model);
        AddDetailsGrid(section, model);
        AddNarrative(section, "Introduction", model.Introduction);
        AddNarrative(section, "Work completed", model.WorkCompleted);
        AddUpdates(section, model);
        AddNarrative(section, "Upcoming works", model.UpcomingWorks);
        AddFooter(section, model);

        var renderer = new PdfDocumentRenderer { Document = document };
        renderer.RenderDocument();
        using var stream = new MemoryStream();
        renderer.PdfDocument.Save(stream, closeStream: false);
        return stream.ToArray();
    }

    // ---- Sections -----------------------------------------------------------------------------

    private static void AddHeaderBand(Section section, ProgressReportDocumentModel model)
    {
        var table = section.AddTable();
        table.Borders.Width = 0;
        var left = table.AddColumn(Unit.FromCentimeter(11.3));
        var right = table.AddColumn(Unit.FromCentimeter(6.5));
        right.Format.Alignment = ParagraphAlignment.Right;

        var row = table.AddRow();
        row.Shading.Color = Navy;
        row.TopPadding = Unit.FromMillimeter(4);
        row.BottomPadding = Unit.FromMillimeter(4);
        row.Cells[0].Format.LeftIndent = Unit.FromMillimeter(4);
        row.Cells[1].Format.RightIndent = Unit.FromMillimeter(4);
        row.Cells[0].VerticalAlignment = VerticalAlignment.Center;
        row.Cells[1].VerticalAlignment = VerticalAlignment.Center;

        // The official Jewel Bespoke Build logo leads the band — the gold/orange registered
        // artwork reads directly on the navy ground (embedded once in DocumentBranding).
        DocumentBranding.AddLogo(row.Cells[0], Unit.FromCentimeter(3.4), Unit.FromMillimeter(1.5));

        var heading = row.Cells[0].AddParagraph("PROGRESS REPORT");
        heading.Format.Font.Size = 17;
        heading.Format.Font.Bold = true;
        heading.Format.Font.Color = White;
        SpaceAfter(heading, 1);

        var sub = row.Cells[0].AddParagraph(model.Title);
        sub.Format.Font.Size = 9.5;
        sub.Format.Font.Bold = true;
        sub.Format.Font.Color = Gold;

        var project = row.Cells[1].AddParagraph(model.ProjectReference.ToUpperInvariant());
        project.Format.Font.Size = 10;
        project.Format.Font.Bold = true;
        project.Format.Font.Color = White;
        SpaceAfter(project, 2);

        var period = row.Cells[1].AddParagraph($"Period  {Period(model.PeriodStart, model.PeriodEnd)}");
        period.Format.Font.Size = 8;
        period.Format.Font.Color = Gold;

        Hairline(section);
    }

    private static void AddDetailsGrid(Section section, ProgressReportDocumentModel model)
    {
        var spacer = section.AddParagraph();
        spacer.Format.SpaceAfter = Unit.FromMillimeter(1.5);
        spacer.Format.Font.Size = 2;

        var table = section.AddTable();
        table.Borders.Color = Hair;
        table.Borders.Width = 0.5;
        var labelW = Unit.FromCentimeter(3.3);
        var valueW = Unit.FromCentimeter(5.6);
        table.AddColumn(labelW);
        table.AddColumn(valueW);
        table.AddColumn(labelW);
        table.AddColumn(valueW);

        AddGridRow(table,
            "Project", model.ProjectName,
            "Project reference", model.ProjectReference);
        AddGridRow(table,
            "Client", string.IsNullOrWhiteSpace(model.ClientName) ? "—" : model.ClientName,
            "Report period", Period(model.PeriodStart, model.PeriodEnd));
        AddGridRow(table,
            "Prepared by", model.PreparedByEmail,
            "Report date", Date(model.GeneratedAt));

        SpaceAfterTable(section);
    }

    private static void AddNarrative(Section section, string heading, string text)
    {
        if (string.IsNullOrWhiteSpace(text))
            return;

        SectionHeading(section, heading);
        Panelled(section, text);
        SpaceAfterTable(section);
    }

    private static void AddUpdates(Section section, ProgressReportDocumentModel model)
    {
        if (model.Updates.Count == 0)
            return;

        SectionHeading(section, "Progress of the works");

        foreach (var update in model.Updates)
        {
            var title = section.AddParagraph(update.Title);
            title.Format.Font.Size = 10;
            title.Format.Font.Bold = true;
            title.Format.Font.Color = Navy;
            title.Format.KeepWithNext = true;
            SpaceBefore(title, 4);
            SpaceAfter(title, 1);

            if (update.WorkDate is { } workDate)
            {
                var when = section.AddParagraph(Date(workDate));
                when.Format.Font.Size = 8;
                when.Format.Font.Color = Gold;
                when.Format.Font.Bold = true;
                when.Format.KeepWithNext = true;
                SpaceAfter(when, 1.5);
            }

            if (!string.IsNullOrWhiteSpace(update.Description))
            {
                var description = section.AddParagraph(update.Description);
                description.Format.Font.Size = 9.5;
                SpaceAfter(description, 2);
            }

            if (update.Weather is { } weather)
                AddWeather(section, weather);

            AddPhotoGrid(section, update.Photos);
        }

        SpaceAfterTable(section);
    }

    // Weather conditions recorded on site: a summary line with the observation time, then the
    // figures (°C high/low, wind mph, humidity %, precipitation inches — the units the daily-log
    // reports have always used). Every part is optional; only what was recorded is printed.
    private static void AddWeather(Section section, ProgressWeather weather)
    {
        var label = section.AddParagraph("Weather conditions");
        label.Format.Font.Size = 8;
        label.Format.Font.Bold = true;
        label.Format.Font.Color = Muted;
        label.Format.KeepWithNext = true;
        SpaceAfter(label, 1);

        var summaryParts = new List<string>();
        if (!string.IsNullOrWhiteSpace(weather.Summary)) summaryParts.Add(weather.Summary.Trim());
        if (weather.ObservedAt is { } observedAt) summaryParts.Add(observedAt.ToString("ddd, dd MMM yyyy, HH:mm", Uk));
        if (summaryParts.Count > 0)
        {
            var summary = section.AddParagraph(string.Join("    ", summaryParts));
            summary.Format.Font.Size = 9.5;
            summary.Format.KeepWithNext = true;
            SpaceAfter(summary, 1);
        }

        var figures = new List<string>();
        var temps = (weather.TempHighC, weather.TempLowC) switch
        {
            ({ } highC, { } lowC) => $"{highC}°C / {lowC}°C",
            ({ } highC, null) => $"{highC}°C",
            (null, { } lowC) => $"{lowC}°C",
            _ => null
        };
        if (temps is not null) figures.Add(temps);
        if (weather.WindMph is { } windMph) figures.Add($"Wind {windMph} mph");
        if (weather.HumidityPercent is { } humidity) figures.Add($"Humidity {humidity}%");
        if (weather.PrecipInches is { } precip) figures.Add($"Total precip {precip.ToString("0.##", Uk)}\"");
        if (figures.Count > 0)
        {
            var line = section.AddParagraph(string.Join("  ·  ", figures));
            line.Format.Font.Size = 9;
            line.Format.Font.Bold = true;
            line.Format.Font.Color = Navy;
            SpaceAfter(line, 2);
        }
    }

    // Photos sit two-up in a borderless table; MigraDoc keeps the aspect ratio, so rows grow to
    // the taller of their two images.
    private static void AddPhotoGrid(Section section, IReadOnlyList<ProgressReportDocumentPhoto> photos)
    {
        if (photos.Count == 0)
            return;

        var table = section.AddTable();
        table.Borders.Width = 0;
        table.AddColumn(Unit.FromCentimeter(8.9));
        table.AddColumn(Unit.FromCentimeter(8.9));

        for (var index = 0; index < photos.Count; index += 2)
        {
            var row = table.AddRow();
            row.TopPadding = Unit.FromMillimeter(1);
            row.BottomPadding = Unit.FromMillimeter(1);
            AddPhotoCell(row.Cells[0], photos[index]);
            if (index + 1 < photos.Count)
                AddPhotoCell(row.Cells[1], photos[index + 1]);
        }

        SpaceAfterTable(section);
    }

    private static void AddPhotoCell(Cell cell, ProgressReportDocumentPhoto photo)
    {
        // MigraDoc accepts image data inline via the base64: pseudo-filename, which keeps the
        // renderer a pure function of the model (no temp files).
        var image = cell.AddImage("base64:" + Convert.ToBase64String(photo.Content));
        image.LockAspectRatio = true;
        image.Width = Unit.FromCentimeter(8.6);
    }

    private static void AddFooter(Section section, ProgressReportDocumentModel model) =>
        HouseFooter(section, $"Generated {DateTime(model.GeneratedAt)} · from the JPMS register (source of truth)");

    // ---- Helpers ------------------------------------------------------------------------------

    private static void SectionHeading(Section section, string text)
    {
        var p = section.AddParagraph(text);
        p.Format.Font.Size = 10.5;
        p.Format.Font.Bold = true;
        p.Format.Font.Color = Navy;
        p.Format.Borders.Bottom.Width = 0.75;
        p.Format.Borders.Bottom.Color = Orange;
        p.Format.Borders.Distance = Unit.FromMillimeter(1.5);
        p.Format.KeepWithNext = true;
        SpaceBefore(p, 4);
        SpaceAfter(p, 2.5);
    }

    private static Paragraph Panelled(Section section, string text)
    {
        var table = section.AddTable();
        table.Borders.Width = 0;
        table.AddColumn(Unit.FromCentimeter(17.8));
        var row = table.AddRow();
        row.Shading.Color = Panel;
        row.TopPadding = Unit.FromMillimeter(2.5);
        row.BottomPadding = Unit.FromMillimeter(2.5);
        row.Cells[0].Format.LeftIndent = Unit.FromMillimeter(2.5);
        row.Cells[0].Format.RightIndent = Unit.FromMillimeter(2.5);
        var p = row.Cells[0].AddParagraph(text);
        p.Format.Font.Size = 9.5;
        return p;
    }

    private static void AddGridRow(Table table, string l1, string v1, string l2, string v2)
    {
        var row = table.AddRow();
        row.TopPadding = Unit.FromMillimeter(1.2);
        row.BottomPadding = Unit.FromMillimeter(1.2);
        LabelCell(row.Cells[0], l1);
        ValueCell(row.Cells[1], v1);
        LabelCell(row.Cells[2], l2);
        ValueCell(row.Cells[3], v2);
    }

    private static void LabelCell(Cell cell, string text)
    {
        cell.Shading.Color = Panel;
        cell.Format.LeftIndent = Unit.FromMillimeter(1.5);
        var p = cell.AddParagraph(text);
        p.Format.Font.Size = 8;
        p.Format.Font.Bold = true;
        p.Format.Font.Color = Muted;
    }

    private static void ValueCell(Cell cell, string text)
    {
        cell.Format.LeftIndent = Unit.FromMillimeter(1.5);
        var p = cell.AddParagraph(string.IsNullOrWhiteSpace(text) ? "—" : text);
        p.Format.Font.Size = 9;
        p.Format.Font.Color = Ink;
    }


    private static void SpaceBefore(Paragraph p, double mm) => p.Format.SpaceBefore = Unit.FromMillimeter(mm);
    private static void SpaceAfter(Paragraph p, double mm) => p.Format.SpaceAfter = Unit.FromMillimeter(mm);


    private static string Period(DateTimeOffset? start, DateTimeOffset? end) => (start, end) switch
    {
        ({ } s, { } e) => $"{Date(s)} – {Date(e)}",
        ({ } s, null) => $"From {Date(s)}",
        (null, { } e) => $"To {Date(e)}",
        _ => "—"
    };

    private static string DateTime(DateTimeOffset value) => value.ToString("dd MMM yyyy HH:mm", Uk);

}
