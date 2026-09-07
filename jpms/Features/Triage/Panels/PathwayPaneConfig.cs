
namespace Jewel.JPMS.Features.Triage.Panels;

/// <summary>
/// What one pathway pane contains (the 2026-08-27 Control Centre restructure): which record types
/// its tagging sections link to, which record-less communication family gives it category
/// registers, and which system actions belong to its side. The four panes are one component
/// (PathwayPane) fed by these four constants, so the pathway split lives in data, not in four
/// hand-written panes.
/// </summary>
public sealed record PathwayPaneConfig(
    string Pathway,
    string Hint,
    IReadOnlyList<RecordType> LinkTypes,
    CommunicationFamily? Family,
    IReadOnlyList<(string Title, IReadOnlyList<SystemActionKind> Kinds)> ActionGroups)
{
    /// <summary>Every action kind this pane offers — for badging staged actions per pathway.</summary>
    public IReadOnlyList<SystemActionKind> AllActionKinds { get; } =
        ActionGroups.SelectMany(group => group.Kinds).ToList();

    public static PathwayPaneConfig Client { get; } = new(
        "Client",
        "The client, or their architect and team",
        new[]
        {
            RecordType.Request, RecordType.Variation,
            RecordType.BuildingControlInspection, RecordType.BuildingControlCase,
            RecordType.Lad, RecordType.ValuationClaim, RecordType.ValuationReportSnapshot
        },
        Family: null,
        new (string, IReadOnlyList<SystemActionKind>)[]
        {
            (SystemActionGuide.RaiseGroup, new[]
            {
                SystemActionKind.RaiseRfi,
                SystemActionKind.RaiseVariationOrder,
                SystemActionKind.RaiseBuildingControlInspection,
            }),
            (SystemActionGuide.MoveGroup, new[]
            {
                SystemActionKind.PromoteRequestToRfi,
                SystemActionKind.ReopenRfi,
                SystemActionKind.CloseRfi,
                SystemActionKind.ApproveVariationOrder,
                SystemActionKind.RejectVariationOrder,
            }),
            // Handing a client-side email to someone at Jewel — a tender enquiry the QS or Sales &
            // Marketing should pick up, say — is a to-do assigned to them (2026-09-03: "Forward to
            // QS" retired, and the same day "Log Tender Enquiry" with it: the assignee sees the
            // email on the to-do). No register of forwards or enquiries is kept.
            (SystemActionGuide.PeopleGroup, new[] { SystemActionKind.CreateTodos }),
        });

    public static PathwayPaneConfig Subcontractor { get; } = new(
        "Subcontractor",
        "A subcontractor — the trades Jewel places work with",
        new[] { RecordType.BidPackageInvite, RecordType.WorkOrder, RecordType.Defect },
        CommunicationFamily.Subcontractor,
        new (string, IReadOnlyList<SystemActionKind>)[]
        {
            (SystemActionGuide.RaiseGroup, new[]
            {
                SystemActionKind.RaiseWorkOrder,
                SystemActionKind.CreateBidPackageInvite,
                SystemActionKind.RaiseDefect,
            }),
            (SystemActionGuide.MoveGroup, new[] { SystemActionKind.FileBidPackageTender }),
            // Logging the trade you're emailing is a subcontractor-side act — the same kind is
            // offered on the Supplier and Internal panes; StagedSystemAction.Pathway keeps the
            // badges honest about which pane staged it.
            (SystemActionGuide.PeopleGroup, new[] { SystemActionKind.AddDirectoryContact }),
        });

    public static PathwayPaneConfig Supplier { get; } = new(
        "Supplier",
        "A materials or goods supplier, as distinct from a subcontractor",
        // Inventory (2026-08-28) is the pane's first linkable record type: goods for the job —
        // what the product is, where it's kept. Purchase orders split from work orders remain a
        // later phase.
        // Defects joined it 2026-09-07 (James): faulty goods and short deliveries are put right by
        // the MERCHANT, not a trade, so a defect must be raisable and taggable from the supplier's
        // own email. The record is the same DEF-#### on the same Defects register — a defect names
        // the directory company it is raised with, and that company can be either category.
        new[] { RecordType.Inventory, RecordType.Defect },
        CommunicationFamily.Supplier,
        new (string, IReadOnlyList<SystemActionKind>)[]
        {
            (SystemActionGuide.RaiseGroup, new[]
            {
                SystemActionKind.AddInventoryItem,
                SystemActionKind.RaiseDefect,
                SystemActionKind.RaiseCalendarEvent,
            }),
            (SystemActionGuide.PeopleGroup, new[] { SystemActionKind.AddDirectoryContact }),
        });

    // Bid packages and work orders came OUT of the Internal link types in this restructure
    // (Nigel, 2026-08-27): they are subcontractor records and live on the Subcontractor pane.
    // Site instructions (2026-09-03, James) became a real record here — like a to-do, raised
    // with written detail in Actions and tagged to from the Tagging list — replacing the
    // record-less "Tag as Site instruction" category tick, which said nothing about what the
    // instruction was.
    public static PathwayPaneConfig Internal { get; } = new(
        "Internal",
        "Jewel staff — company admin",
        new[] { RecordType.Todo, RecordType.SiteInstruction, RecordType.CalendarEvent },
        CommunicationFamily.Internal,
        new (string, IReadOnlyList<SystemActionKind>)[]
        {
            (SystemActionGuide.RaiseGroup, new[]
            {
                SystemActionKind.RaiseSiteInstruction,
                SystemActionKind.RaiseCalendarEvent,
            }),
            (SystemActionGuide.PeopleGroup, new[]
            {
                SystemActionKind.CreateTodos,
                SystemActionKind.CompleteTodo,
                SystemActionKind.AddDirectoryContact,
                // Administrators only (SystemActionGuide.AdministratorOnly) — the section
                // drops it from the dropdown for every other role.
                SystemActionKind.MarkAsKpi,
            }),
        });

    public static IReadOnlyList<PathwayPaneConfig> All { get; } =
        new[] { Client, Subcontractor, Supplier, Internal };

    /// <summary>Singular UI label for a link type's section — Relevant Event (not Scheduling),
    /// LADs claim, Variation Order (one record): the same terminology map the old System Tags
    /// pane carried.</summary>
    public static string TypeLabel(RecordType type) => type switch
    {
        RecordType.Request => "Request / RFI",
        RecordType.Variation => "Variation Order",
        RecordType.Lad => "LADs claim",
        RecordType.ValuationClaim => "Valuation claim",
        RecordType.ValuationReportSnapshot => "Valuation report snapshot",
        RecordType.Scheduling => "Relevant Event",
        RecordType.BidPackageInvite => "Bid Package Invite",
        RecordType.WorkOrder => "Work Order",
        RecordType.Defect => "Defect",
        RecordType.Inventory => "Inventory item",
        RecordType.SiteInstruction => "Site instruction",
        RecordType.Todo => "To-do item",
        RecordType.CalendarEvent => "Calendar event",
        RecordType.BuildingControlInspection => "Building Control Inspection",
        RecordType.BuildingControlCase => "Building Control Case",
        RecordType.SubcontractorComms => "Subcontractor communication",
        RecordType.SupplierComms => "Supplier communication",
        RecordType.InternalComms => "Internal communication",
        _ => type.ToString()
    };

    /// <summary>Plural section title ("Requests / RFIs") — light-touch, keyed where a bare "s"
    /// doesn't read.</summary>
    public static string TypeLabelPlural(RecordType type) => type switch
    {
        RecordType.Request => "Requests / RFIs",
        RecordType.Lad => "LADs claims",
        _ => TypeLabel(type) + "s"
    };
}
