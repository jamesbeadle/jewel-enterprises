namespace Jewel.JPMS.Contracts.Documents.Excel;

/// <summary>
/// Number/display format applied to a column when exporting to Excel.
/// </summary>
public enum ExcelFormat
{
    /// <summary>Excel's General format — used for plain text and untyped values.</summary>
    General,
    /// <summary>Whole numbers with thousands separators (#,##0).</summary>
    Integer,
    /// <summary>Two-decimal numbers with thousands separators (#,##0.00).</summary>
    Number,
    /// <summary>Pounds sterling (£#,##0.00).</summary>
    Currency,
    /// <summary>UK date (dd/mm/yyyy).</summary>
    Date,
    /// <summary>UK date and time (dd/mm/yyyy hh:mm).</summary>
    DateTime,
    /// <summary>Percentage with one decimal (0.0%). Supply values as fractions, e.g. 0.42 for 42%.</summary>
    Percent,
}

/// <summary>
/// Named font treatments for presentation cells. Each maps to one concrete font
/// (size, weight, colour) in the writer — the JewelBB document palette, matching the
/// branded PDFs (navy band, gold accents, muted greys).
/// </summary>
public enum ExcelFont
{
    /// <summary>Calibri 11 in the default ink.</summary>
    Default,
    /// <summary>Calibri 11 bold.</summary>
    Bold,
    /// <summary>Calibri 10 muted grey — secondary facts, comments.</summary>
    Muted,
    /// <summary>Calibri 9 muted grey — footnotes and legends.</summary>
    SmallMuted,
    /// <summary>Calibri 16 bold white — the report title on the navy band.</summary>
    Title,
    /// <summary>Calibri 10 bold gold — subtitles on the navy band.</summary>
    Gold,
    /// <summary>Calibri 9 white — small print on the navy band.</summary>
    BandText,
    /// <summary>Calibri 11 bold navy — section headings and strong figures.</summary>
    NavyBold,
    /// <summary>Calibri 11 negative red — omits and other negative money.</summary>
    Negative,
}

/// <summary>Background fill for a presentation cell.</summary>
public enum ExcelFill
{
    None,
    /// <summary>The JewelBB navy header band.</summary>
    Navy,
    /// <summary>Light panel grey — column headers, totals rows, strong summary rows.</summary>
    Panel,
    /// <summary>Warm gold tint — lines that moved this period.</summary>
    Highlight,
    /// <summary>Light green tint — a month (or line) that made money. Pairs with <see cref="Negative"/>: the
    /// running-profit grid's one colour rule, the sign of the month's £.</summary>
    Positive,
    /// <summary>Light red tint — a month (or line) that lost money.</summary>
    Negative,
}

/// <summary>Bottom border applied to a presentation cell.</summary>
public enum ExcelBorder
{
    None,
    /// <summary>Thin grey hairline under data rows.</summary>
    Hairline,
    /// <summary>The orange rule under section headings.</summary>
    Accent,
}

/// <summary>Horizontal alignment override. Auto leaves Excel's own default (numbers right, text left).</summary>
public enum ExcelAlign { Auto, Left, Right, Center }

/// <summary>
/// The full visual treatment of one presentation cell. The writer deduplicates styles, so
/// reusing the same record instance (or equal values) costs nothing.
/// </summary>
public sealed record ExcelCellStyle(
    ExcelFormat Format = ExcelFormat.General,
    ExcelFont Font = ExcelFont.Default,
    ExcelFill Fill = ExcelFill.None,
    ExcelBorder Border = ExcelBorder.None,
    ExcelAlign Align = ExcelAlign.Auto,
    bool WrapText = false);

/// <summary>
/// One presentation cell: a value plus its style. Mix freely with plain values in a row —
/// plain values keep taking their column's format, styled cells override everything.
/// A null value with a style still renders (an empty navy band cell, a filled spacer).
/// </summary>
public sealed record ExcelStyledCell(object? Value, ExcelCellStyle Style);

/// <summary>
/// A column definition for an exported sheet. Width is in Excel character units;
/// leave null to size automatically from the header and cell contents.
/// </summary>
public sealed record ExcelColumn(string Header, ExcelFormat Format = ExcelFormat.General, double? Width = null);

/// <summary>
/// A single worksheet: an optional styled header row followed by data rows.
/// Row cells are matched to columns by position; nulls render as blank cells.
/// The presentation flags (header row, autofilter, freeze, gridlines) default to the
/// classic data-table behaviour; a branded presentation sheet turns them off and
/// builds its layout from <see cref="ExcelStyledCell"/>s instead.
/// </summary>
public sealed class ExcelSheet
{
    public ExcelSheet(string name, IReadOnlyList<ExcelColumn> columns)
    {
        Name = name;
        Columns = columns;
    }

    public string Name { get; }
    public IReadOnlyList<ExcelColumn> Columns { get; }
    public List<object?[]> Rows { get; } = new();

    /// <summary>Render the bold column-header row (and count it in the autofilter/freeze).</summary>
    public bool ShowHeaderRow { get; set; } = true;
    /// <summary>Put an autofilter across the header row. Ignored when the header row is off.</summary>
    public bool AutoFilter { get; set; } = true;
    /// <summary>Freeze the header row. Ignored when the header row is off.</summary>
    public bool FreezeHeaderRow { get; set; } = true;
    /// <summary>A presentation grid's own freeze — this many leading rows (its title and heading rows)
    /// and leading columns (its label column) stay put while the body scrolls. Read only when the
    /// header-row freeze above does not apply; zero means no freeze.</summary>
    public int FrozenRows { get; set; }
    public int FrozenColumns { get; set; }
    /// <summary>Show worksheet gridlines. Presentation sheets switch them off.</summary>
    public bool ShowGridLines { get; set; } = true;
    /// <summary>Print landscape, fitted to one page wide — for presentation sheets an accountant may print.</summary>
    public bool PrintLandscapeFitToWidth { get; set; }

    /// <summary>ARGB hex (e.g. "FFC00000") to colour the sheet's tab in Excel's tab bar; null keeps the default grey.</summary>
    public string? TabColour { get; set; }

    /// <summary>A1-style ranges to merge (e.g. "A1:F1"). Content and style come from the top-left cell;
    /// give the other cells in the range the same fill so the band reads as one block.</summary>
    public List<string> MergedRanges { get; } = new();

    public void AddRow(params object?[] cells) => Rows.Add(cells);
}

/// <summary>
/// A workbook to export: one or more sheets. Build with collection or object
/// initializer syntax, then hand to <see cref="ExcelExportService"/>.
/// </summary>
public sealed class ExcelWorkbook
{
    public List<ExcelSheet> Sheets { get; } = new();

    public ExcelSheet AddSheet(string name, params ExcelColumn[] columns)
    {
        var sheet = new ExcelSheet(name, columns);
        Sheets.Add(sheet);
        return sheet;
    }
}
