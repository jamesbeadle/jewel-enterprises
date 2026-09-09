using System.Text;

namespace Jewel.JPMS.Features.Procurement;

/// <summary>
/// The purchase-order covering email to the supplier, composed once for every route that emails a
/// work order: the PO page's "Draft email to supplier…" modal (pre-fill, editable) and the automatic
/// send fired when an order is released — created without "save as draft" (Work Orders tab, Control
/// Centre) or a draft approved. One builder means the supplier reads the same email whichever door
/// the order went out through: the order summary (priced lines, or value + scope when there is no
/// breakdown), programme dates when set, and the standard pre-start paperwork line (RAMS/insurances
/// to projects@ — never a named person). The purchase order PDF is attached by the API on both the
/// send and the draft (2026-09-09, the accountant's ask), so the body says so and carries no link
/// to the supplier portal: suppliers have no portal login yet and the link only reached a sign-in
/// page. The link returns with supplier users and their role testing.
/// </summary>
public static class WorkOrderPoEmail
{
    /// <summary>One displayed line of the order summary table. Mirrors WorkOrderLine's display
    /// fields so callers that only have staged input (Control Centre) can build rows the same way
    /// the server will store them (quantity 1, unit "item", total = amount).</summary>
    public sealed record Line(string Title, decimal Quantity, string Unit, decimal LineTotal);

    public static Line ToLine(WorkOrderLine line) => new(line.Title, line.Quantity, line.Unit, line.LineTotal);

    // Scope is plain text typed in the work-order form; the PO sheet prints it pre-wrap. The
    // email body is HTML, where raw newlines collapse into spaces — encode then convert breaks,
    // so a breakdown typed one charge per line stays one charge per line here too.
    private static string AsHtmlLines(string text) =>
        System.Net.WebUtility.HtmlEncode(text.Trim()).Replace("\r\n", "\n").Replace("\n", "<br/>");

    public static string Subject(WorkOrder order, string projectName) =>
        $"Work order {order.Reference} — {order.Title} — {projectName}";

    public static string Body(
        WorkOrder order,
        string supplierName,
        IReadOnlyList<Line> lines,
        string projectName)
    {
        var sb = new StringBuilder();
        sb.AppendLine($"<p>Hello {supplierName},</p>");
        sb.AppendLine($"<p>Please find below the details of our work order <strong>{order.Reference}</strong> for <strong>{(string.IsNullOrWhiteSpace(projectName) ? "the project" : projectName)}</strong>. The purchase order is attached.</p>");
        if (lines.Count > 0)
        {
            sb.AppendLine("<table border=\"1\" cellpadding=\"6\" cellspacing=\"0\" style=\"border-collapse:collapse\">");
            sb.AppendLine("<tr><th align=\"left\">Item</th><th align=\"left\">Qty</th><th align=\"left\">Unit</th><th align=\"right\">Total</th></tr>");
            foreach (var line in lines)
                sb.AppendLine($"<tr><td>{line.Title}</td><td>{line.Quantity}</td><td>{line.Unit}</td><td align=\"right\">{line.LineTotal:£#,##0.00}</td></tr>");
            sb.AppendLine($"<tr><td colspan=\"3\"><strong>Order total</strong></td><td align=\"right\"><strong>{order.Value:£#,##0.00}</strong></td></tr>");
            sb.AppendLine("</table>");
        }
        else
        {
            sb.AppendLine($"<p><strong>Order value:</strong> {order.Value:£#,##0.00}</p>");
            if (!string.IsNullOrWhiteSpace(order.Scope))
                sb.AppendLine($"<p><strong>Scope:</strong><br/>{AsHtmlLines(order.Scope)}</p>");
        }
        if (order.ProgrammeStart is { } start)
            sb.AppendLine($"<p><strong>Programme start:</strong> {start.LocalDateTime:d MMM yyyy}</p>");
        if (order.ScheduledCompletion is { } completion)
            sb.AppendLine($"<p><strong>Scheduled completion:</strong> {completion.LocalDateTime:d MMM yyyy}</p>");
        sb.AppendLine("<p>Before starting on site, please send your RAMS documentation and current insurance certificates to projects@jewelbb.co.uk.</p>");
        sb.AppendLine("<p>Kind regards,<br/>Jewel Bespoke Build</p>");
        return sb.ToString();
    }
}
