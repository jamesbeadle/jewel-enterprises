using System.ComponentModel.DataAnnotations;

namespace Jewel.JPMS.Api.Data.Entities;

/// <summary>
/// One work order's share of an allocated Xero purchase line — the line-to-order tie
/// behind the Financials tab and WO Allocation tab. A bill that pays one order carries
/// a single row for its full net; a bill paying several orders at once (a subcontractor
/// invoicing a main order plus variation orders together) carries one row per order.
/// Amount is signed like the line's net (credit notes negative). A hand link (WO Allocation
/// tab) carries no cost code and sits on a line allocated whole to the order's project, or
/// split across cost centres on that one project. A Work Order bill approval (2026-09-09)
/// writes one row per share — order AND cost code — so a line split across two orders on
/// the same project still says which order paid which centre; the unique (line, order,
/// code) index stops the same share being written twice.
/// </summary>
public sealed class XeroLineWorkOrderLinkEntity
{
    [Key, MaxLength(64)] public string XeroLineWorkOrderLinkId { get; set; } = "";
    [MaxLength(140)]     public string XeroLedgerLineId { get; set; } = "";
    [MaxLength(64)]      public string WorkOrderId { get; set; } = "";
    [MaxLength(64)]      public string ProjectId { get; set; } = "";
    [MaxLength(32)]      public string? CostCenterCode { get; set; }
    public decimal Amount { get; set; }
}
