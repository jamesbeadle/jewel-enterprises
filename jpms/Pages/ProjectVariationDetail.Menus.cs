using Jewel.JPMS.Contracts.Variations;
using Jewel.JPMS.Features.RecordLinks;

namespace Jewel.JPMS.Pages;

public partial class ProjectVariationDetail
{
    // ---- Title-bar menus --------------------------------------------------------------------------
    // Both menus are built here rather than inline in markup, so the whole set of things you can do
    // to a variation — and exactly what gates each one — reads as a single list in one place. The
    // panels below the bar carry no buttons of their own any more: every editor, dialog and confirm
    // they hold is opened from here (bound open-flags), which is what keeps the page's actions in
    // one menu instead of scattered down the sidebar.

    private string DocumentHref => $"/api/variation-orders/{VariationOrderId}/document";

    // The one green button. Pre-approval it is the approval itself (the step that writes the
    // contract figures); once approved, instructing the works. A rejected order has no next step.
    private string? PrimaryLabel => order is null || !CanManage ? null : order.Status switch
    {
        VariationOrderStatus.Approved => "Issue work order",
        VariationOrderStatus.Rejected => null,
        _ => IsManualApproval ? "Approve manually & raise VO…" : "Approve & raise VO…",
    };

    private Task RunPrimary() => order?.Status == VariationOrderStatus.Approved ? IssueWorkOrder() : FocusApprovePanel();

    private bool IsManualApproval => string.IsNullOrEmpty(order?.SelectedSubcontractorId);

    // Empty (a plain pill renders) when the user can't manage the record or it has reached the
    // terminal Rejected status. Approved -> Issued is never allowed directly.
    private List<DropdownMenu.Item> StatusMenuItems
    {
        get
        {
            if (order is null || !CanManage || order.Status == VariationOrderStatus.Rejected)
                return new List<DropdownMenu.Item>();

            return OrderStatusOptions
                .Select(status =>
                {
                    var isCurrent = status == order.Status;
                    var blocked = order.Status == VariationOrderStatus.Approved && status == VariationOrderStatus.Issued;
                    return new DropdownMenu.Item(
                        Label: PillOptionLabel(status),
                        OnSelect: EventCallback.Factory.Create(this, () => ChangeStatus(status)),
                        Hint: PillOptionTitle(status),
                        Disabled: busy || isCurrent || blocked,
                        Selected: isCurrent);
                })
                .ToList();
        }
    }

    private List<DropdownMenu.Item> ActionMenuItems
    {
        get
        {
            var items = new List<DropdownMenu.Item>();
            if (order is null) return items;

            // Group 0 — the document itself. Everyone who can see the record can download it.
            items.Add(new(Label: "Download Variation Order PDF", Href: DocumentHref, Download: true,
                Hint: "Rendered fresh from the record on every download — scope, commercial basis, programme impact, exclusions and the cost breakdown"));

            if (!CanManage) return items;

            var approved = order.Status == VariationOrderStatus.Approved;
            var preApproval = order.Status.IsPreApproval();

            // Group 1 — moving the variation along. Approval also has the primary slot; it stays
            // here so the menu is a complete list of what can be done, not a list of leftovers.
            if (preApproval)
            {
                items.Add(new(Label: IsManualApproval ? "Approve manually & raise Variation Order…" : "Approve & raise Variation Order…",
                    OnSelect: EventCallback.Factory.Create(this, FocusApprovePanel),
                    Hint: "Build the variation up from priced lines — one per cost centre — then approve. Writes the Valuation Report, CVR and cost-centre budgets",
                    Disabled: busy, Group: 1));
                items.Add(new(Label: order.DraftLines is { Count: > 0 } ? "Edit staged build-up…" : "Stage agreed build-up…",
                    OnSelect: EventCallback.Factory.Create(this, () => { buildUpDialogOpen = true; }),
                    Hint: "Capture the client-agreed lines now: their total becomes the estimate and approval opens pre-filled",
                    Disabled: busy, Group: 1));
                items.Add(new(Label: "Record agreed tender…",
                    OnSelect: EventCallback.Factory.Create(this, () => { recordingTender = true; }),
                    Hint: "Who the works will be instructed to if approved, and the value they agreed",
                    Disabled: busy, Group: 1));
            }
            if (approved)
            {
                items.Add(new(Label: "Issue work order",
                    OnSelect: EventCallback.Factory.Create(this, IssueWorkOrder),
                    Hint: "Instructs the approved works to the selected subcontractor",
                    Disabled: busy, Group: 1));
                // Held shut until the report lines are in: the dialog seeds itself from them, so
                // opening it early would offer an empty build-up.
                items.Add(new(Label: "Edit lines…",
                    OnSelect: EventCallback.Factory.Create(this, OpenEditLines),
                    Hint: ValuationLinesReady
                        ? "Add, edit or remove the priced lines without un-approving — the report, CVR and budgets move by the difference"
                        : "Waiting for the Valuation Report lines to load",
                    Disabled: busy || !ValuationLinesReady, Group: 1));
                if (ValuationLinesReady && VariationLines.Count <= 1)
                    items.Add(new(Label: "Revise value…",
                        OnSelect: EventCallback.Factory.Create(this, OpenReviseValue),
                        Hint: "Re-prices the line on the Valuation Report and moves the CVR and committed budget by the difference",
                        Disabled: busy, Group: 1));
            }

            // Group 2 — editing the record's wording and facts.
            items.Add(new(Label: "Edit title…",
                OnSelect: EventCallback.Factory.Create(this, StartRename),
                Hint: "Allowed at every stage — it is the number, not the wording, the client's paperwork is keyed to",
                Disabled: busy, Group: 2));
            items.Add(new(Label: "Edit document sections…",
                OnSelect: EventCallback.Factory.Create(this, () => { editingSections = true; }),
                Hint: "Commercial basis, programme impact and exclusions — the next download picks the wording up",
                Disabled: busy || editingSections, Group: 2));
            if (CanEditEstimate)
                items.Add(new(Label: "Edit estimate…",
                    OnSelect: EventCallback.Factory.Create(this, () => { editingEstimate = true; }),
                    Hint: "Blank or zero marks the order unpriced, taking it off the valuation export's Pending tab",
                    Disabled: busy || editingEstimate, Group: 2));
            if (string.IsNullOrWhiteSpace(order.RequestId))
                items.Add(new(Label: "Link originating request…",
                    OnSelect: EventCallback.Factory.Create(this, () => { linkingRequest = true; }),
                    Hint: "Attach the RFI or request this variation was raised from, so its history can be traced",
                    Disabled: busy, Group: 2));

            // Group 3 — the commercial reversals and the destructive tail, kept last and marked.
            if (approved)
            {
                items.Add(new(Label: "Return to quoting…",
                    OnSelect: EventCallback.Factory.Create(this, () => ChangeStatus(VariationOrderStatus.Quoting)),
                    Hint: "Approved in error? Un-approves the order and reverses the approval's valuation / CVR / budget writes",
                    Disabled: busy, Destructive: true, Group: 3));
                items.Add(new(Label: "Reject variation order…",
                    OnSelect: EventCallback.Factory.Create(this, () => ChangeStatus(VariationOrderStatus.Rejected)),
                    Hint: "A real commercial event: the order stays on the register as Rejected and its writes are reversed",
                    Disabled: busy, Destructive: true, Group: 3));
            }
            if (preApproval)
            {
                items.Add(new(Label: "Decline variation…",
                    OnSelect: EventCallback.Factory.Create(this, () => ChangeStatus(VariationOrderStatus.Rejected)),
                    Hint: "The client declined it or it has been withdrawn — stays on the register as Rejected, nothing commercial written",
                    Disabled: busy, Destructive: true, Group: 3));
            }
            // Anything not approved — a rejected order included — can be deleted: nothing of it is
            // on the Valuation Report, so no commercial record changes.
            if (!approved)
                items.Add(new(Label: "Delete variation…",
                    OnSelect: EventCallback.Factory.Create(this, () => { deletingOrder = true; }),
                    Hint: "Wrong variation? Nothing is on the Valuation Report, so no commercial records change — but it can't be undone",
                    Disabled: busy, Destructive: true, Group: 3));

            return items;
        }
    }
}
