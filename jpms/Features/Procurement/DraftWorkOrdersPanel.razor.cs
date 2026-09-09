using Jewel.JPMS.Features.Projects;
using Jewel.JPMS.Features.Subcontractors;
using static Jewel.JPMS.Features.Procurement.WorkOrderDisplay;

namespace Jewel.JPMS.Features.Procurement;

public partial class DraftWorkOrdersPanel
{
    [Inject] private ICommandSender Commands { get; set; } = default!;
    [Inject] private ProjectWorkOrdersReadModel WorkOrders { get; set; } = default!;
    [Inject] private SubcontractorsReadModel Subcontractors { get; set; } = default!;
    [Inject] private ProjectListReadModel Projects { get; set; } = default!;
    [Inject] private AuthService Auth { get; set; } = default!;

    [Parameter, EditorRequired] public string ProjectId { get; set; } = "";
    [Parameter, EditorRequired] public IReadOnlyList<ProjectWorkOrderDetail> Drafts { get; set; } = default!;
    /// <summary>Edit opens the page's manual-order modal over the draft.</summary>
    [Parameter] public EventCallback<ProjectWorkOrderDetail> OnEdit { get; set; }
    /// <summary>Delete opens the page's confirm modal — the one choice that leaves no record behind.</summary>
    [Parameter] public EventCallback<ProjectWorkOrderDetail> OnDelete { get; set; }
    /// <summary>What happened to the purchase-order email an approval promised — the page shows it at the top.</summary>
    [Parameter] public EventCallback<string> OnPoEmailNote { get; set; }

    private enum DraftDecision { Approve, Reject }

    // pendingDraftId holds the draft awaiting its confirm click, pendingDecision which decision
    // was asked for.
    private string? pendingDraftId;
    private DraftDecision pendingDecision;
    private bool decisionBusy;
    private string? decisionError;

    private DraftDecision? PendingFor(ProjectWorkOrderDetail detail) =>
        pendingDraftId == detail.Order.WorkOrderId ? pendingDecision : null;

    private void SetPending(ProjectWorkOrderDetail detail, DraftDecision decision)
    {
        pendingDraftId = detail.Order.WorkOrderId;
        pendingDecision = decision;
        decisionError = null;
    }

    private void ClearPending() => pendingDraftId = null;

    // The draft row's Actions menu. Approve and Reject only ARM the row's inline confirm step
    // (SetPending) — the decision itself still takes the second, spelled-out click. Delete opens
    // the confirm modal instead, because it alone leaves no record behind.
    private IReadOnlyList<DropdownMenu.Item> MenuItems(ProjectWorkOrderDetail detail) =>
        new List<DropdownMenu.Item>
        {
            new(Label: "Preview PO",
                Href: PurchaseOrderPath(ProjectId, detail.Order.WorkOrderId),
                Hint: "Open the purchase order exactly as it will read once approved — the sheet carries a Draft mark until the number is minted; print or save it as a PDF from there"),
            new(Label: "Edit…",
                OnSelect: EventCallback.Factory.Create(this, () => OnEdit.InvokeAsync(detail)),
                Hint: "Edit the draft's supplier, title, scope and priced lines"),
            new(Label: "Approve…",
                OnSelect: EventCallback.Factory.Create(this, () => SetPending(detail, DraftDecision.Approve)),
                Hint: "Approve this draft — it takes the next order number and becomes a live order",
                Group: 1),
            new(Label: "Reject…",
                OnSelect: EventCallback.Factory.Create(this, () => SetPending(detail, DraftDecision.Reject)),
                Hint: "Reject this draft — terminal; its value stops counting everywhere, and it stays listed under Rejected",
                Destructive: true, Group: 1),
            new(Label: "Delete…",
                OnSelect: EventCallback.Factory.Create(this, () => OnDelete.InvokeAsync(detail)),
                Hint: "Delete this draft outright — for drafts raised in error; nothing stays on this page",
                Destructive: true, Group: 2)
        };

    /// <summary>The supplier's directory email for an order, or null when the directory has
    /// none — which is exactly the case the approve warning must call out.</summary>
    private string? SupplierEmailFor(ProjectWorkOrderDetail detail)
    {
        var supplier = (Subcontractors.Current ?? Array.Empty<Subcontractor>()).FirstOrDefault(sub =>
            string.Equals(sub.SubcontractorId, detail.Order.SubcontractorId, StringComparison.OrdinalIgnoreCase));
        return string.IsNullOrWhiteSpace(supplier?.ContactEmail) ? null : supplier!.ContactEmail;
    }

    private async Task DecideAsync(ProjectWorkOrderDetail detail, DraftDecision decision)
    {
        if (decisionBusy) return;
        decisionBusy = true;
        decisionError = null;
        try
        {
            if (decision == DraftDecision.Approve)
            {
                var approved = await Commands.SendAsync(new ApproveWorkOrder(
                    ProjectId, detail.Order.WorkOrderId, Auth.CurrentUser?.Email ?? ""), CancellationToken.None);
                pendingDraftId = null;
                await WorkOrders.RefreshAsync(ProjectId, CancellationToken.None);
                // The email the approve warning promised — non-fatal by design: the order is
                // released either way, and the note says what happened to its email.
                await OnPoEmailNote.InvokeAsync(await TrySendPoEmailAsync(approved, detail));
            }
            else
            {
                await Commands.SendAsync(new RejectWorkOrder(
                    ProjectId, detail.Order.WorkOrderId), CancellationToken.None);
                pendingDraftId = null;
                await WorkOrders.RefreshAsync(ProjectId, CancellationToken.None);
            }
        }
        catch (CommandFailedException ex)
        {
            // 400s are answers for the caller to show, not toasts — say what refused and why.
            decisionError = decision == DraftDecision.Approve
                ? $"Couldn't approve \"{detail.Order.Title}\": {ex.Message}"
                : $"Couldn't reject \"{detail.Order.Title}\": {ex.Message}";
        }
        finally
        {
            decisionBusy = false;
        }
    }

    /// <summary>Sends the purchase-order email a release promised (draft approval path).
    /// Never throws — the order is released either way; the returned note says what happened.</summary>
    private async Task<string> TrySendPoEmailAsync(WorkOrder order, ProjectWorkOrderDetail detail)
    {
        if (SupplierEmailFor(detail) is null)
            return $"{order.Reference} was approved, but the supplier has no email address in the directory "
                + "so the purchase order wasn't emailed — add one, then send it from the PO page.";
        var projectName = Projects.Find(ProjectId)?.Name ?? "";
        var emailLines = detail.Lines.OrderBy(l => l.SortOrder).Select(WorkOrderPoEmail.ToLine).ToList();
        try
        {
            var outcome = await Commands.SendAsync(new SendWorkOrderPoEmail(
                order.WorkOrderId,
                WorkOrderPoEmail.Subject(order, string.IsNullOrWhiteSpace(projectName) ? ProjectId : projectName),
                WorkOrderPoEmail.Body(order, detail.SubcontractorName, emailLines, projectName)),
                CancellationToken.None);
            return outcome.Sent
                ? $"{order.Reference} was approved and the purchase order was emailed to {outcome.RecipientEmail}."
                : $"{order.Reference} was approved. {outcome.FailureNote}";
        }
        catch (CommandFailedException ex)
        {
            return $"{order.Reference} was approved, but the purchase-order email couldn't be sent: "
                + $"{ex.Message} You can send it from the PO page.";
        }
        catch
        {
            return $"{order.Reference} was approved, but the purchase-order email couldn't be sent "
                + "— you can send it from the PO page.";
        }
    }
}
