namespace Jewel.JPMS.Contracts.Ai;

/// <summary>Finance, Xero and the money reports. Data only.</summary>
public static class FinancePageGuides
{
    public static readonly IReadOnlyList<PageGuide> Guides = new PageGuide[]
    {
        new("/finance/allocation", "Cost allocation",
            "The workbench that reconciles Xero with the projects: every cost-of-sales purchase "
            + "line (nominal accounts starting 3) is allocated to a project and master cost centre, "
            + "or split across several. The user works tabs — Unallocated, one tab per project, "
            + "Labour, Allocated, Buckets, Disputed, Ignored — with per-row Allocate, Set/Unset, "
            + "Split…, Allocate to bucket, Ignore and Dispute…, plus bulk bars, an \"Allocate all "
            + "matched\" banner, Sync from Xero, Re-check matches, search and Excel export; the "
            + "Allocated tab adds Send to cost centre…, Send to project… and Undo. The Labour tab "
            + "holds bills whose supplier matches a worker on the labour registry (by worker or "
            + "linked subcontractor company name): those are settlement of approved timesheets, "
            + "not costs to allocate — the user marks each as settlement of its month (a §6 "
            + "timesheet cover; \"Allocate all matched\" skips them) and the Labour overview's "
            + "Settlement view reconciles and codes them into Xero. The Work Order bills tab holds "
            + "bills from suppliers with an open work order, one card per bill pre-filled from the "
            + "order (a bill that pays two orders takes a figure per order off the bill total — "
            + "proposed when the lines name their orders, typed on the card otherwise — and the Xero "
            + "lines are left exactly as the supplier raised them) — one Approve allocates, links "
            + "each share to its order, writes the tracking to Xero and approves the bill there. "
            + "Allocating every line of a draft bill by hand does the same confirm-and-approve. You can "
            + "navigate_to here and read list_cost_codes / list_projects for valid codes and "
            + "projects; none of this page's dialogs are registered, so allocation itself is the "
            + "user's act."),

        new("/finance/cash-forecast", "Cash Forecast",
            "The company time-phased cash forecast: every known future cash movement placed in its "
            + "expected month, with a directors-only KPI strip and closing-bank-balance row seeded "
            + "from Xero, lowest month flagged. The user filters with a project multi-select "
            + "(defaults to live jobs), expands cash-in/cash-out categories to per-project lines, "
            + "and edits three things inline: the monthly company overheads default, per-month "
            + "overhead overrides, and — on the Future valuations rows — each project's next "
            + "expected valuation date and expected monthly valuation. Below the divider sits the "
            + "Position to Completion statement; Excel export covers the lot. Phased months tie to "
            + "each project's Cashflow tab to the penny. You can navigate_to here; nothing on this "
            + "page is a registered dialog. Per-project statements live on "
            + "/projects/{project}/cashflow, not here.",
            Aliases: new[] { "/finance", "/finance/cash-summary" }),

        new("/finance/weekly-cashflow", "Weekly Cashflow",
            "The accountant's live 13-week payment plan (directors and Accounts): every outstanding "
            + "Xero bill and sales invoice seeded into the week of its due date — or its Xero Planned "
            + "(bills) / Expected (invoices) date when one is set — plus the manual items Xero can't "
            + "see (subcontractors, staff, subscriptions, direct debits, other), one column per week "
            + "with anything overdue in the current week and a Later column beyond the horizon. "
            + "Directors' tiles: cash in bank, to pay this week, the lowest week and the horizon-end "
            + "balance; Accounts sees to pay this week plus cash out and cash in over the 13 visible "
            + "weeks instead (the bank position is directors only). The user moves any entry to the "
            + "week it will really be paid "
            + "with ‹ › on its cell (↺ returns it; ‣ marks a moved entry), groups suppliers into one "
            + "line via Group suppliers (a group row is one line — its bill count is in the hover "
            + "text, and its ‹ › move every bill in the cell), excludes an entry already covered "
            + "elsewhere with ⊘ (parked struck-through, uncounted), and adds or edits manual items "
            + "with Add item — the toolbar button, or the standing Add item row at the foot of Cash "
            + "out. A manual item is an OUTGOING Xero can't see yet (a supplier's invoice that hasn't "
            + "landed, overheads, a wages run), one-off or recurring; its band shows only while it "
            + "holds a live item, so an emptied band disappears until the next item is added. There "
            + "is no manual receipt — an expected receipt is its sales invoice in Xero at its Expected "
            + "date. Moves change WHEN, never HOW MUCH, and are shared with the whole team. "
            + "The Excel export is the grid on paper: a Weekly plan tab with one line per supplier "
            + "(groups honoured) and a column per week plus band totals, net movement and the "
            + "directors' closing balance; a Detail tab opening every line into its bills with due "
            + "and expected dates; a Data tab as a flat list for pivoting — a shaded amount is one "
            + "the accountant moved. Read the grid as the page shows it with "
            + "get_weekly_cashflow_grid (one line per supplier, amounts per week, moved flags; "
            + "includeEntries for the bills behind a line) and the raw overlay with "
            + "get_weekly_cashflow_plan; the plan's writes are the perform_action weekly-cashflow "
            + "suite (create/update/archive item, place entry, set exclusion, save/remove supplier "
            + "group). Real payment agreements belong in Xero as the bill's Planned date or the "
            + "invoice's Expected date — a portal placement is week-to-week juggling. Ageing and the "
            + "drafts-included totals live on /finance/aged-payables and /finance/aged-receivables."),

        new("/finance/profit-summary", "Profit Summary",
            "Gross profit by project, three ways: the Running profit by month grid (project-to-date "
            + "running % per month end, Xero site P&L, invoiced basis), summary tiles (budgeted "
            + "profit, forecast profit, biggest swing vs the deal), the budget→forecast bridge, and "
            + "the table in bands — the deal as signed, current position (certified against "
            + "allocated Xero spend), to finish, and forecast at completion. Trajectory and "
            + "cumulative-position panels sit under the table, with a Refresh button re-pulling the "
            + "stored Xero site P&L. The user filters with the project multi-select and exports to "
            + "Excel. You can navigate_to here and read list_projects; the page is read-only bar "
            + "the filter and Refresh. The certified-basis table and invoiced-basis panels "
            + "deliberately differ."),

        new("/finance/xero", "Xero Transactions",
            "Purchase invoices read live from Xero, each line carrying its Xero site and cost code. "
            + "Two views behind a tab toggle: Transactions — search by supplier/number/reference, "
            + "status chip filters, click a row to expand its line detail — and Site × cost code, a "
            + "net-spend pivot per calendar year on the accountant's basis (paid, authorised or "
            + "awaiting approval, minus credit notes; drafts excluded). The toolbar has Refresh "
            + "from Xero and Excel export; a banner warns when the fetch cap truncates totals. You "
            + "can navigate_to here to show the raw Xero picture. Nothing is edited or allocated on "
            + "this page — coding lines to projects and cost centres happens on /finance/allocation."),

        new("/finance/aged-payables", "Aged Payables",
            "Everything owed to suppliers, aged exactly as Xero's report ages it but including "
            + "draft bills still being coded — the only complete payables picture, since the "
            + "accounting procedure leaves bills in draft until coded through the portal. Tiles "
            + "show total payables, the draft slice, and overdue; the table is one row per "
            + "supplier, expandable to the bills behind it with Draft and Credit badges, and a "
            + "toggle switches ageing between due date and invoice date. Refresh from Xero and "
            + "Excel export in the toolbar. You can navigate_to here to answer \"who do we owe and "
            + "how old is it\". Nothing is paid, coded or edited here; coding draft bills happens "
            + "via /finance/allocation."),

        new("/finance/aged-receivables", "Aged Receivables",
            "Everything clients owe, aged exactly as Xero's report ages it but including draft "
            + "sales invoices still being prepared — the sales-side mirror of Aged Payables. Tiles "
            + "show total receivables, the draft slice, and overdue; the table is one row per "
            + "client, expandable to the invoices behind it with Draft and Credit badges, and a "
            + "toggle switches ageing between due date (Xero's default) and invoice date. Refresh "
            + "from Xero and Excel export in the toolbar. You can navigate_to here to answer \"who "
            + "owes us and how old is it\". Invoices are not raised, chased or edited on this page "
            + "— it is a read-only Xero snapshot."),

        new("/finance/payment-certificates", "Payment Certificates",
            "The payment certificate register — what the client is paying, certified — "
            + "company-wide with rows grouped by project in the standing live-work order and a "
            + "per-group certified total. Each row shows the certificate number, issued date, the "
            + "valuation claim it certifies (when tied), the certified amount and the file, with "
            + "Preview for PDFs inline and Download for everything. The only control is the "
            + "project filter. You can navigate_to here and read list_projects to resolve the "
            + "filter. Certificates are not created here: they arrive filed from Document Triage, "
            + "and the valuations they certify live on each project's valuation report."),

        new("/cost-codes", "Cost codes",
            "The global cost-centre master that every project's financials, valuation report and "
            + "invoice allocations group by, in three tabs: Our cost codes (the master — New cost "
            + "code button, per-row Edit and Retire/Reinstate, a Show retired toggle, Excel export; "
            + "retired codes keep their historical allocations), Xero sites and Xero cost codes "
            + "(the options of Xero's tracking categories spelt exactly as Xero holds them, with a "
            + "Refresh from Xero button). You read the same data with list_cost_codes — use that "
            + "for exact code spellings rather than guessing — and can navigate_to here. The "
            + "New/Edit dialogs are not registered, so the user fills them by hand."),

        new("/rate-library", "Rate library",
            "The rate library — priced rates by trade, each with description, supplier, unit, rate "
            + "and last-priced date, with the header counting total rates and how many are stale "
            + "(not priced in over 60 days). The page is a read-only register: the only actions are "
            + "Excel export and the \"View stale rates\" link. You can navigate_to here when the "
            + "user wants current pricing by trade or supplier. Rates are not edited on this page, "
            + "and there is no dialog registered here."),

        new("/rate-library/stale", "Stale rates",
            "The rate library's stale slice: rates not priced in over 60 days, in the same table "
            + "shape as the main library, with the instruction to re-price before the next tender. "
            + "The only actions are Excel export and the breadcrumb back to /rate-library. You can "
            + "navigate_to here when the user asks which rates need re-pricing. Nothing is edited "
            + "here."),

        new("/projects/{project}/financials", "Project Financials",
            "This project's cost ledger by cost centre: contract sales value, % complete, target "
            + "cost, committed work orders, actual cost of sales (Xero spend allocated on "
            + "/finance/allocation), drawdown/overspend split by sign, and forecasted cost of "
            + "sales. Figures click through to modals — valuation lines, work orders, and invoices, "
            + "where an invoice can be linked to a work order or moved to another centre; the user "
            + "also edits cost % complete inline, locks finalised lines (realising drawdown to "
            + "profit/loss), rolls ticked rows into named groups, and manages reconciliation "
            + "packages below. You can navigate_to here and read list_cost_codes for the codes the "
            + "rows group by. None of these dialogs are registered — every edit is the user's."),

        new("/projects/{project}/cashflow", "Project Cashflow",
            "This project's to-completion cash statement: the project claim, cash allocated "
            + "(received plus retention held), left to claim, then the cash still to move — cost "
            + "centre drawdowns, uninvoiced work orders, unpaid Xero purchase invoices — through "
            + "retention releases 1 and 2 to the practical and project completion cashflow totals. "
            + "Separate cards show overspends available to buy back and, dashed, the potential from "
            + "unapproved variations. The page is read-only; its inputs are edited elsewhere "
            + "(Financials tab, valuation report, retention terms on Settings, /finance/allocation). "
            + "You can navigate_to here and read list_variations for the unapproved variations in "
            + "the potential card. Spreading these figures across months, company-wide, is "
            + "/finance/cash-forecast, not this page."),
    };
}
