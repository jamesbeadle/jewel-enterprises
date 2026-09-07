using System.Globalization;
using System.Text;

namespace Jewel.JPMS.Contracts.Documents.Excel;

/// <summary>
/// The workbook writer's style registry: builds styles.xml from the styles the sheets
/// actually reference. The first eight cellXfs reproduce the writer's classic fixed indexes
/// (default, header, and the six column formats) so plain data sheets keep rendering exactly
/// as before; presentation styles are registered on first use and deduplicated by value.
/// </summary>
internal sealed class ExcelStyleRegistry
{
    // JewelBB document palette — matches the branded PDF renderers.
    private const string MutedArgb = "FF606672";
    private const string NavyArgb = "FF1A1E29";
    private const string GoldArgb = "FFC09A51";
    private const string WhiteArgb = "FFFFFFFF";
    private const string NegativeArgb = "FFB42318";
    private const string PanelArgb = "FFF3F3F5";
    private const string HighlightArgb = "FFFBF2E2";
    private const string PositiveFillArgb = "FFDCEFE3";
    private const string NegativeFillArgb = "FFF6DADA";
    private const string LegacyHeaderFillArgb = "FFF2F1EE";
    private const string HairlineArgb = "FFB9B6B0";
    private const string AccentArgb = "FFFF8300";

    private sealed record FontSpec(double Size, bool Bold, string? ColorArgb);
    private sealed record XfKey(int FontId, int FillId, int BorderId, int NumFmtId, ExcelAlign Align, bool WrapText);

    private readonly List<FontSpec> fonts = new();
    private readonly List<string?> fills = new();     // solid fill ARGB; null = none, "gray125" = the mandatory second fill
    private readonly List<ExcelBorder> borders = new();
    private readonly List<XfKey> cellXfs = new();
    private readonly Dictionary<XfKey, int> xfIndex = new();

    public ExcelStyleRegistry()
    {
        FontId(new FontSpec(11, false, null));                       // font 0 — default
        FontId(new FontSpec(11, true, null));                        // font 1 — bold
        fills.Add(null); fills.Add("gray125");                       // fills 0, 1 — required by the spec
        borders.Add(ExcelBorder.None);                               // border 0

        // The eight classic cellXfs, in their historical order: 0 default, 1 header,
        // 2 integer, 3 number, 4 currency, 5 date, 6 datetime, 7 percent.
        Register(new XfKey(0, 0, 0, 0, ExcelAlign.Auto, false));
        Register(new XfKey(1, FillId(LegacyHeaderFillArgb), BorderId(ExcelBorder.Hairline), 0, ExcelAlign.Auto, false));
        Register(new XfKey(0, 0, 0, NumFmtId(ExcelFormat.Integer), ExcelAlign.Auto, false));
        Register(new XfKey(0, 0, 0, NumFmtId(ExcelFormat.Number), ExcelAlign.Auto, false));
        Register(new XfKey(0, 0, 0, NumFmtId(ExcelFormat.Currency), ExcelAlign.Auto, false));
        Register(new XfKey(0, 0, 0, NumFmtId(ExcelFormat.Date), ExcelAlign.Auto, false));
        Register(new XfKey(0, 0, 0, NumFmtId(ExcelFormat.DateTime), ExcelAlign.Auto, false));
        Register(new XfKey(0, 0, 0, NumFmtId(ExcelFormat.Percent), ExcelAlign.Auto, false));
    }

    public int Header => 1;

    public int For(ExcelFormat format) => format switch
    {
        ExcelFormat.Integer => 2,
        ExcelFormat.Number => 3,
        ExcelFormat.Currency => 4,
        ExcelFormat.Date => 5,
        ExcelFormat.DateTime => 6,
        ExcelFormat.Percent => 7,
        _ => 0,
    };

    public int For(ExcelCellStyle style)
    {
        var font = style.Font switch
        {
            ExcelFont.Bold => new FontSpec(11, true, null),
            ExcelFont.Muted => new FontSpec(10, false, MutedArgb),
            ExcelFont.SmallMuted => new FontSpec(9, false, MutedArgb),
            ExcelFont.Title => new FontSpec(16, true, WhiteArgb),
            ExcelFont.Gold => new FontSpec(10, true, GoldArgb),
            ExcelFont.BandText => new FontSpec(9, false, WhiteArgb),
            ExcelFont.NavyBold => new FontSpec(11, true, NavyArgb),
            ExcelFont.Negative => new FontSpec(11, false, NegativeArgb),
            _ => new FontSpec(11, false, null),
        };
        var fill = style.Fill switch
        {
            ExcelFill.Navy => FillId(NavyArgb),
            ExcelFill.Panel => FillId(PanelArgb),
            ExcelFill.Highlight => FillId(HighlightArgb),
            ExcelFill.Positive => FillId(PositiveFillArgb),
            ExcelFill.Negative => FillId(NegativeFillArgb),
            _ => 0,
        };
        return Register(new XfKey(
            FontId(font), fill, BorderId(style.Border), NumFmtId(style.Format), style.Align, style.WrapText));
    }

    private int FontId(FontSpec spec)
    {
        var index = fonts.IndexOf(spec);
        if (index >= 0) return index;
        fonts.Add(spec);
        return fonts.Count - 1;
    }

    private int FillId(string argb)
    {
        var index = fills.IndexOf(argb);
        if (index >= 0) return index;
        fills.Add(argb);
        return fills.Count - 1;
    }

    private int BorderId(ExcelBorder border)
    {
        var index = borders.IndexOf(border);
        if (index >= 0) return index;
        borders.Add(border);
        return borders.Count - 1;
    }

    private static int NumFmtId(ExcelFormat format) => format switch
    {
        ExcelFormat.Integer => 164,
        ExcelFormat.Number => 165,
        ExcelFormat.Currency => 166,
        ExcelFormat.Date => 167,
        ExcelFormat.DateTime => 168,
        ExcelFormat.Percent => 169,
        _ => 0,
    };

    private int Register(XfKey key)
    {
        if (xfIndex.TryGetValue(key, out var existing)) return existing;
        cellXfs.Add(key);
        var index = cellXfs.Count - 1;
        xfIndex[key] = index;
        return index;
    }

    public string ToXml()
    {
        var builder = new StringBuilder();
        builder.Append("""<?xml version="1.0" encoding="UTF-8" standalone="yes"?>""");
        builder.Append("""<styleSheet xmlns="http://schemas.openxmlformats.org/spreadsheetml/2006/main">""");

        builder.Append("""<numFmts count="6">""");
        builder.Append("""<numFmt numFmtId="164" formatCode="#,##0"/>""");
        builder.Append("""<numFmt numFmtId="165" formatCode="#,##0.00"/>""");
        builder.Append("""<numFmt numFmtId="166" formatCode="&quot;£&quot;#,##0.00"/>""");
        builder.Append("""<numFmt numFmtId="167" formatCode="dd/mm/yyyy"/>""");
        builder.Append("""<numFmt numFmtId="168" formatCode="dd/mm/yyyy\ hh:mm"/>""");
        builder.Append("""<numFmt numFmtId="169" formatCode="0.0%"/>""");
        builder.Append("</numFmts>");

        builder.Append($"""<fonts count="{fonts.Count}">""");
        foreach (var font in fonts)
        {
            builder.Append("<font>");
            if (font.Bold) builder.Append("<b/>");
            builder.Append($"""<sz val="{font.Size.ToString("0.##", CultureInfo.InvariantCulture)}"/>""");
            if (font.ColorArgb is not null) builder.Append($"""<color rgb="{font.ColorArgb}"/>""");
            builder.Append("""<name val="Calibri"/></font>""");
        }
        builder.Append("</fonts>");

        builder.Append($"""<fills count="{fills.Count}">""");
        foreach (var fill in fills)
        {
            builder.Append(fill switch
            {
                null => """<fill><patternFill patternType="none"/></fill>""",
                "gray125" => """<fill><patternFill patternType="gray125"/></fill>""",
                _ => $"""<fill><patternFill patternType="solid"><fgColor rgb="{fill}"/></patternFill></fill>""",
            });
        }
        builder.Append("</fills>");

        builder.Append($"""<borders count="{borders.Count}">""");
        foreach (var border in borders)
        {
            builder.Append(border switch
            {
                ExcelBorder.Hairline => $"""<border><left/><right/><top/><bottom style="thin"><color rgb="{HairlineArgb}"/></bottom><diagonal/></border>""",
                ExcelBorder.Accent => $"""<border><left/><right/><top/><bottom style="medium"><color rgb="{AccentArgb}"/></bottom><diagonal/></border>""",
                _ => "<border><left/><right/><top/><bottom/><diagonal/></border>",
            });
        }
        builder.Append("</borders>");

        builder.Append("""<cellStyleXfs count="1"><xf numFmtId="0" fontId="0" fillId="0" borderId="0"/></cellStyleXfs>""");

        builder.Append($"""<cellXfs count="{cellXfs.Count}">""");
        foreach (var xf in cellXfs)
        {
            builder.Append($"<xf numFmtId=\"{xf.NumFmtId}\" fontId=\"{xf.FontId}\" fillId=\"{xf.FillId}\" borderId=\"{xf.BorderId}\" xfId=\"0\"");
            if (xf.NumFmtId != 0) builder.Append(" applyNumberFormat=\"1\"");
            if (xf.FontId != 0) builder.Append(" applyFont=\"1\"");
            if (xf.FillId != 0) builder.Append(" applyFill=\"1\"");
            if (xf.BorderId != 0) builder.Append(" applyBorder=\"1\"");
            if (xf.Align != ExcelAlign.Auto || xf.WrapText)
            {
                builder.Append(" applyAlignment=\"1\"><alignment");
                if (xf.Align != ExcelAlign.Auto)
                    builder.Append($" horizontal=\"{xf.Align.ToString().ToLowerInvariant()}\"");
                if (xf.WrapText) builder.Append(" wrapText=\"1\" vertical=\"top\"");
                builder.Append("/></xf>");
            }
            else
            {
                builder.Append("/>");
            }
        }
        builder.Append("</cellXfs>");

        builder.Append("""<cellStyles count="1"><cellStyle name="Normal" xfId="0" builtinId="0"/></cellStyles>""");
        builder.Append("</styleSheet>");
        return builder.ToString();
    }
}
