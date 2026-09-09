using Jewel.JPMS.Api.Features.Commercial.Documents;
using Jewel.JPMS.Api.Features.MailboxIntake.Sharing;
using Jewel.JPMS.Contracts.Commercial;
using Jewel.JPMS.Contracts.ValuationInvoices;
using Microsoft.Extensions.DependencyInjection;

namespace Jewel.JPMS.Api.Features.Ai.Tools;

/// <summary>
/// The valuation-invoice register and the frozen report snapshots, readable (2026-08-31). The
/// parity audit's deepest blind-write pair (docs/ai/11 §3): all nine invoice lifecycle actions
/// were mirrored while the register itself — statuses, certified-to-date, what the client was
/// actually sent — was invisible. Each tool wraps the SAME query handler its endpoint composes.
/// export_valuation_report (2026-09-02, the accountant's ask) adds the report as FILES: the
/// portal's own PDF and workbook, rendered by the download endpoints' builders and handed over
/// as expiring links — so an AI session pulls the real document instead of rebuilding one.
/// </summary>
internal static partial class AiValuationInvoiceTools
{
    private static readonly JsonSerializerOptions Json = new() { WriteIndented = false };

    private static string Serialise(object value) => JsonSerializer.Serialize(value, Json);
    private static string Fail(string message) => Serialise(new { ok = false, error = message });

    private static string? ProjectId(AiToolContext context, JsonElement input) =>
        AiToolSchema.Text(input, "projectId") ?? context.Scope?.ProjectId;

    public static IReadOnlyList<AiTool> Build() => Registers().Concat(XeroRaiseTools()).ToList();

    private static IReadOnlyList<AiTool> Registers()
    {
        return new List<AiTool>
        {
            new(
                "list_valuation_invoices",
                "A project's valuation-invoice register — every VI with its status (Raised, "
                + "Submitted, Approved, Issued, Paid, Cancelled), amount, deposit credit, gross "
                + "certificate, payment received and lifecycle dates — plus the project's money "
                + "summary: total raised, awaiting approval, invoiced, certified to date, paid and "
                + "outstanding. Call this BEFORE any valuation-invoice action: it is the register "
                + "those actions change.",
                AiToolSchema.Object(
                    ("projectId", "string", "Defaults to the project in view; pass it otherwise (list_projects returns ids).", false)),
                AiToolKind.Read,
                JpmsRoleSets.AllInternal,
                async (context, input, ct) =>
                {
                    var projectId = ProjectId(context, input);
                    if (string.IsNullOrWhiteSpace(projectId))
                        return Fail("Say which project: pass projectId (list_projects returns ids).");

                    var invoices = await context.Services
                        .GetRequiredService<IQueryHandler<ListValuationInvoicesForProject, IReadOnlyList<ValuationInvoice>>>()
                        .HandleAsync(new ListValuationInvoicesForProject(projectId), ct);
                    var summary = await context.Services
                        .GetRequiredService<IQueryHandler<GetProjectValuationInvoiceSummary, ProjectValuationInvoiceSummary>>()
                        .HandleAsync(new GetProjectValuationInvoiceSummary(projectId), ct);

                    return Serialise(new
                    {
                        ok = true,
                        projectId,
                        summary = new
                        {
                            summary.TotalRaised,
                            summary.TotalAwaitingApproval,
                            summary.TotalInvoiced,
                            summary.TotalDepositCredited,
                            certifiedToDate = summary.TotalCertified,
                            summary.TotalPaid,
                            summary.Outstanding
                        },
                        invoices = invoices.Select(invoice => new
                        {
                            invoice.ValuationInvoiceId,
                            number = invoice.DisplayNumber,
                            periodMonth = invoice.PeriodMonth.ToString("yyyy-MM"),
                            status = invoice.Status.ToString(),
                            invoice.Amount,
                            invoice.DepositCredited,
                            grossCertificate = invoice.CertifiedAmount,
                            invoice.AmountPaid,
                            invoice.RaisedAt,
                            invoice.SubmittedAt,
                            invoice.ApprovedAt,
                            invoice.IssuedAt,
                            invoice.PaidAt,
                            invoice.RejectedAt,
                            invoice.RejectionReason,
                            invoice.AmendmentCount,
                            invoice.IsManual,
                            snapshotId = invoice.ValuationReportSnapshotId,
                            invoice.XeroInvoiceId,
                            invoice.XeroInvoiceNumber,
                            invoice.XeroRaisedAt
                        }),
                        note = "The client-facing statement behind an invoice is its FROZEN snapshot "
                               + "(get_valuation_snapshot), never the live report."
                    });
                }),

            new(
                "list_valuation_snapshots",
                "A project's frozen valuation-report snapshots — each the exact statement a client "
                + "was (or could be) sent: label, when taken, the invoice it backs, whether a later "
                + "snapshot supersedes it, and its frozen summary figures. The live report is a "
                + "working copy; a snapshot is the issued record.",
                AiToolSchema.Object(
                    ("projectId", "string", "Defaults to the project in view; pass it otherwise.", false)),
                AiToolKind.Read,
                JpmsRoleSets.AllInternal,
                async (context, input, ct) =>
                {
                    var projectId = ProjectId(context, input);
                    if (string.IsNullOrWhiteSpace(projectId))
                        return Fail("Say which project: pass projectId (list_projects returns ids).");

                    var snapshots = await context.Services
                        .GetRequiredService<IQueryHandler<ListValuationReportSnapshotsForProject, IReadOnlyList<ValuationReportSnapshot>>>()
                        .HandleAsync(new ListValuationReportSnapshotsForProject(projectId), ct);

                    return Serialise(new
                    {
                        ok = true,
                        projectId,
                        count = snapshots.Count,
                        snapshots = snapshots.Select(snapshot => new
                        {
                            snapshot.ValuationReportSnapshotId,
                            snapshot.Label,
                            snapshot.TakenAt,
                            invoiceId = snapshot.ValuationInvoiceId,
                            snapshot.IsSuperseded,
                            snapshot.ContractSum,
                            snapshot.NetVariations,
                            snapshot.RevisedContractSum,
                            snapshot.TotalWorksComplete,
                            snapshot.RetentionPercent,
                            snapshot.RetentionHeld
                        }),
                        note = "get_valuation_snapshot(valuationReportSnapshotId) returns a snapshot's frozen lines."
                    });
                }),

            new(
                "get_valuation_snapshot",
                "One frozen valuation-report snapshot in full — the summary footer and every "
                + "frozen line (section, variation ref, cost code, description, quantity, rate) "
                + "exactly as the statement stood when it was taken. This is what the client saw; "
                + "compare against get_valuation_context for what has moved since.",
                AiToolSchema.Object(
                    ("valuationReportSnapshotId", "string", "The snapshot's id from list_valuation_snapshots or list_valuation_invoices.", true)),
                AiToolKind.Read,
                JpmsRoleSets.AllInternal,
                async (context, input, ct) =>
                {
                    var snapshotId = AiToolSchema.Text(input, "valuationReportSnapshotId")?.Trim();
                    if (string.IsNullOrWhiteSpace(snapshotId))
                        return Fail("A valuationReportSnapshotId is required — list_valuation_snapshots returns them.");

                    ValuationReportSnapshotDetail detail;
                    try
                    {
                        detail = await context.Services
                            .GetRequiredService<IQueryHandler<GetValuationReportSnapshot, ValuationReportSnapshotDetail>>()
                            .HandleAsync(new GetValuationReportSnapshot(snapshotId), ct);
                    }
                    catch (InvalidOperationException)
                    {
                        return Fail($"No snapshot exists with id \"{snapshotId}\" — "
                                    + "list_valuation_snapshots returns the ids that do.");
                    }

                    return Serialise(new
                    {
                        ok = true,
                        snapshot = detail.Snapshot,
                        lines = detail.Lines.Select(line => new
                        {
                            line.SectionCode,
                            line.SectionName,
                            line.VariationRef,
                            lineType = line.LineType.ToString(),
                            line.CostCode,
                            clientReference = line.ClientReference,
                            line.Description,
                            line.Unit,
                            line.Quantity,
                            line.Rate,
                            line.LineAmount,
                            line.PercentComplete,
                            line.CumulativeClaimed,
                            line.PeriodIncrement,
                            countsTowardTotals = line.CountsTowardTotals
                        })
                    });
                }),

            new(
                "export_valuation_report",
                "The portal's OWN valuation report files — the branded PDF and the Excel workbook "
                + "the valuation page's Download / Export buttons produce — rendered server-side "
                + "and handed over as time-limited download links (the same expiring links large "
                + "email attachments travel by). Use this to give the user the report as a file: "
                + "never rebuild a statement from get_valuation_context or get_valuation_snapshot "
                + "figures when this can hand them the real document. By default it exports the "
                + "LIVE report as a working copy of the latest claim (stamped as such throughout — "
                + "the review-before-you-claim export); pass valuationReportSnapshotId to export a "
                + "frozen snapshot instead, which is the only form a client may be sent. Returns "
                + "one link per file with its name, size and expiry, plus the statement's headline "
                + "figures so you can describe what the file says.",
                AiToolSchema.Object(
                    ("projectId", "string", "Defaults to the project in view; pass it otherwise (list_projects returns ids). Ignored when valuationReportSnapshotId is given.", false),
                    ("valuationReportSnapshotId", "string", "Export this frozen snapshot (list_valuation_snapshots / list_valuation_invoices return ids) instead of the live working copy.", false),
                    ("files", "string", "Which files to render: \"both\" (default), \"pdf\" or \"excel\".", false)),
                AiToolKind.Read,
                // Mirrors DownloadValuationReportDraftPdfEndpoint / DownloadValuationReportSnapshotPdfEndpoint:
                // commercial reads are internal-only, external portal logins have no view of project money.
                JpmsRoleSets.AllInternal,
                async (context, input, ct) =>
                {
                    var snapshotId = AiToolSchema.Text(input, "valuationReportSnapshotId")?.Trim();
                    var projectId = ProjectId(context, input);
                    var files = (AiToolSchema.Text(input, "files") ?? "both").Trim().ToLowerInvariant();
                    if (files is not ("both" or "pdf" or "excel"))
                        return Fail("files must be \"both\", \"pdf\" or \"excel\".");
                    if (string.IsNullOrWhiteSpace(snapshotId) && string.IsNullOrWhiteSpace(projectId))
                        return Fail("Say which project: pass projectId (list_projects returns ids), or a valuationReportSnapshotId for a frozen statement.");

                    var shareStore = context.Services.GetRequiredService<IEmailFileShareStore>();
                    if (!shareStore.IsConfigured)
                    {
                        return Fail("This portal host has no file-share store configured, so download links cannot "
                                    + "be minted here. The same files are one click away in the portal: the "
                                    + "valuation page's Download PDF and Export to Excel buttons"
                                    + (string.IsNullOrWhiteSpace(projectId) ? "." : $" (/projects/{projectId}/valuation)."));
                    }

                    var pdfBuilder = context.Services.GetRequiredService<ValuationReportSnapshotPdfBuilder>();
                    ValuationReportStatement statement;
                    try
                    {
                        statement = string.IsNullOrWhiteSpace(snapshotId)
                            ? await pdfBuilder.LoadDraftAsync(projectId!, ct)
                            : await pdfBuilder.LoadAsync(snapshotId, ct);
                    }
                    catch (InvalidOperationException)
                    {
                        return Fail(string.IsNullOrWhiteSpace(snapshotId)
                            ? $"No project exists with id \"{projectId}\" — list_projects returns the ids that do."
                            : $"No snapshot exists with id \"{snapshotId}\" — list_valuation_snapshots returns the ids that do.");
                    }

                    var rendered = new List<(string Kind, string FileName, string ContentType, byte[] Content)>();
                    if (files is "both" or "pdf")
                    {
                        var pdf = ValuationReportSnapshotPdfBuilder.Render(statement);
                        rendered.Add(("pdf", pdf.FileName, "application/pdf", pdf.Content));
                    }
                    if (files is "both" or "excel")
                    {
                        var workbook = await context.Services
                            .GetRequiredService<ValuationReportWorkbookBuilder>()
                            .BuildAsync(statement, ct);
                        rendered.Add(("excel", workbook.FileName,
                            "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", workbook.Content));
                    }

                    // One link per file, or none: a half-shared export would hand the user a PDF
                    // that says one thing and no workbook to reconcile it against.
                    var links = new List<object>();
                    foreach (var (kind, fileName, contentType, content) in rendered)
                    {
                        var link = await shareStore.ShareAsync(statement.ProjectReference, fileName, contentType, content, ct);
                        if (link is null)
                            return Fail("The file-share store could not mint a download link (its credential cannot "
                                        + "sign links). The files are still one click away on the valuation page's "
                                        + "Download PDF and Export to Excel buttons.");
                        links.Add(new
                        {
                            kind,
                            fileName = link.FileName,
                            sizeBytes = link.SizeBytes,
                            url = link.Url.ToString(),
                            expiresAt = link.ExpiresAt
                        });
                    }

                    var snapshot = statement.Detail.Snapshot;
                    return Serialise(new
                    {
                        ok = true,
                        projectId = statement.ProjectId,
                        projectReference = statement.ProjectReference,
                        projectName = statement.ProjectName,
                        statement = new
                        {
                            label = snapshot.Label,
                            isWorkingCopy = statement.IsDraft,
                            snapshotId = statement.IsDraft ? null : snapshot.ValuationReportSnapshotId,
                            producedAt = snapshot.TakenAt,
                            lines = statement.Detail.Lines.Count
                        },
                        files = links,
                        figures = new
                        {
                            snapshot.ContractSum,
                            snapshot.NetVariations,
                            snapshot.RevisedContractSum,
                            snapshot.TotalWorksComplete,
                            snapshot.RetentionHeld,
                            snapshot.RetentionReleased,
                            snapshot.CertifiedToDate,
                            snapshot.PaymentDueExVat
                        },
                        note = (statement.IsDraft
                                   ? "This is the LIVE report as a working copy — stamped as such on every page — for "
                                     + "checking a claim before it goes anywhere. A client is only ever sent the frozen "
                                     + "snapshot behind an invoice: pass valuationReportSnapshotId for that. "
                                   : "This is the frozen statement exactly as it stood when the snapshot was taken. ")
                               + $"The links expire after {AzureBlobEmailFileShareStore.LinkLifetime.TotalDays:0} days; "
                               + "give them to the user as links to click — the files are theirs to download, not "
                               + "content to reproduce."
                    });
                })
        };
    }
}
