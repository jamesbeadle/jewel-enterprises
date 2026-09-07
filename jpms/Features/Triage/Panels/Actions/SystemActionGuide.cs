namespace Jewel.JPMS.Features.Triage.Panels;

/// <summary>
/// How the System Actions dropdown reads (Nigel, 2026-08-22): fourteen flat rows left him unsure
/// whether an incoming tender was a Raise RFI, a File Bid Package Tender or a to-do. The rows are
/// grouped by what they do to the system, and the chosen action says in one line when it's the
/// right one.
/// </summary>
public static class SystemActionGuide
{
    public const string RaiseGroup = "Raise something new";
    public const string MoveGroup = "Move an existing record on";
    public const string PeopleGroup = "People and to-dos";

    /// <summary>Dropdown groups, in display order, each with its actions in Nigel's order.</summary>
    public static IReadOnlyList<(string Title, IReadOnlyList<SystemActionKind> Kinds)> Groups { get; } =
        new (string, IReadOnlyList<SystemActionKind>)[]
        {
            (RaiseGroup, new[]
            {
                SystemActionKind.RaiseRfi,
                SystemActionKind.RaiseVariationOrder,
                SystemActionKind.RaiseWorkOrder,
                SystemActionKind.CreateBidPackageInvite,
                SystemActionKind.RaiseDefect,
                SystemActionKind.AddInventoryItem,
                SystemActionKind.RaiseSiteInstruction,
                SystemActionKind.RaiseCalendarEvent,
                SystemActionKind.RaiseBuildingControlInspection,
            }),
            (MoveGroup, new[]
            {
                SystemActionKind.PromoteRequestToRfi,
                SystemActionKind.ReopenRfi,
                SystemActionKind.CloseRfi,
                SystemActionKind.ApproveVariationOrder,
                SystemActionKind.RejectVariationOrder,
                SystemActionKind.FileBidPackageTender,
            }),
            (PeopleGroup, new[]
            {
                SystemActionKind.CreateTodos,
                SystemActionKind.CompleteTodo,
                SystemActionKind.AddDirectoryContact,
                SystemActionKind.MarkAsKpi,
            }),
        };

    /// <summary>The kinds only an administrator is offered (2026-09-03) — PathwayActionsSection
    /// drops them from the dropdown for every other active role. One list, so the Internal pane's
    /// config can name the kind without repeating the gate.</summary>
    public static readonly IReadOnlyList<SystemActionKind> AdministratorOnly = new[] { SystemActionKind.MarkAsKpi };

    public static string WhenToUse(SystemActionKind kind) => kind switch
    {
        SystemActionKind.RaiseRfi => "A question for the client side that needs a formal, numbered answer — an architect's detail, a spec gap, a sequencing clash.",
        SystemActionKind.RaiseVariationOrder => "The client side has asked for, or caused, extra or changed work that needs pricing and approval.",
        SystemActionKind.RaiseWorkOrder => "You're placing work with a subcontractor — the email is the agreed scope or price; Apply emails them the purchase order.",
        SystemActionKind.CreateBidPackageInvite => "A package of work you're about to put out to tender — sets up the package so subcontractors can be invited.",
        SystemActionKind.RaiseDefect => "Something the company you're emailing has to put right — a trade's workmanship, or a merchant's faulty or short-delivered goods. Logs it on the project's Defects tab against that company, where it is sent to them and chased.",
        SystemActionKind.AddInventoryItem => "Goods for the job worth keeping on the books — what the product is and where it's kept. Adds it to the project's Inventory tab with this email filed to it.",
        SystemActionKind.RaiseSiteInstruction => "This email calls for an instruction to site — write what site is to do and where. The email alone is rarely the instruction, so the words you write here ARE the record; it goes on the project's Site Instructions page with this email filed to it. (An instruction already raised is tagged in the Tagging tab instead.)",
        SystemActionKind.RaiseCalendarEvent => "This email is arranging something dated — a site visit, a delivery, a meeting, subcontractor attendance. Puts it on the project's Calendar tab so everyone sees it coming.",
        SystemActionKind.RaiseBuildingControlInspection => "The building control inspector arranging or confirming a visit — logs the stage on the project's Building Control tab and files their thread against it. (An inspection already logged is tagged in System Tags instead.)",
        SystemActionKind.PromoteRequestToRfi => "An older General request that has turned out to need a formal RFI number.",
        SystemActionKind.ReopenRfi => "An answered RFI that this email reopens — the answer was wrong, or more has come up.",
        SystemActionKind.CloseRfi => "This email answers an open RFI — file the answer and close it.",
        SystemActionKind.ApproveVariationOrder => "The client side has approved a variation in this email.",
        SystemActionKind.RejectVariationOrder => "The client side has turned a variation down in this email.",
        SystemActionKind.FileBidPackageTender => "A subcontractor has returned pricing for a package you invited them to. (An architect inviting Jewel to tender is a to-do for whoever will price it, not this.)",
        SystemActionKind.CreateTodos => "Something for someone at Jewel to do — the catch-all when no record fits.",
        SystemActionKind.CompleteTodo => "This email shows a to-do is done — tick it off.",
        SystemActionKind.AddDirectoryContact => "A new supplier, subcontractor or contact to keep on file from this email.",
        SystemActionKind.MarkAsKpi => "This email is evidence of how someone at Jewel is performing — good or bad. Files it as a KPI under that person in the administrators-only register (Admin → KPI emails). Nothing is tagged in the mailbox; nobody else sees the mark.",
        _ => ""
    };
}
