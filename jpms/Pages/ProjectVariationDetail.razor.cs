using Jewel.JPMS.Contracts.Variations;
using Jewel.JPMS.Features.RecordLinks;
using Jewel.JPMS.Features.Triage;
using Jewel.JPMS.Features.Triage.Panels;

namespace Jewel.JPMS.Pages;

public partial class ProjectVariationDetail
{
    [Parameter] public string ProjectId { get; set; } = "";
    [Parameter] public string VariationOrderId { get; set; } = "";

    // Session checked and the user is signed in — not "the record is here". The tab chrome shows
    // straight away; the record and each panel's own sources arrive behind their gates.
    private bool sessionReady;
    // The record's fetch has answered, one way or the other. Distinct from `order is not null`,
    // which is also how "no such variation" looks.
    private bool orderLoaded;
    private bool busy;
    private string? error;
    private VariationOrder? order;
    private Request? request; // originating request, for the lineage bar


    // ---- Retitle -------------------------------------------------------------------------------
    // Editing the title in a dialog, at any stage. Held apart from the approve/revise flows on
    // purpose: this moves the wording and nothing else, so it can never be the thing that quietly
    // shifted a figure. Cancelling simply drops the draft — the record is untouched until Save.
    private bool renamingOrder;
    private string renameTitle = "";
    // A refused save has to land inside the dialog — the page banner sits behind the overlay.
    private string? renameError;

    // ---- Open-flags for the editors and dialogs the Actions menu opens -------------------------
    // The panels own their forms; the page owns whether they are open, so one menu can reach them
    // all (and CloseOpenDialogs can drop them before the page moves on to another record).
    private bool editingSections;   // VariationDocumentPanel's narrative editor
    private bool editingEstimate;   // VariationDetailsCard's estimate editor
    private bool revisingValue;     // ApprovedFiguresPanel's revise-value editor
    private bool buildUpDialogOpen; // StagedBuildUpPanel's dialog
    private bool recordingTender;   // RecordAgreedTenderPanel (a dialog)
    private bool linkingRequest;    // OriginatingRequestRepair (a dialog)
    private bool deletingOrder;     // DeleteVariationPanel (a confirm dialog)

    // The Architect's Instructions that cover this variation — the evidence behind its figures, and
    // the thing an Awaiting-AI variation is waiting for.
    private List<ArchitectInstruction> linkedInstructions = new();
    private bool instructionsLoaded;

    private bool returningToQuoting;
    private bool rejectingOrder;   // post-approval reject (reverses commercial writes) — inline panel
    private bool decliningOrder;   // pre-approval decline (plain status move) — confirm modal
    private bool editLinesModalOpen;

    // The approved variation's current lines, shaped for the edit panel to seed its rows.
    // Each row carries the report line it came from, so a save says "re-price this line" rather
    // than "delete them all and add these" — that is what keeps a claimed line's history attached.
    private IReadOnlyList<VariationLineInput> CurrentLineInputs =>
        VariationLines
            .Select(line => new VariationLineInput(
                line.CostCode, line.Description, line.Quantity, line.Rate, line.ValuationLineItemId))
            .ToList();

    // The status pill's choices, in ladder order — rendered by the shared DropdownMenu (see Menus.cs).
    private static readonly VariationOrderStatus[] OrderStatusOptions =
    {
        VariationOrderStatus.Quoting, VariationOrderStatus.Issued,
        VariationOrderStatus.AwaitingArchitectInstruction,
        VariationOrderStatus.Approved, VariationOrderStatus.Rejected
    };


    // ---- Originating-request repair: the candidates the panel offers ----
    private IReadOnlyList<VariationOrder> projectQuotes = Array.Empty<VariationOrder>();

}
