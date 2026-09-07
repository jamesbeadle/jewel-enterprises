
namespace Jewel.JPMS.Features.Triage.Panels;

/// <summary>
/// What the record explorer can search, and how each type reads on screen. One place for the
/// labels so the type picker, the empty states and the open document header can never disagree.
/// </summary>
public static class ExplorerRecordTypes
{
    /// <summary>Every record family the explorer offers — the same set an email can be linked to.</summary>
    public static readonly RecordType[] All =
    {
        RecordType.Request, RecordType.Variation, RecordType.WorkOrder,
        RecordType.BidPackageInvite, RecordType.Defect, RecordType.Inventory, RecordType.SiteInstruction, RecordType.Lad, RecordType.Todo,
        RecordType.CalendarEvent, RecordType.BuildingControlInspection, RecordType.BuildingControlCase
    };

    public static string Label(RecordType type) => type switch
    {
        RecordType.Request => "Requests / RFIs",
        RecordType.Variation => "Variation Orders",
        RecordType.WorkOrder => "Work Orders",
        RecordType.BidPackageInvite => "Bid Package Invites",
        RecordType.Defect => "Defects",
        RecordType.Inventory => "Inventory",
        RecordType.SiteInstruction => "Site instructions",
        RecordType.Lad => "LADs claims",
        RecordType.Todo => "To-do items",
        RecordType.CalendarEvent => "Calendar events",
        RecordType.BuildingControlInspection => "Building Control Inspections",
        RecordType.BuildingControlCase => "Building Control Cases",
        _ => type.ToString()
    };

    /// <summary>The same by bare type + ids — for a reference held on another record (a to-do's
    /// "about" record) where no LinkableRecord projection is to hand.</summary>
    public static string? FullPageHref(RecordType type, string projectId, string recordId) =>
        FullPageHref(new LinkableRecord(type, recordId, projectId, "", "", ""));

    /// <summary>Where a record's own full page lives — the explorer reads, the page edits.</summary>
    public static string? FullPageHref(LinkableRecord record) => record.Type switch
    {
        RecordType.Request => $"/projects/{record.ProjectId}/requests/view/{record.RecordId}",
        RecordType.Variation or RecordType.VariationQuote => $"/projects/{record.ProjectId}/variations/{record.RecordId}",
        RecordType.BidPackageInvite => $"/projects/{record.ProjectId}/bid-package-invites/{record.RecordId}",
        RecordType.WorkOrder => $"/projects/{record.ProjectId}/work-orders",
        RecordType.Defect => $"/projects/{record.ProjectId}/defects/{record.RecordId}",
        RecordType.Inventory => $"/projects/{record.ProjectId}/inventory",
        RecordType.SiteInstruction => $"/projects/{record.ProjectId}/site-instructions",
        RecordType.Scheduling or RecordType.Lad => $"/projects/{record.ProjectId}/programme",
        RecordType.Todo => string.IsNullOrEmpty(record.ProjectId) ? "/todos" : $"/projects/{record.ProjectId}/todos",
        RecordType.CalendarEvent => $"/projects/{record.ProjectId}/calendar?event={record.RecordId}",
        RecordType.BuildingControlInspection => $"/projects/{record.ProjectId}/building-control/inspections/{record.RecordId}",
        RecordType.BuildingControlCase => $"/projects/{record.ProjectId}/building-control",
        _ => null
    };
}
