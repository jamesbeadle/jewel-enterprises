namespace Jewel.JPMS.Contracts.Commercial;

/// <summary>
/// The one name a valuation report goes out under, whichever file it is: the PDF and the
/// spreadsheet of the same statement share it exactly, extension aside, so the pair sits
/// together in a downloads folder or an email. Project reference, project name, then the
/// statement — the claim's period name for the live report, the snapshot's label for a frozen
/// one — then the date it was produced. Nothing else: no "working copy", no time of day. The
/// project name is there for whoever files these outside the portal (accounts asked, 2026-09-07):
/// a reference alone means nothing in a downloads folder full of them. Characters no file system
/// accepts are dropped here, once, so neither side's own sanitising can make the two names drift
/// apart.
/// </summary>
public static class ValuationReportFileNames
{
    private const string DocumentTitle = "Valuation report";
    private const string DateFormat = "yyyy-MM-dd";
    private const string Separator = " - ";
    private const string UnsafeCharacters = "\\/:*?\"<>|";

    /// <summary>The full name, date included, without an extension.</summary>
    public static string For(string projectReference, string? projectName, string? statementLabel, DateTimeOffset producedOn) =>
        $"{Stem(projectReference, projectName, statementLabel)} {producedOn.ToString(DateFormat)}";

    /// <summary>The name before its date — what the Excel export button stamps a date onto.</summary>
    public static string Stem(string projectReference, string? projectName, string? statementLabel)
    {
        var parts = new[] { projectReference, projectName, DocumentTitle, statementLabel }
            .Select(part => Safe(part ?? ""))
            .Where(part => part.Length > 0);
        return string.Join(Separator, parts);
    }

    private static string Safe(string part)
    {
        var kept = part.Where(character => !UnsafeCharacters.Contains(character) && !char.IsControl(character));
        return new string(kept.ToArray()).Trim();
    }
}
