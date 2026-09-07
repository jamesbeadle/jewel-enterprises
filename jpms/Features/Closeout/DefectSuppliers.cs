using Jewel.JPMS.Features.Triage;

namespace Jewel.JPMS.Features.Closeout;

// The supplier side of a defect on the client: the directory pool a defect can be raised with,
// and the email the defect page drafts to that supplier. One place, so the register's raise
// form, the page's Edit dialog and the page's "Send to supplier" all agree on who counts as a
// supplier and what the first email says.
public static class DefectSuppliers
{
    /// <summary>The picker pool: vetted directory records of the Subcontractor and Supplier
    /// categories (never prospects, never clients or architects), by company name.</summary>
    public static IReadOnlyList<SearchSelect.Option> Options(IEnumerable<Subcontractor> directory) =>
        directory
            .Where(s => !s.IsProspect && s.Category is DirectoryCategory.Subcontractor or DirectoryCategory.Supplier)
            .OrderBy(s => s.CompanyName, StringComparer.OrdinalIgnoreCase)
            .Select(s => new SearchSelect.Option(s.SubcontractorId,
                string.IsNullOrWhiteSpace(s.ContactEmail) ? $"{s.CompanyName} (no email on record)" : s.CompanyName))
            .ToList();

    /// <summary>The first email to the supplier — the defect as it stands, with the ask. Plain
    /// text: the composer turns it into HTML and the sender edits before sending. The subject
    /// leads with the reference so the supplier's reply keeps it in view.</summary>
    public static ComposePrefill RaiseEmail(Defect defect, Project? project, Subcontractor? supplier)
    {
        var projectLabel = project is null ? "" : $" at {project.Name} ({project.Reference})";
        var greeting = supplier is null || string.IsNullOrWhiteSpace(supplier.ContactName)
            ? "Hello,"
            : $"Hello {supplier.ContactName.Split(' ')[0]},";
        var body =
            $"{greeting}\n\n"
            + $"We have logged the following defect{projectLabel} and need it put right:\n\n"
            + $"{defect.Reference}\n"
            + (string.IsNullOrWhiteSpace(defect.Location) ? "" : $"Location: {defect.Location}\n")
            + $"Description: {defect.Description}\n\n"
            + "Please confirm when you can attend and rectify this. Reply to this email so your response stays on file against the defect.\n\n"
            + "Kind regards,\n";
        return new ComposePrefill(To: defect.SupplierEmail, Subject: Subject(defect, project), Body: body);
    }

    /// <summary>A chase — the defect was sent already; this asks for the response that hasn't come.</summary>
    public static ComposePrefill ChaseEmail(Defect defect, Project? project, Subcontractor? supplier)
    {
        var greeting = supplier is null || string.IsNullOrWhiteSpace(supplier.ContactName)
            ? "Hello,"
            : $"Hello {supplier.ContactName.Split(' ')[0]},";
        var sent = defect.SentToSupplierAt is { } at ? $" sent to you on {DateFormats.DateText(at)}" : "";
        var body =
            $"{greeting}\n\n"
            + $"A reminder about defect {defect.Reference}{sent}"
            + (string.IsNullOrWhiteSpace(defect.Location) ? "" : $" ({defect.Location})")
            + ":\n\n"
            + $"{defect.Description}\n\n"
            + "Please let us know when this will be attended to.\n\n"
            + "Kind regards,\n";
        return new ComposePrefill(To: defect.SupplierEmail, Subject: "Chasing: " + Subject(defect, project), Body: body);
    }

    private static string Subject(Defect defect, Project? project)
    {
        var parts = new List<string> { defect.Reference };
        if (!string.IsNullOrWhiteSpace(defect.Location)) parts.Add(defect.Location);
        if (project is not null) parts.Add(project.Name);
        return string.Join(" · ", parts);
    }
}
