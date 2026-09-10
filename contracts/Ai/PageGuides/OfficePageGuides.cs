namespace Jewel.JPMS.Contracts.Ai;

/// <summary>To-dos, the directory registers, the Sales section, audit, agents and admin. Data only.</summary>
public static class OfficePageGuides
{
    public static readonly IReadOnlyList<PageGuide> Guides = new PageGuide[]
    {
        new("/todos", "To-dos",
            "The master to-do list across every project — company-wide and project items together, "
            + "with a project filter. The MD and administrators see every item; everyone else sees "
            + "only items assigned to a role they hold, minus items pinned to a different named "
            + "person. An item reads as Open, In progress (started, chased or emailed from its page — "
            + "an amber chip) or Done. Manually: Board view (drag a card between Open and Done) or "
            + "List view with Open/Done/All tabs; filters for project scope, role and — for MD/admin "
            + "— person; "
            + "\"Add to-do\" opens a modal with Project picker (blank = company-wide, MD/admin "
            + "only), title, assignee, due date and notes. Clicking an item opens its detail page. "
            + "find_by_reference resolves a spoken \"TODO-0074\" to the item (title, notes, "
            + "project, route), and read_record_emails (record_type todo) reads its tagged mail — "
            + "so ACTION an item yourself rather than telling the user to click it: e.g. a "
            + "\"raise this WO\" item means read its emails, then open_modal work_order_create "
            + "with the item's projectId. You navigate_to here; no dialog opens on this page "
            + "itself. To-dos staged from an email (stage_triage_todo) happen in the Control "
            + "Centre, not here."),

        new("/todos/{todoItemId}", "To-do detail",
            "One to-do item's own page — its full facts (including \"About\": the record the item "
            + "is about, e.g. DEF-0012, linking to that record's page — set when it was raised from "
            + "the record's page or by add_todo aboutRecordType/aboutRecordId), multiline notes, "
            + "its Timeline, the to-dos linked to it by shared tagged mail, and its communications: "
            + "the item's tagged emails "
            + "read live, each answerable here, plus a new outbound email filed to the item. The "
            + "item is Open, In progress or Done: \"Working on it\" moves Open to In progress; so "
            + "does \"Log a chase\" in the Timeline panel (a chase or a note with words — for chases "
            + "made outside the portal, e.g. from the person's own Outlook or by phone); an email "
            + "sent from this page logs itself on the Timeline and starts the item too. The item "
            + "stays open until Mark done — being chased is not being finished. The Timeline lists "
            + "every change newest first (added, started, chased, emailed, reassigned, moved, due "
            + "date, done/reopened) with who and when. Above the communications list a notice "
            + "reports newer replies on the item's threads that are not filed to it yet (they wait "
            + "in the Control Centre queue); \"File it here\" tags them to the item (triage roles), "
            + "and the file_unfiled_replies action does the same for you — read_record_emails "
            + "lists them under unfiledReplies. "
            + "Manually: Mark done / Reopen; a two-click armed Delete; the reassign editor (role, "
            + "optionally pinned to a person) and the move editor (to another project, or "
            + "company-wide for MD/admin). Reassign, move and delete need the manage gate; "
            + "completing and logging progress need manage or the item being the reader's own. "
            + "You navigate_to with the ready-made route "
            + "(find_by_reference resolves \"TODO-0074\" to it), and read_record_emails "
            + "(record_type todo) reads this item's communications; no dialog opens here, but "
            + "actioning the item's work often opens one elsewhere — e.g. work_order_create for a "
            + "\"raise this WO\" item. Items are added on /todos or a project's To-do tab, not "
            + "on this page."),

        new("/architects", "Architect practices",
            "The register of architect practices — when an architect is a project's party, RFIs "
            + "and other request documents go to the contact email held here. Manually: a table of "
            + "Practice / Contact / Contact email with per-row Contacts (the party contacts "
            + "editor) and Edit buttons, a \"New architect\" button, and Export to Excel. You can "
            + "navigate_to only — no dialog is registered, so creating or editing a practice is "
            + "the user's manual act. Client accounts are managed on /clients, not here."),

        new("/clients", "Client accounts",
            "The register of client accounts — when a client is a project's party, request "
            + "documents go to the primary contact held here. Manually: a table of Client / "
            + "Primary contact / Contact email with per-row Contacts and Edit buttons, a \"New "
            + "client\" button, and Export to Excel. You can navigate_to only — no dialog is "
            + "registered here. Architect practices are managed on /architects; when Jewel works "
            + "through an architect, the practice is the party on the project or request."),

        // ---- Sales (2026-09-06): strategies for finding leads, and the register they feed ----
        new("/sales/leads", "Leads",
            "Sales → Leads: the one register of everyone Jewel might convince to build with it — "
            + "to upgrade a house or have a new home built — whatever found them: a sales strategy "
            + "(the lead carries the strategy's name), an inbound enquiry, a referral, an architect, "
            + "a past client, or someone typing it in. Each lead is LD-#### with contact, company, "
            + "prospect kind, the property/site address and postcode, a one-line summary, source, "
            + "strategy, stage on the ladder New → Contacted → Engaged → Site visit → Proposal "
            + "ending Won / Lost (or parked in Nurture), estimated value, owner and dates. "
            + "Manually: stage chips filter the table (Open by default), a strategy filter, a "
            + "search box, \"New lead\" opens the capture modal, a row opens the lead's own page. "
            + "Over the connector: list_leads (filter by stage / strategyId), get_lead, "
            + "capture_lead, update_lead, move_lead_stage, log_lead_activity, win_lead (directors: "
            + "creates the Client and the project shell). Never file a lead as Won by hand — "
            + "win_lead is what creates the records."),

        new("/sales/leads/{leadId}", "Lead",
            "One lead: its details (edit in a modal), its stage with a move control (any open "
            + "stage; Lost asks for a reason; Nurture parks it; Won is the \"Won — create client "
            + "& project\" button, directors only, which asks for the project reference and name "
            + "and creates the Client account and the project shell in one move), and its "
            + "timeline — every call, email, letter, meeting, site visit, proposal and note "
            + "logged by hand plus every stage change, newest first. \"Log activity\" adds a "
            + "touch. A Won lead links to its project. Over the connector: get_lead reads it "
            + "(an LD-#### reference resolves), log_lead_activity, move_lead_stage, update_lead, "
            + "win_lead act on it. Below the details sit the two panels of the journey AFTER a lead "
            + "is identified. Imagine: \"Issue link & QR code\" mints the lead's private "
            + "/imagine/{token} link (the QR code goes on the letter — only that link opens the "
            + "page, there is no general address; re-issuing kills printed codes); every round the "
            + "prospect runs shows here — their photos and brief, the AI concepts rendered over their "
            + "own photos, what they liked and said — with Retry for a failed render. Proposals: "
            + "versions of the scope / base price / options (price deltas) / schedule of works / "
            + "terms; a draft is edited and sent (the prospect is emailed the imagine link, where it "
            + "shows; the lead moves to Proposal); their acceptance — name, email, options, price, "
            + "moment — is the agreement the Won button then builds on."),

        new("/sales/inbox", "Sales inbox",
            "Sales → Inbox: sales@jewelbb.co.uk read live from the mailbox — deliberately its own "
            + "thing, NOT the Control Centre's projects@ triage: nothing is tagged, moved or "
            + "stored. Left: the Inbox newest first (search reads the whole mailbox), each sender "
            + "chipped with the lead whose contact email it is. Open a row for the whole thread; "
            + "expand a message for its body. From the thread: Reply (sent from sales@, reply-all, "
            + "quoted history, logged on the matched lead), \"Log on LD-####\" (an Email activity "
            + "on the lead's timeline), \"New lead from this email\" (the lead form pre-filled from "
            + "the sender) or \"Log on a lead…\" (pick one). If the page says it isn't connected, "
            + "the API needs the MailboxIntake Graph credentials and the Exchange access policy "
            + "must include sales@."),

        new("/imagine/{token}", "Imagine (public)",
            "The prospect's private page, opened from the QR code on their letter — no sign-in; "
            + "the token is the lead's. They upload photos of their house or plot (shrunk in the "
            + "browser), write what they dream of, leave an email, and the worker returns three "
            + "concepts rendered over their own photos (Claude writes them, Azure image generation "
            + "renders them); they like, comment, and ask for a revision of one (up to four rounds). "
            + "When a proposal has been sent it shows here too — options with a live price, the "
            + "schedule of works, the terms — and they accept or decline. Staff never use this "
            + "page; everything it does lands on the lead's timeline."),

        new("/sales/strategies", "Sales strategies",
            "Sales → Strategies: the methodologies Jewel uses to FIND leads, each written down "
            + "with its justification so it can be judged — e.g. homeowners in postcodes where "
            + "house prices are about to move, approached by post with a research brochure; "
            + "architects, shown how the portal's project management removes their chasing. A "
            + "strategy starts as a BRIEF — the idea in the team's own words — with an audience "
            + "and a channel; AI research (run_strategy_research, \"Research with AI\" on the page, "
            + "or the tick on the New strategy dialog) then searches the web and fills in the "
            + "target area, the hypothesis (why these people, why now), the evidence with source "
            + "URLs and the proposition, writes its findings, and drafts the approach plan — or "
            + "the team writes any of those by hand and the research keeps what they wrote. A "
            + "status (Draft / Active / Paused / Retired), an owner — and a funnel counted from "
            + "the leads that carry its id: "
            + "leads found, contacted, engaged, proposals, won, lost, the open pipeline value "
            + "and the won value. Manually: cards with the funnel figures, \"New strategy\" "
            + "opens the create modal, a card opens the strategy page. Over the connector: "
            + "list_sales_strategies, get_sales_strategy, create_sales_strategy, "
            + "update_sales_strategy, set_sales_strategy_status (directors), "
            + "generate_strategy_plan. When the user describes a new way of finding clients, "
            + "that is a strategy — capture the hypothesis and evidence in their words."),

        new("/sales/strategies/{strategyId}", "Sales strategy",
            "One strategy: its brief and definition (edit in a modal), the research state "
            + "(\"Research with AI\" queues a background run — Queued → Running → Complete / "
            + "Failed with the reason; the page polls while it runs), the research findings "
            + "(markdown with sources), its funnel, the approach plan (markdown — \"Generate "
            + "with AI\" drafts or redrafts it from the definition and findings with an optional "
            + "steer; \"Edit plan\" rewrites it by hand), a status control (directors) and the "
            + "leads it has found, with \"Add lead\" capturing a new one already attributed to "
            + "the strategy. Over the connector: get_sales_strategy, run_strategy_research, "
            + "update_sales_strategy, generate_strategy_plan, set_sales_strategy_status, and "
            + "capture_lead with this strategyId."),

        new("/dashboard", "Home",
            "The signed-in home page, rendered per active role: administrators get the admin home "
            + "(user directory and pending access-request panels), every other role gets its role "
            + "home — for site-floor roles that is the My Day workspace. The director / finance "
            + "director home shows count tiles (Live projects, Valuations overdue, Control Centre "
            + "inbox, Xero lines to allocate, Open and Overdue to-dos, Documents expiring) — each "
            + "tile is a link: Valuations overdue lands on /projects filtered to the overdue rows "
            + "— then the My to-dos board, the Upcoming valuations table (every live project's next "
            + "expected valuation date, Overdue in red, Due soon in amber, sorted soonest first) "
            + "and, for the MD and FD, the Expiring documents panel: every subcontractor "
            + "compliance document expired or inside 30 days, soonest first, each row linking to "
            + "the company's directory record. \"Valuation "
            + "overdue\" means the project's next expected valuation date (set on its Settings tab) "
            + "has passed. There are no page-level actions of its own. The retired /my-day route "
            + "redirects here.",
            Aliases: new[] { "/my-day" }),

        new("/audit", "Audit trail",
            "The append-only audit register — who routed, linked and filed what: every recorded "
            + "client-facing interaction plus finance reconciliation events, newest first, 50 per "
            + "page with Load more. Manually: pathway tabs (All / Client / Subcontractor / "
            + "Internal), an event-type select and a project filter; the Email column's \"Open in "
            + "Outlook\" link opens the drafted email; request references link to the request "
            + "detail page. You navigate_to only — the register is read-only; nothing is created "
            + "or edited here."),

        new("/agents/activity", "Agent activity",
            "What the assistant has done, on whose behalf, and what it cost — every agent run, "
            + "newest first, with when, agent, who it ran as (rows marked \"unattended\" ran with "
            + "nobody watching), action, outcome, tools used, duration, tokens and cost, plus "
            + "totals underneath. Manually: All runs and Unattended only filters; clicking a row "
            + "that carries a route navigates to the page that run touched. You navigate_to only — "
            + "the log is read-only. Every connector tool call lands here under the caller's name."),

        new("/admin/skills", "AI skills",
            "The assistant's editable domain skills — versioned markdown manuals that are in force "
            + "on the assistant's very next message, no deploy. Manually: a table of skills "
            + "(agent, Pinned/On demand, Active/Off, version, size, references, updated); \"New "
            + "skill\" or clicking a row opens the editor: skill key (fixed after creation), "
            + "owning agent or Shared, display name, description (what the assistant routes on), "
            + "Pinned and Active toggles, and the markdown body — pasting a whole SKILL.md lifts "
            + "its frontmatter into the empty fields; each save is a new version, and reference "
            + "documents can be added per skill. You navigate_to; no dialog opens here — the "
            + "editor is the user's own form."),

        new("/admin/users", "Users",
            "User administration — everyone who can sign in, with invites and the role "
            + "assignments that decide what each person sees. Revoking a user moves them to the "
            + "Revoked list at /admin/users/revoked, where they can be restored or permanently "
            + "deleted. Requires the Administrator active role. You navigate_to only — role "
            + "changes and invites are the administrator's manual acts."),

        new("/admin/users/revoked", "Revoked users",
            "The people whose access has been revoked — they cannot sign in, but their record and "
            + "old roles are kept. Manually: Restore puts a user back exactly as they were; Delete "
            + "permanently removes the record and is offered only here, never on a live account. "
            + "Requires the Administrator active role. You navigate_to only."),

        new("/admin/system", "System",
            "The announced app version — the version every signed-in tab is asked to run. "
            + "Manually: the Announced version panel's \"Publish update\" button (two-step "
            + "confirm) increments the version and raises the refresh bar on every open tab, used "
            + "once a deploy has finished; below it, the Tender terms & conditions panel holds the "
            + "company's single standard T&C PDF that is attached automatically to every "
            + "tender-invite email on every project. Requires the Administrator active role. You "
            + "navigate_to only — publishing and uploading are the administrator's manual acts."),

        new("/admin/trades", "Trades",
            "Admin → Trades: the curated master list of trades the directory and bid packages "
            + "pick from — this page is where that vocabulary is managed. A person adds, renames "
            + "or deletes a trade; deletes are refused while directory records or packages still "
            + "use one. You navigate_to only — no dialog or page action is registered here."),

        new("/admin/kpis", "KPI emails",
            "Admin → KPI emails: the administrators-only register of emails marked as a KPI "
            + "against a person at Jewel — evidence of how someone is performing, filed under "
            + "that person: a portal user, or someone without a login added by name (\"Add "
            + "person\" in the header, or typed into the picker when marking). Filter by person; "
            + "each row shows the email's subject, sender and date, the note, who marked it and "
            + "when, with Open (opens the email in the Control Centre), Edit (re-file under "
            + "another person, rewrite the note) and Remove. Emails are marked in the Control "
            + "Centre's Internal pane → Actions → Mark as KPI (administrators see that action; "
            + "nobody else does), or over the connector with mark_email_as_kpi. Nothing is tagged "
            + "in the mailbox. Read the register with list_kpi_emails, the people with "
            + "list_kpi_people; a KPI-#### reference resolves through find_by_reference. Never "
            + "repeat the register's contents to, or in a draft for, anyone but the administrator "
            + "asking. You navigate_to only — no dialog is registered here."),

        new("/registers", "Registers",
            "The company registers — insurances, subscriptions, vehicles and trade accounts, one "
            + "tab per kind, each dated field tracking a renewal so nothing lapses unseen. A "
            + "person adds and edits entries in place. You navigate_to only — no dialog or page "
            + "action is registered here."),

        new("/policies", "Policies & sign-off",
            "Staff sign-off forms: NDAs, staff policies and H&S documents. An admin publishes a "
            + "document to named users; each recipient reads and signs, and the page tracks who "
            + "has signed what. You navigate_to only — no dialog or page action is registered "
            + "here."),

        new("/labour/xero-mapping", "Xero mapping",
            "The effective-dated bridges between the portal and Xero: project to Sites tracking "
            + "category, worker to Xero contact, and the account codes labour lands on. Finance "
            + "sets a mapping with a from-date; the labour coding runs read whatever is effective. "
            + "You navigate_to only — no dialog or page action is registered here."),
    };
}
