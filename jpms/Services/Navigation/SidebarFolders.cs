
namespace Jewel.JPMS.Services.Navigation;

/// <summary>
/// The sidebar's folders (docs/Financial-Reports-and-Nav-Refactor-Plan.md §1, restructure of
/// 2026-08-11 — successor to the 2026-08-04 grouping). The job first (Project — the full record
/// chain from Requests through Variations to the snapshots sent out), then the subcontractor
/// relationship, the company's own running lists (Internal), time on site (Time), the working
/// money (Finance), the read-only money (Financial Reports), the Xero screens (one home per
/// screen), oversight (Audit) and finally the system itself (Admin). Folders mix scopes
/// deliberately — project-scoped rows ("/projects/{project}/…" templates) and company rows sit
/// side by side where the work does. A row that belongs to no folder at all — the Control Centre,
/// Document Triage, Xero Cost Allocation and the Valuation Report — lives in
/// SidebarFolders.Standalone.
///
/// The retired Client folder's rows (Requests, Architect's Instructions, Valuation Report
/// Snapshots) moved into Project: with only directors using the system there is no external
/// audience to group "as the client sees it" for, and the chain reads better in one folder.
/// </summary>
public enum SidebarFolder
{
    Project,
    Subcontractor,
    Supplier,
    Internal,
    Time,
    Finance,
    FinancialReports,
    Xero,
    Audit,
    Sales,
    Admin
}

/// <summary>One sidebar row: a destination plus the roles that may see it. The whole catalog is
/// currently clamped to the directors (DesktopNavigation.DirectorRoles) — decision 2026-08-11:
/// only the MD, FD and administrators use the system, every other role sees Home alone until its
/// own nav is designed. Administrators bypass every gate (DesktopNavigation.CanSee). API
/// authorisation is untouched by the clamp — this is nav visibility, not permission.</summary>
public sealed record SidebarRow(NavigationItem Item, IReadOnlyList<Role> VisibleTo);

/// <summary>A folder in the catalog: a collapsible header in the sidebar, one icon in the
/// collapsed rail, one destination card on the role landing page. The IconKey is a "#…" group
/// key resolved by NavIcon — never navigated to.</summary>
public sealed record SidebarFolderInfo(
    SidebarFolder Folder,
    string Label,
    string IconKey,
    IReadOnlyList<SidebarRow> Rows);

public static class SidebarFolders
{
    // Page slugs are unchanged from the old grouping so existing links and bookmarks keep working
    // (the standing convention: labels move, slugs don't). Role sets live on DesktopNavigation —
    // the single home for nav RBAC — so the catalog and the gates can never drift apart.
    public static readonly IReadOnlyList<SidebarFolderInfo> All = new[]
    {
        // ---- Project: the day-to-day running of the picked job, led by the record chain —
        // Request → RFI → Variation is one lifecycle, split across the RFIs page and the
        // Variation Orders page (2026-08-14). First folder, so its first row (RFIs) is the
        // bare-project-URL landing. ----
        new SidebarFolderInfo(
            SidebarFolder.Project,
            "Project",
            "#project",
            new[]
            {
                // The RFI register (RFIs leading, the legacy General requests one tab behind —
                // split from the combined document register 2026-08-14). Exact on the base route:
                // the variations pages belong to the row below, so this row matches the register's
                // other views and the request detail pages explicitly. The slug stays /requests
                // (the standing convention: labels move, slugs don't).
                new SidebarRow(new NavigationItem("RFIs", "/projects/{project}/requests",
                        new[]
                        {
                            "/projects/{project}/requests/all",
                            "/projects/{project}/requests/general",
                            "/projects/{project}/requests/rfis",
                            "/projects/{project}/requests/view"
                        }, ExactMatch: true),
                    DesktopNavigation.DirectorRoles),
                // The variation book, on its own page since 2026-08-14 (plus the variation detail
                // pages and the legacy routes — /requests/variations from its register-tab days
                // and /voq — kept for links already sent out).
                new SidebarRow(new NavigationItem("Variation Orders", "/projects/{project}/variations",
                        new[] { "/projects/{project}/requests/variations", "/projects/{project}/voq" }),
                    DesktopNavigation.DirectorRoles),
                // The formal instructions that authorise varied work — what a variation at
                // Awaiting AI is waiting for.
                new SidebarRow(new NavigationItem("Architect's Instructions", "/projects/{project}/architect-instructions"),
                    DesktopNavigation.DirectorRoles),
                // Point-in-time captures of issued valuation reports — what the client was
                // actually sent, frozen.
                new SidebarRow(new NavigationItem("Valuation Report Snapshots", "/projects/{project}/valuation-snapshots"),
                    DesktopNavigation.DirectorRoles),
                new SidebarRow(new NavigationItem("Documents", "/projects/{project}/documents"),
                    DesktopNavigation.DirectorRoles),
                new SidebarRow(new NavigationItem("Programme", "/projects/{project}/programme"),
                    DesktopNavigation.DirectorRoles),
                // What is coming up on site — visits, deliveries, meetings, attendance. Events
                // carry CAL-#### references (mail tag stems) and a client-visible flag ready for
                // the client surface when that access is built.
                new SidebarRow(new NavigationItem("Calendar", "/projects/{project}/calendar"),
                    DesktopNavigation.DirectorRoles),
                // The project-specific to-do view — second way in, alongside Internal's master list.
                new SidebarRow(new NavigationItem("To-do", "/projects/{project}/todos"),
                    DesktopNavigation.DirectorRoles),
                new SidebarRow(new NavigationItem("Progress", "/projects/{project}/progress"),
                    DesktopNavigation.DirectorRoles),
                // The defect register (DEF-#### references). Defects are raised here or from a
                // subcontractor email in the Control Centre; each reads its mail back live by tag.
                new SidebarRow(new NavigationItem("Defects", "/projects/{project}/defects"),
                    DesktopNavigation.DirectorRoles),
                // The inventory register (INV-#### references) — goods held for the job, product
                // + location details. Items are added here or from a supplier email in the
                // Control Centre; each reads its mail back live by tag.
                new SidebarRow(new NavigationItem("Inventory", "/projects/{project}/inventory"),
                    DesktopNavigation.DirectorRoles),
                // The site-instruction register (SI-#### references, 2026-09-03) — written
                // instructions to site: title, the instruction, where it applies. Raised here or
                // from an email in the Control Centre's Internal pane; each reads its mail back
                // live by tag. Replaced the record-less Internal → Site Instructions tag register.
                new SidebarRow(new NavigationItem("Site Instructions", "/projects/{project}/site-instructions"),
                    DesktopNavigation.DirectorRoles),
                // The statutory sign-off trail — the case with the building control body and its
                // BCI-#### inspection stages (plus each inspection's own detail page).
                new SidebarRow(new NavigationItem("Building Control", "/projects/{project}/building-control"),
                    DesktopNavigation.DirectorRoles),
                new SidebarRow(new NavigationItem("Communications", "/projects/{project}/communications"),
                    DesktopNavigation.DirectorRoles),
                // Internal-only titled free-text notes for the office — door codes, key safes,
                // site access. Every internal role reads AND edits (the API's
                // UsefulInformationRoles); external roles never see them.
                new SidebarRow(new NavigationItem("Useful Information", "/projects/{project}/useful-information"),
                    DesktopNavigation.DirectorRoles),
                new SidebarRow(new NavigationItem("Project Settings", "/projects/{project}/settings"),
                    DesktopNavigation.DirectorRoles)
            }),

        // ---- Subcontractor: the subcontractor relationship — inviting bids, placing the orders,
        // and the correspondence. Work Orders moved back in from Finance (2026-08-11, reversing
        // 2026-08-04): placing the order is done WITH the subcontractor; paying for it stays
        // under Finance as WO Allocation. ----
        new SidebarFolderInfo(
            SidebarFolder.Subcontractor,
            "Subcontractor",
            "#subcontractor",
            new[]
            {
                new SidebarRow(new NavigationItem("Bid Package Invites", "/projects/{project}/bid-package-invites"),
                    DesktopNavigation.DirectorRoles),
                new SidebarRow(new NavigationItem("Work Orders", "/projects/{project}/work-orders"),
                    DesktopNavigation.DirectorRoles),
                // General subcontractor correspondence — every email tagged "JPMS/SubComms" at
                // triage (the Control Centre Subcontractor pane's communication tick), read live.
                new SidebarRow(new NavigationItem("Communications", "/subcontractors/communications", ExactMatch: true),
                    DesktopNavigation.DirectorRoles),
                // The category registers (2026-08-27 restructure): each record-less category reads
                // like any other register — the Communications page with that chip preselected.
                new SidebarRow(new NavigationItem("Chasers", "/subcontractors/communications/chaser"),
                    DesktopNavigation.DirectorRoles),
                new SidebarRow(new NavigationItem("Info Requests", "/subcontractors/communications/info-request"),
                    DesktopNavigation.DirectorRoles),
                new SidebarRow(new NavigationItem("H&S", "/subcontractors/communications/h-s"),
                    DesktopNavigation.DirectorRoles)
            }),

        // ---- Supplier: materials and goods suppliers, split from subcontractors 2026-08-27
        // ("materials are mainly supplier") — the SupComms registers, Materials first. ----
        new SidebarFolderInfo(
            SidebarFolder.Supplier,
            "Supplier",
            "#supplier",
            new[]
            {
                // General supplier correspondence — every email tagged "JPMS/SupComms" at triage
                // (the Control Centre Supplier pane's communication tick), read live.
                new SidebarRow(new NavigationItem("Communications", "/suppliers/communications", ExactMatch: true),
                    DesktopNavigation.DirectorRoles),
                // Materials kept its SubComms-Mats tag stem in the move, so mail tagged before the
                // split reads back here too.
                new SidebarRow(new NavigationItem("Materials", "/suppliers/communications/materials"),
                    DesktopNavigation.DirectorRoles),
                new SidebarRow(new NavigationItem("Finishes", "/suppliers/communications/finishes"),
                    DesktopNavigation.DirectorRoles)
            }),

        // ---- Internal: the company's own running lists — the master to-do and the people
        // directory. ----
        new SidebarFolderInfo(
            SidebarFolder.Internal,
            "Internal",
            "#internal",
            new[]
            {
                // The master to-do list: all projects plus company-wide items, with a project filter.
                new SidebarRow(new NavigationItem("Todo", "/todos"),
                    DesktopNavigation.DirectorRoles),
                // The Tender Enquiries register (TEQ-####, 2026-08-25) was retired on 2026-09-03:
                // an architect's invitation to tender is handled as a to-do (James: "we will just
                // do this through todo"), so nothing here lists them any more.
                // Everyone the company deals with — Clients · Architects · Subcontractors · Staff.
                new SidebarRow(new NavigationItem("Directory", "/directory"),
                    DesktopNavigation.DirectorRoles),
                // Staff-to-staff correspondence — every email tagged "JPMS/IntComms" at triage
                // (the Control Centre Internal pane's communication ticks), read live.
                new SidebarRow(new NavigationItem("Communications", "/internal/communications", ExactMatch: true),
                    DesktopNavigation.DirectorRoles),
                // The Internal category registers are gone: Build-ups and Spec Notes retired
                // 2026-08-27 (too fine-grained), Site Instructions became a per-project record
                // 2026-09-03 (Project folder → Site Instructions). Old category-tagged mail
                // still reads via the Tagged view's tag search.
                // The Monday replacement: insurances, subscriptions, vans, trade accounts.
                new SidebarRow(new NavigationItem("Registers", "/registers"),
                    DesktopNavigation.DirectorRoles),
                // Staff sign-off forms: NDAs, policies, H&S acknowledgements.
                new SidebarRow(new NavigationItem("Policies", "/policies"),
                    DesktopNavigation.DirectorRoles)
            }),

        // ---- Time: timesheets — labour recorded on the picked site, and the company-wide
        // worker registry it draws from. ----
        new SidebarFolderInfo(
            SidebarFolder.Time,
            "Time",
            "#time",
            new[]
            {
                new SidebarRow(new NavigationItem("Labour overview", "/labour/overview"),
                    DesktopNavigation.DirectorRoles),
                new SidebarRow(new NavigationItem("Labour", "/projects/{project}/labour"),
                    DesktopNavigation.DirectorRoles),
                new SidebarRow(new NavigationItem("Workers", "/labour/workers"),
                    DesktopNavigation.DirectorRoles),
                new SidebarRow(new NavigationItem("Xero mapping", "/labour/xero-mapping"),
                    DesktopNavigation.DirectorRoles)
            }),

        // ---- Finance: the money that is worked, not read — the picked project's cost ledger,
        // the subcontractor spend allocation that feeds it, and the coding setup. The Xero
        // screens have their own folder below; the read-only statements live in Financial
        // Reports. Cost Codes and Rates split into their own rows (2026-08-11). ----
        new SidebarFolderInfo(
            SidebarFolder.Finance,
            "Finance",
            "#finance",
            new[]
            {
                new SidebarRow(new NavigationItem("Financials", "/projects/{project}/financials"),
                    DesktopNavigation.DirectorRoles),
                new SidebarRow(new NavigationItem("WO Allocation", "/projects/{project}/work-order-allocation"),
                    DesktopNavigation.DirectorRoles),
                // The payment certificate register — what the client is paying, certified, filed
                // from Document Triage. Company page with a project filter (viewable by project).
                new SidebarRow(new NavigationItem("Payment Certificates", "/finance/payment-certificates"),
                    DesktopNavigation.DirectorRoles),
                new SidebarRow(new NavigationItem("Cost Codes", "/cost-codes"),
                    DesktopNavigation.DirectorRoles),
                new SidebarRow(new NavigationItem("Rates", "/rate-library"),
                    DesktopNavigation.DirectorRoles)
            }),

        // ---- Financial Reports: the money that is read — the picked project's statement, then
        // the company-wide views. The Valuation Report moved to the top level (Standalone); the
        // aged views moved to the Xero folder (2026-08-11). ----
        new SidebarFolderInfo(
            SidebarFolder.FinancialReports,
            "Financial Reports",
            "#financial-reports",
            new[]
            {
                // The per-project to-completion statement — "if this job runs to the end, where
                // does its cash land". Renamed from "Cashflow" (2026-08-11): the company view
                // next door is the one that carries the time axis.
                new SidebarRow(new NavigationItem("Project Cashflow", "/projects/{project}/cashflow"),
                    DesktopNavigation.DirectorRoles),
                // The company Cash Forecast (Pages/CashForecast.razor) — the time-phased view,
                // with the former Cash Summary preserved below its divider while the §4 phasing
                // rules await FD/consultant sign-off. The retired slugs /finance and
                // /finance/cash-summary land on the same page, so old bookmarks keep working;
                // "$" keeps the bare /finance prefix from stealing the Xero rows' routes.
                new SidebarRow(new NavigationItem("Cash Forecast", "/finance/cash-forecast",
                        new[] { "/finance$", "/finance/cash-summary" }),
                    DesktopNavigation.DirectorRoles),
                // The accountant's live 13-week payment plan (Pages/WeeklyCashflow.razor):
                // Xero-seeded, week by week, every entry movable to the week it will really be
                // paid, plus the manual outgoings Xero can't see (subcontractors, staff,
                // subscriptions). The one row Accounts can see — see WeeklyCashflowRoles.
                new SidebarRow(new NavigationItem("Weekly Cashflow", "/finance/weekly-cashflow"),
                    DesktopNavigation.WeeklyCashflowRoles),
                // Gross profit by project: budgeted, current and forecast (finance meeting
                // 2026-08-03).
                new SidebarRow(new NavigationItem("Profit Summary", "/finance/profit-summary"),
                    DesktopNavigation.DirectorRoles)
            }),

        // ---- Xero: every screen that reads or feeds the accounts system — one home per screen
        // (2026-08-11; Allocation and Transactions were tabs behind one row, the aged views
        // lived under Financial Reports). Cost Allocation moved out to Standalone (2026-08-14):
        // it is a standing work queue, not a reference screen. ----
        new SidebarFolderInfo(
            SidebarFolder.Xero,
            "Xero",
            "#xero",
            new[]
            {
                new SidebarRow(new NavigationItem("Xero Transactions", "/finance/xero"),
                    DesktopNavigation.DirectorRoles),
                // Outstanding sales invoices aged as in Xero but including drafts still being
                // prepared (finance meeting 2026-08-03).
                new SidebarRow(new NavigationItem("Aged Receivables", "/finance/aged-receivables"),
                    DesktopNavigation.DirectorRoles),
                // Outstanding supplier bills aged as in Xero but including drafts — the invoices
                // the accounting procedure leaves in DRAFT until coded through the portal, which
                // Xero's own aged payables report cannot see.
                new SidebarRow(new NavigationItem("Aged Payables", "/finance/aged-payables"),
                    DesktopNavigation.DirectorRoles)
            }),

        // ---- Audit: who did what — the reconciliation trail, the routing register and the
        // assistant's activity log. Near the foot deliberately: review, not day-to-day work. ----
        new SidebarFolderInfo(
            SidebarFolder.Audit,
            "Audit",
            "#audit",
            new[]
            {
                // The finance reconciliation trail: every cost-centre move on the valuation
                // report — who moved which line, from where to where, when. Project-scoped, so
                // it leads the folder (the house pattern: the picked project first).
                new SidebarRow(new NavigationItem("Reconciliation Audit", "/projects/{project}/reconciliation-audit"),
                    DesktopNavigation.DirectorRoles),
                // The append-only audit register — who routed, linked and filed what. Renamed
                // from "Audit Trail" (2026-08-11, label only): beside the reconciliation trail
                // the old name no longer said WHICH trail.
                new SidebarRow(new NavigationItem("System Audit Trail", "/audit"),
                    DesktopNavigation.DirectorRoles),
                // What the AI connector has done, on whose behalf — every tool call a team
                // member's own AI made through MCP lands here under their name.
                new SidebarRow(new NavigationItem("Agent Activity", "/agents/activity"),
                    DesktopNavigation.DirectorRoles),
                // The AI tools connected through the MCP connector, and the disconnect switch.
                // Every signed-in user can open /settings/ai-connections directly (the setup
                // guide links it); the nav row follows the rail's directors-only clamp.
                new SidebarRow(new NavigationItem("AI Connections", "/settings/ai-connections"),
                    DesktopNavigation.DirectorRoles)
            }),

        // ---- Sales: how Jewel finds its next projects (2026-09-06) — the lead register, and
        // the strategies (methodologies with their justification) that feed it. Leads first: it
        // is the working list; Strategies is where a new way of finding clients is written down
        // and judged by its funnel. Sits between Audit and Admin (James's call, 2026-09-06). ----
        new SidebarFolderInfo(
            SidebarFolder.Sales,
            "Sales",
            "#sales",
            new[]
            {
                // Every lead whatever found it — strategy, inbound, referral, architect, past
                // client, by hand — on one ladder: New → Contacted → Engaged → Site visit →
                // Proposal → Won / Lost, Nurture for the parked. Won creates the client and
                // the project shell.
                new SidebarRow(new NavigationItem("Leads", "/sales/leads"),
                    DesktopNavigation.DirectorRoles),
                // sales@jewelbb.co.uk read live — its own thing, not the Control Centre's
                // triage: the conversation with a prospect beside the lead it belongs to.
                new SidebarRow(new NavigationItem("Inbox", "/sales/inbox"),
                    DesktopNavigation.DirectorRoles),
                // The ways of finding leads, each with its hypothesis, evidence, channel,
                // Claude-drafted approach plan and the funnel its leads make.
                new SidebarRow(new NavigationItem("Strategies", "/sales/strategies"),
                    DesktopNavigation.DirectorRoles)
            }),

        // ---- Admin: running the system itself, not any project — administrators only (the
        // empty role list + CanSee bypass). Last folder deliberately: it is the least
        // day-to-day thing on the rail. ----
        new SidebarFolderInfo(
            SidebarFolder.Admin,
            "Admin",
            "#admin",
            new[]
            {
                new SidebarRow(new NavigationItem("Users", "/admin/users"),
                    DesktopNavigation.AdministratorOnly),
                // The announced app version: publishing an update here raises the refresh bar
                // (UpdateToast) on every signed-in tab.
                new SidebarRow(new NavigationItem("System", "/admin/system"),
                    DesktopNavigation.AdministratorOnly),
                // The portal's shared external connections — Bluebeam Studio first: one account,
                // connected once, that every drawing extraction runs through.
                new SidebarRow(new NavigationItem("Integrations", "/admin/integrations"),
                    DesktopNavigation.AdministratorOnly),
                // The curated trade list every directory record and bid package picks from —
                // add, rename, delete (delete blocked while any record carries the trade).
                new SidebarRow(new NavigationItem("Trades", "/admin/trades"),
                    DesktopNavigation.AdministratorOnly),
                // Emails marked as a KPI against a member of staff (2026-09-03) — the
                // administrators-only register; nothing else in the system shows the marks.
                new SidebarRow(new NavigationItem("KPI emails", "/admin/kpis"),
                    DesktopNavigation.AdministratorOnly),
                // The connector's skills — the working knowledge the team's own Claude reads, edited
                // by the discipline owner (docs/ai/10-mcp-connector.md §2d). Directors rather than
                // administrator-only, deliberately: the MD maintaining his own doctrine is the
                // entire point of the store. Mirrors the API's SkillRoles.ManageSkills.
                new SidebarRow(new NavigationItem("AI Skills", "/admin/skills"),
                    DesktopNavigation.DirectorRoles),
                // Skills wired to connector actions — which doctrine describe_action serves with
                // which action (2026-08-31). Same audience as the skill store it draws from.
                new SidebarRow(new NavigationItem("AI Actions", "/admin/ai-actions"),
                    DesktopNavigation.DirectorRoles)
            })
    };

    /// <summary>Rows that belong to no folder — they render as top-level links at the FOOT of the
    /// sidebar, below every folder, with an icon, like Home. Gated per row exactly like folder
    /// rows. Two kinds of resident: whole-company destinations that answer to no one project —
    /// the standing work queues (the Control Centre, Document Triage and, since 2026-08-14,
    /// Xero Cost Allocation) — and the one project record important enough to outrank its folder
    /// (the Valuation Report — the system's flagship output, elevated 2026-08-11; being a
    /// {project} template it follows the picker like any folder row).</summary>
    public static readonly IReadOnlyList<SidebarRow> Standalone = new[]
    {
        // The Control Centre (formerly Triage) — the mailbox intake queue and router for ALL
        // correspondence across EVERY project. Mirrors the API's TriageRoles gate on the page;
        // the nav row carries the 2026-08-11 directors-only clamp like every other row.
        new SidebarRow(new NavigationItem("Control Centre", "/control-centre"),
            DesktopNavigation.DirectorRoles),
        // Document Triage (renamed from Document Control 2026-08-17 — one "Control" phrase in this
        // section is enough) — the attachment triage queue for ALL projects: files sent in from the
        // Control Centre, filed out to Drawings, Payment Certificates or subcontractor records.
        // Same whole-company footing as the Control Centre it feeds from (decision 2026-08-12).
        new SidebarRow(new NavigationItem("Document Triage", "/document-triage"),
            DesktopNavigation.DirectorRoles),
        // Xero Cost Allocation — distributing allocated purchase lines to cost centres. Moved up
        // from the Xero folder (2026-08-14): like the two queues above it is standing work that
        // NEEDS DOING, not a screen that is merely read, so it sits with them at the foot of the
        // rail rather than behind a folder header.
        new SidebarRow(new NavigationItem("Xero Cost Allocation", "/finance/allocation"),
            DesktopNavigation.DirectorRoles),
        // The picked project's live valuation report.
        new SidebarRow(new NavigationItem("Valuation Reports", "/projects/{project}/valuation"),
            DesktopNavigation.DirectorRoles)
    };
}
