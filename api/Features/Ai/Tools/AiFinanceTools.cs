using Jewel.JPMS.Api.Features.DocumentControl;
using Jewel.JPMS.Api.Features.WeeklyCashflow;
using Jewel.JPMS.Api.Features.Xero.Ledger;
using Jewel.JPMS.Contracts.DocumentControl;
using Jewel.JPMS.Contracts.WeeklyCashflow;
using Jewel.JPMS.Contracts.Xero;
using Microsoft.Extensions.DependencyInjection;

namespace Jewel.JPMS.Api.Features.Ai.Tools;

/// <summary>
/// The finance read surface (2026-08-31, docs/ai/11 §3): the weekly cashflow plan, the aged
/// payables/receivables pictures (drafts INCLUDED — the coding procedure holds bills in draft, so
/// Xero's own aged reports undercount; these are the complete ones), payment certificates and the
/// Xero allocation ledger. Every tool wraps the query handler its endpoint composes and mirrors
/// that endpoint's role gate exactly.
/// </summary>
internal static partial class AiFinanceTools
{
    private static readonly JsonSerializerOptions Json = new() { WriteIndented = false };

    /// <summary>Mirror of the aged payables/receivables endpoints' gate.</summary>
    private static readonly RoleSet FinanceReaders = RoleSet.Of(
        Role.Admin, JpmsRoles.Director, JpmsRoles.FinanceDirector, JpmsRoles.ProjectManager,
        JpmsRoles.Estimator, JpmsRoles.Accounts);

    private static string Serialise(object value) => JsonSerializer.Serialize(value, Json);
    private static string Fail(string message) => Serialise(new { ok = false, error = message });

    public static IReadOnlyList<AiTool> Build()
    {
        return new List<AiTool>
        {
            new(
                "get_weekly_cashflow_plan",
                "The accountant's live 13-week payment plan: the manual items, every placement "
                + "(which week an entry was moved to and by whom), the supplier groups whose bills "
                + "move together, and the exclusions. Xero-fed bills and invoices seed the grid at "
                + "their due/planned weeks; this is the overlay that says where they REALLY land. "
                + "Read this before any weekly-cashflow action.",
                AiToolSchema.Empty(),
                AiToolKind.Read,
                WeeklyCashflowGates.WeeklyCashflowRoles,
                async (context, _, ct) =>
                {
                    var plan = await context.Services
                        .GetRequiredService<IQueryHandler<GetWeeklyCashflowPlan, WeeklyCashflowPlan>>()
                        .HandleAsync(new GetWeeklyCashflowPlan(), ct);
                    return Serialise(new
                    {
                        ok = true,
                        items = plan.Items,
                        placements = plan.Placements,
                        supplierGroups = plan.SupplierGroups,
                        exclusions = plan.Exclusions,
                        note = "Moving an entry changes WHEN it is paid, never how much. Real payment "
                               + "agreements belong in Xero as the bill's planned date."
                    });
                }),

            new(
                "get_aged_payables",
                "Everything the company owes suppliers, aged — INCLUDING draft bills, which the "
                + "coding procedure deliberately holds in draft until allocated, so Xero's own aged "
                + "payables report undercounts. This is the complete payables picture; quote it, "
                + "never Xero's report, for what we owe.",
                AiToolSchema.Object(
                    ("force", "boolean", "true bypasses the server's short cache for a fresh Xero read.", false)),
                AiToolKind.Read,
                FinanceReaders,
                async (context, input, ct) =>
                {
                    var snapshot = await context.Services
                        .GetRequiredService<IQueryHandler<GetXeroAgedPayables, XeroAgedPayablesSnapshot>>()
                        .HandleAsync(new GetXeroAgedPayables(AiToolSchema.Flag(input, "force") ?? false), ct);
                    if (!snapshot.IsConfigured) return Fail("Xero is not configured on this server.");
                    if (snapshot.Error is not null) return Fail($"Xero refused the read: {snapshot.Error}");
                    return Serialise(new
                    {
                        ok = true,
                        fetchedAtUtc = snapshot.FetchedAtUtc,
                        truncated = snapshot.Truncated,
                        count = snapshot.Bills.Count,
                        bills = snapshot.Bills
                    });
                }),

            new(
                "get_aged_receivables",
                "Everything clients owe the company, aged — including draft sales invoices, the "
                + "same completeness rule as get_aged_payables. The sales-side mirror.",
                AiToolSchema.Object(
                    ("force", "boolean", "true bypasses the server's short cache for a fresh Xero read.", false)),
                AiToolKind.Read,
                FinanceReaders,
                async (context, input, ct) =>
                {
                    var snapshot = await context.Services
                        .GetRequiredService<IQueryHandler<GetXeroAgedReceivables, XeroAgedReceivablesSnapshot>>()
                        .HandleAsync(new GetXeroAgedReceivables(AiToolSchema.Flag(input, "force") ?? false), ct);
                    if (!snapshot.IsConfigured) return Fail("Xero is not configured on this server.");
                    if (snapshot.Error is not null) return Fail($"Xero refused the read: {snapshot.Error}");
                    return Serialise(new
                    {
                        ok = true,
                        fetchedAtUtc = snapshot.FetchedAtUtc,
                        truncated = snapshot.Truncated,
                        count = snapshot.Invoices.Count,
                        invoices = snapshot.Invoices
                    });
                }),

            new(
                "list_payment_certificates",
                "The payment-certificate register — the client's (or their agent's) certificates "
                + "saying what is being paid against valuations, filed from Document Triage: "
                + "number, issued date, certified amount, the claim each ties to, and the file's "
                + "name. Optionally one project's.",
                AiToolSchema.Object(
                    ("projectId", "string", "Only this project's certificates; omit for all.", false)),
                AiToolKind.Read,
                DocumentControlRoles.AllowedToReadPaymentCertificates,
                async (context, input, ct) =>
                {
                    var certificates = await context.Services
                        .GetRequiredService<IQueryHandler<ListPaymentCertificates, IReadOnlyList<PaymentCertificate>>>()
                        .HandleAsync(new ListPaymentCertificates(AiToolSchema.Text(input, "projectId")), ct);
                    return Serialise(new { ok = true, count = certificates.Count, certificates });
                }),

            new(
                "list_xero_ledger_lines",
                "The Xero allocation ledger — cost-of-sales purchase lines and where each stands: "
                + "Unallocated (awaiting a project + cost centre), Allocated (with any splits), "
                + "Bucketed, Ignored or Disputed (with the dispute thread). Pass a status to read "
                + "that queue, or a projectId for one project's allocated lines; with neither, the "
                + "per-status counts come back so you can pick. This is the data behind the Xero "
                + "Cost Allocation page and each project's cost-of-sales spend. Unallocated lines "
                + "whose bill matched an open work order carry workOrderBill (the Work Order bills "
                + "tab's card: the orders, the proposed per-order slices, every open order of the "
                + "supplier) — approve_work_order_bill takes it from there; lines a candidate order "
                + "refused carry workOrderExceptionReason; lines approved as a Work Order bill carry "
                + "workOrderApproval.",
                AiToolSchema.Object(
                    ("status", "string", "Unallocated, Allocated, Bucketed, Ignored or Disputed.", false),
                    ("projectId", "string", "One project's allocated lines instead of a status queue.", false),
                    ("take", "number", "With projectId only: maximum lines, default 100.", false)),
                AiToolKind.Read,
                XeroLedgerRoles.AllowedToAllocate,
                async (context, input, ct) =>
                {
                    var projectId = AiToolSchema.Text(input, "projectId");
                    if (!string.IsNullOrWhiteSpace(projectId))
                    {
                        var projectLines = await context.Services
                            .GetRequiredService<IQueryHandler<ListXeroLedgerLinesForProject, IReadOnlyList<XeroLedgerLine>>>()
                            .HandleAsync(new ListXeroLedgerLinesForProject(
                                projectId, Math.Clamp(AiToolSchema.Number(input, "take") ?? 100, 1, 500)), ct);
                        return Serialise(new { ok = true, projectId, count = projectLines.Count,
                            lines = projectLines.Select(Line) });
                    }

                    var statusText = AiToolSchema.Text(input, "status")?.Trim();
                    if (string.IsNullOrWhiteSpace(statusText))
                    {
                        var counts = await context.Services
                            .GetRequiredService<IQueryHandler<GetXeroLedgerCounts, XeroLedgerCounts>>()
                            .HandleAsync(new GetXeroLedgerCounts(), ct);
                        return Serialise(new { ok = true, counts,
                            note = "Pass a status to read that queue's lines." });
                    }

                    if (!Enum.TryParse<XeroAllocationStatus>(statusText, ignoreCase: true, out var status))
                        return Fail("status must be Unallocated, Allocated, Bucketed, Ignored or Disputed.");

                    var lines = await context.Services
                        .GetRequiredService<IQueryHandler<ListXeroLedgerLines, IReadOnlyList<XeroLedgerLine>>>()
                        .HandleAsync(new ListXeroLedgerLines(status), ct);
                    return Serialise(new { ok = true, status = status.ToString(), count = lines.Count,
                        lines = lines.Select(Line) });
                })
        };
    }
}
