namespace Jewel.JPMS.Contracts.Ai;

/// <summary>Bid packages, work orders and the directory. Data only.</summary>
public static class ProcurementPageGuides
{
    public static readonly IReadOnlyList<PageGuide> Guides = new PageGuide[]
    {
        new("/projects/{project}/bid-package-invites", "Bid package invites",
            "The register of this project's bid packages — title, trade, status, created date; "
            + "closed packages sort last but stay reachable. Manually, \"New bid package\" "
            + "(Admin/MD/PM) takes just a title and trade, creates a Draft and jumps straight to its "
            + "detail page; \"Suggest bid packages\" has the AI read the live valuation report and "
            + "propose trade packages for the remaining works, each ticked suggestion becoming an "
            + "ordinary Draft; there is also an Excel export. You can navigate_to here and open a "
            + "package row's detail route; no dialog is registered on this list route. Building out "
            + "a package's summary, lines, invites, quotes and award all happen on the package's own "
            + "detail page, not here."),

        new("/projects/{project}/bid-package-invites/{bidPackageId}", "Bid package detail",
            "One bid package worked end to end, in five tabs: Details (specification summary plus "
            + "the line schedule, edited together in one \"Edit details\" dialog; each line links to "
            + "a cost centre or a variation), Tender list (add subcontractors from the directory, "
            + "quick-add, \"Find local subcontractors\" web search, and the Invite email composer "
            + "that sends from the projects mailbox with tenderers in BCC), Submissions (\"Extract "
            + "information\" AI-reads a filed tender email against the line schedule, \"Record "
            + "tender manually\", and Award — which raises the work order), Documents and Emails "
            + "(tagged thread with Reply/Forward). You read with get_bid_package_context, "
            + "read_record_emails and read_email_attachment, check list_cost_codes, and open the "
            + "bid_package_details dialog (open_modal) to propose summary and lines in one update. "
            + "The tender_reply composer is page-anchored — update_open_modal only, never "
            + "open_modal."),

        new("/projects/{project}/work-orders", "Work orders",
            "The project's work orders as a financial roll-up — grouped by cost centre or supplier "
            + "(toggle remembered per user), showing committed, paid (from Xero bills), remaining "
            + "and left to invoice, with a supplier search and Excel export. Manually: \"Add work "
            + "order\" raises a manual order (the same modal edits one); drafts await a two-click "
            + "Approve (mints the next WO number and emails the purchase order from the projects "
            + "mailbox) or Reject (terminal); each issued line offers PO (the printable purchase "
            + "order page), Re-code (move or split a line across cost centres without changing the "
            + "order's value) and Cancel (MD/FD only, refused while bills are linked or money "
            + "paid). You can navigate_to here, and two dialogs are registered: work_order_create "
            + "raises a new manual order (Add work order, empty) and work_order_edit corrects an "
            + "existing one (get_work_order_context resolves \"WO-0045\" to the id and reads its "
            + "lines; read_record_emails record_type work_order reads its correspondence). "
            + "Awarding a tender happens on the bid package page; linking invoices happens on WO "
            + "Allocation."),

        new("/projects/{project}/work-orders/{workOrderId}/po", "Purchase order",
            "The printable purchase order for one work order — the sheet as issued, with supplier "
            + "and site addresses, approver and payment terms, plus a status pill (Draft awaiting "
            + "approval, Awaiting supplier acceptance, Accepted, Rejected, Cancelled). Manually: "
            + "\"Print / save PDF\" via the browser dialog, and \"Email to supplier…\" which SENDS the "
            + "covering email now from the shared projects mailbox with the purchase order PDF "
            + "attached (a failed send is left in the mailbox's Drafts folder); the button is hidden "
            + "for draft, rejected and cancelled orders. Below the sheet sits a record-keeping attachments "
            + "panel (quotes, signed copies) that never prints or goes to the supplier. You can "
            + "navigate_to here with a real work-order id. Approving, rejecting, cancelling and "
            + "re-coding are done on the Work Orders tab, not here."),

        new("/projects/{project}/work-order-allocation", "WO Allocation",
            "The tab that ties each Xero purchase line to the work order it pays against — headline "
            + "cards for cost of sales, linked, not linked and orders fully invoiced. Manually: "
            + "expand a work order to see its linked invoice lines and unlink a slice; in the "
            + "invoice-line queue below, pick the order a line pays (options that cannot take its "
            + "value are disabled) or split one line across several orders; linking recodes the "
            + "whole order to the invoice's cost centre, and an order can never be invoiced past its "
            + "value. One search box filters both tables so a supplier's orders and unlinked bills "
            + "line up; there is an Excel export. You can navigate_to here; no dialogs or dedicated "
            + "tools. Unlinked lines count as non-work-order cost of sales on the Financials tab."),

        new("/directory", "Directory",
            "The unified directory — group chips for Clients, Architects, Subcontractors (the "
            + "default, the company directory) and Internal staff. Manually, on the Subcontractors "
            + "group: search and Type filter, click a row to open its record — each row carries a "
            + "Compliance pill (the company's worst current-document status: Missing, Current, "
            + "Expiring soon inside 30 days, or Expired) and a Compliance chip row narrows the list to "
            + "one standing; a Companies | Compliance register tab row leads to /directory/compliance — "
            + "\"+ Add company\" "
            + "(Admin/MD/FD), \"Import from Xero\", and Consolidate — tick two or more records, pick "
            + "the master and the winning value per field; references re-point, losing contact "
            + "details become contacts on the master. Clients and Architects render read-only here "
            + "with links to their own /clients and /architects pages, where creating and editing "
            + "stay; Staff is the user list. You can navigate_to here or straight to a directory "
            + "entry; no dialog is registered on this route."),

        new("/directory/{subcontractorId}", "Directory entry",
            "A subcontractor's master record: company and contact details, Xero-link badge, trades, "
            + "contacts, portal access, statement of account, the CIS verification panel (the HMRC "
            + "result as three fields — status, verification number, verified-on date — written together "
            + "by \"Record verification…\" / record_cis_verification, never squeezed into the status) "
            + "and the compliance document list beneath it. "
            + "Manually: \"Edit details\" (the company name should match the supplier's exact Xero "
            + "name so invoices line up on WO Allocation; payment terms print on their purchase "
            + "orders), trade chips against the curated list, other contacts beyond the primary, \"Link to "
            + "Xero contact…\" (with an optional \"Also pull the contact's details from Xero\") and, once "
            + "linked, \"Push contacts to Xero…\" — a modal showing Xero's primary person and additional "
            + "people NOW beside what they will be AFTER (the record's primary contact and other "
            + "contacts; an empty side here leaves Xero's alone) before anything is sent — \"Invite to "
            + "portal\" (a set-password link "
            + "scoping their login to their own company's data), and the statement of account — "
            + "every work order they hold with invoices claimed against each — downloadable as PDF "
            + "or drafted by email. You can navigate_to here with a real id (tools return "
            + "ready-made routes); no dialog is registered."),

        new("/directory/compliance", "Compliance register",
            "Every directory company's current compliance documents in ONE list, worst first — "
            + "Expired, Expiring soon (inside 30 days), Missing (a company with nothing on file is a "
            + "row), then Current — with search (company, trade, document), a status chip row with "
            + "counts, and an Excel export that can ignore the search and filter. Rows open the "
            + "company's record, where the renewal is filed. The Companies tab leads back to "
            + "/directory; the dashboard's \"Documents expiring\" tile lands here. Same gate as the "
            + "Directory (Admin/MD/FD/PM). You can navigate_to here; no dialog is registered."),

        new("/subcontractors/communications", "Subcontractor communications",
            "The live list of every email tagged with the SubComms family — general subcontractor "
            + "correspondence plus the Chaser, Info request, Materials and H&S categories — read "
            + "from the mailbox newest first, with a chip row narrowing to one category and Load "
            + "more paging. Manually: expand any email to its full body, and Reply or Forward in "
            + "place — sending goes out from the projects mailbox there and then, and the sent copy "
            + "inherits the thread's tags so it files straight back into this list. You can "
            + "navigate_to here. Tagging and untagging are not done here: emails join this list via "
            + "the Control Centre's System Tags, and untagging lives in the Control Centre's Tagged "
            + "view."),
    };
}
