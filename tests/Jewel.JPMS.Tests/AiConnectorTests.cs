using Jewel.JPMS.Api.Features.Ai;
using Jewel.JPMS.Api.Features.Ai.Tools.Actions;
using Jewel.JPMS.Api.Features.Ai.Tools;
using Jewel.JPMS.Api.Features.Connect;
using Jewel.JPMS.Api.Gates;
using Jewel.JPMS.Models;
using Xunit;

namespace Jewel.JPMS.Tests;

// The MCP connector's static contracts (docs/ai/10-mcp-connector.md): the tool catalogue is
// role-filtered exactly as the endpoints gate, the write tools exist for the roles that may use
// them, and the hand-kept registries still agree (the boot drift check). These pin the surface a
// team member's AI tool is offered — a regression here is a tool silently vanishing from, or
// leaking into, someone's Claude.
public sealed class AiConnectorTests
{
    private static SignedInUser UserWith(params Role[] roles) =>
        new("test@jewelbb.co.uk", "Test User", roles);

    [Fact]
    public void DriftCheck_passes()
    {
        // Throws on drift — the same call the boot makes.
        AiRegistryDriftCheck.Assert();
    }

    [Fact]
    public void ToolNames_areUnique()
    {
        var names = AiToolCatalogue.All.Select(tool => tool.Name).ToList();
        Assert.Equal(names.Count, names.Distinct(StringComparer.OrdinalIgnoreCase).Count());
    }

    [Fact]
    public void Catalogue_carriesNoUiTools()
    {
        // The chat's browser-executed tools are gone; everything left runs server-side.
        Assert.DoesNotContain(AiToolCatalogue.All, tool => tool.Kind == AiToolKind.Ui);
    }

    [Fact]
    public void ForConnector_filtersByRole()
    {
        var director = AiToolCatalogue.ForConnector(UserWith(Role.ManagingDirector)).Select(t => t.Name).ToList();
        var subcontractor = AiToolCatalogue.ForConnector(UserWith(Role.Subcontractor)).Select(t => t.Name).ToList();

        Assert.Contains("list_projects", director);
        Assert.Contains("get_request_context", director);
        // A subcontractor contact must not be offered the internal read surface.
        Assert.DoesNotContain("list_projects", subcontractor);
        Assert.DoesNotContain("get_valuation_context", subcontractor);
        Assert.True(director.Count > subcontractor.Count);
    }

    [Fact]
    public void WriteTools_exist_andAreMarkedAsWrites()
    {
        // Probed as the MD, not Admin: Role.Admin is deliberately NOT a member of
        // JpmsRoleSets.AllInternal / InternalAndArchitect (it is the system role, not a delivery
        // role), so the request/todo write tools are not offered to a bare Admin — the Director
        // sits in every one of these gates.
        var director = AiToolCatalogue.ForConnector(UserWith(Role.ManagingDirector));
        foreach (var name in new[] { "post_request_message", "add_todo", "complete_todo", "log_todo_progress", "save_skill" })
        {
            var tool = director.SingleOrDefault(candidate => candidate.Name == name);
            Assert.NotNull(tool);
            Assert.Equal(AiToolKind.Write, tool!.Kind);
        }
    }

    // The report-as-files tool (2026-09-02) mirrors the download endpoints' gate: every internal
    // role can pull the portal's own PDF and workbook; an external login never sees project money.
    [Fact]
    public void ExportValuationReport_isAnInternalRead()
    {
        var director = AiToolCatalogue.ForConnector(UserWith(Role.ManagingDirector));
        var tool = Assert.Single(director, candidate => candidate.Name == "export_valuation_report");
        Assert.Equal(AiToolKind.Read, tool.Kind);

        var quantitySurveyor = AiToolCatalogue.ForConnector(UserWith(Role.QuantitySurveyor)).Select(t => t.Name);
        Assert.Contains("export_valuation_report", quantitySurveyor);
        var subcontractor = AiToolCatalogue.ForConnector(UserWith(Role.Subcontractor)).Select(t => t.Name);
        Assert.DoesNotContain("export_valuation_report", subcontractor);
    }

    [Fact]
    public void SaveSkill_isNotOfferedOutsideTheSkillGate()
    {
        var quantitySurveyor = AiToolCatalogue.ForConnector(UserWith(Role.QuantitySurveyor)).Select(t => t.Name);
        Assert.DoesNotContain("save_skill", quantitySurveyor);
    }

    [Theory]
    [InlineData("https://claude.ai/api/mcp/auth_callback", true)]
    [InlineData("https://www.perplexity.ai/rest/connections/oauth_callback", true)]
    [InlineData("http://localhost:33418/callback", true)]
    [InlineData("http://127.0.0.1:8976/oauth/callback", true)]
    [InlineData("http://evil.example/callback", false)]
    [InlineData("not-a-url", false)]
    [InlineData("", false)]
    public void RedirectUris_allowHttpsAndLoopbackOnly(string uri, bool acceptable)
    {
        Assert.Equal(acceptable, OAuthRedirects.IsAcceptable(uri));
    }

    [Fact]
    public void ActionRegistry_buildsAndSelfAsserts()
    {
        // Construction IS the assertion: unique names, real stamp parameters, and a typed
        // Allows/Check overload for every command (the 2026-08-28 review found "first overload"
        // selection breaking 22 actions on shared gate classes — this pins the fix).
        var actions = AiActionRegistry.All;
        Assert.True(actions.Count > 150, $"expected a full surface, got {actions.Count}");
        foreach (var action in actions)
            _ = AiActionSchema.InputSchema(action);
    }

    [Fact]
    public void ActionRegistry_roleFiltersLikeThePortal()
    {
        bool Offered(string name, params Role[] roles) =>
            AiActionRegistry.All.Single(a => a.Name == name).VisibleTo.IncludesAny(roles);

        Assert.True(Offered("approve_variation_order", Role.ManagingDirector));
        Assert.False(Offered("approve_variation_order", Role.Subcontractor));
        Assert.False(Offered("delete_project", Role.QuantitySurveyor));
        Assert.False(Offered("issue_valuation_invoice", Role.Subcontractor));
    }

    [Fact]
    public void ActionGateway_toolsExistForEveryRole()
    {
        foreach (var role in System.Enum.GetValues<Role>())
        {
            var names = AiToolCatalogue.ForConnector(UserWith(role)).Select(t => t.Name).ToList();
            Assert.Contains("list_actions", names);
            Assert.Contains("describe_action", names);
            Assert.Contains("perform_action", names);
        }
    }

    [Fact]
    public void ParityAuditReadTools_exist_andStayInternal()
    {
        // The 2026-08-31 read surface (docs/ai/11 §5): the register a mirrored write acts on must
        // be readable by the roles that write it, and none of it may leak to a subcontractor.
        var director = AiToolCatalogue.ForConnector(UserWith(Role.ManagingDirector)).Select(t => t.Name).ToList();
        var subcontractor = AiToolCatalogue.ForConnector(UserWith(Role.Subcontractor)).Select(t => t.Name).ToList();

        var readTools = new[]
        {
            "list_valuation_invoices", "list_valuation_snapshots", "get_valuation_snapshot",
            "list_triage_queue", "get_mailbox_message", "list_mailbox_conversation",
            "search_mailbox", "list_document_triage", "list_project_communications",
            "get_weekly_cashflow_plan", "get_aged_payables", "get_aged_receivables",
            "list_payment_certificates", "list_xero_ledger_lines",
            "list_leads", "get_lead", "list_sales_strategies", "get_sales_strategy", "list_rates", "list_clients",
            "list_architects", "list_workers", "list_company_registers", "list_portal_users",
            "list_rfis_across_projects", "list_useful_information",
            "list_calendar_events", "get_building_control", "get_programme",
            "list_architect_instructions", "list_progress",
            "get_package_reconciliation"
        };
        foreach (var name in readTools)
        {
            Assert.Contains(name, director);
            Assert.DoesNotContain(name, subcontractor);
        }

        // list_documents (list_drawings until the 2026-09-03 Drawings → Documents rename)
        // deliberately mirrors JpmsRoleSets.DrawingReaders, which ADMITS subcontractors — they
        // read revisions for their assigned work, exactly as over HTTP.
        Assert.Contains("list_documents", director);
        Assert.Contains("list_documents", subcontractor);
        // get_document_extraction (2026-09-07) is the structured read of a revision's PDF behind
        // the same DrawingReaders gate as the page's Extracted-data panel.
        Assert.Contains("get_document_extraction", director);
        Assert.Contains("get_document_extraction", subcontractor);
        Assert.DoesNotContain("list_drawings", director); // the old name is a lookup courtesy, never advertised
    }

    [Fact]
    public void LegacyDrawingNames_resolveToDocumentEntries()
    {
        // 2026-09-03: the register was renamed Drawings → Documents. Saved skills and old habits
        // still say the drawing names — Find lands them on the renamed entry, and the catalogue
        // never lists the old spelling twice.
        Assert.Equal("list_documents", AiToolCatalogue.Find("list_drawings")!.Name);
        Assert.Equal("register_document", AiActionRegistry.Find("register_drawing")!.Name);
        Assert.Equal("file_document_to_project_documents", AiActionRegistry.Find("file_document_as_drawing")!.Name);
        Assert.Equal("set_bid_package_documents", AiActionRegistry.Find("SET_BID_PACKAGE_DRAWINGS")!.Name);
        Assert.Equal("Documents", AiLegacyNames.Current("Drawings"));
        Assert.Contains("delete_drawing", AiLegacyNames.AllNamesFor("delete_document"));
        Assert.DoesNotContain(AiActionRegistry.All, action => action.Name.Contains("drawing", StringComparison.OrdinalIgnoreCase));
        Assert.DoesNotContain(AiToolCatalogue.All, tool => tool.Name.Contains("drawing", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void ParityAuditActions_areDeclared()
    {
        // The write gaps recorded in docs/ai/11 §2 and the §4 unlocks, now declared.
        var names = AiActionRegistry.All.Select(a => a.Name).ToList();
        foreach (var name in new[]
        {
            "create_weekly_cashflow_item", "update_weekly_cashflow_item",
            "archive_weekly_cashflow_item", "place_weekly_cashflow_entry",
            "set_weekly_cashflow_exclusion", "save_weekly_cashflow_supplier_group",
            "remove_weekly_cashflow_supplier_group",
            "add_inventory_item", "update_inventory_item", "create_inventory_item_from_message",
            "import_architect_instruction_from_message", "update_architect_instruction",
            "link_architect_instruction_to_variation", "unlink_architect_instruction_from_variation",
            "delete_architect_instruction",
            "create_request_from_message", "discard_mailbox_message",
            "restore_mailbox_message", "remove_mailbox_message_tag",
            "attach_action_skills"
        })
        {
            Assert.Contains(name, names);
        }

        // Deleting an instruction says "permanently" — the registry's own boot assert requires
        // the confirm-first flag, pinned here too so a rewording never drops the gate.
        Assert.True(AiActionRegistry.All.Single(a => a.Name == "delete_architect_instruction").RequiresConfirmation);
    }

    [Fact]
    public void MonthEndActions_areDeclared_andGateOnConfirmation()
    {
        // The 2026-08-31 month-end chain (the accountant's ask): sign-off, the Xero coding run,
        // reconciliation and the mappings, all reachable from the connector.
        var names = AiActionRegistry.All.Select(a => a.Name).ToList();
        foreach (var name in new[]
        {
            "sign_off_labour_week", "remove_labour_week_sign_off", "run_xero_coding",
            "preview_xero_coding", "reset_xero_coding_outcome",
            "set_xero_line_timesheet_cover", "add_labour_settlement_variance",
            "set_site_xero_mapping", "set_cost_code_xero_mapping"
        })
        {
            Assert.Contains(name, names);
        }

        // Confirm-first is the contract on everything that writes to Xero, freezes a week for
        // settlement, posts a variance, or redirects where money codes — pinned so a rewording
        // never drops the gate.
        foreach (var name in new[]
        {
            "sign_off_labour_week", "run_xero_coding", "reset_xero_coding_outcome",
            "add_labour_settlement_variance", "set_site_xero_mapping", "set_cost_code_xero_mapping"
        })
        {
            Assert.True(AiActionRegistry.All.Single(a => a.Name == name).RequiresConfirmation,
                $"{name} must be confirm-first.");
        }

        // The dry run is read-shaped: it never asks for confirmation (2026-09-03).
        Assert.False(AiActionRegistry.All.Single(a => a.Name == "preview_xero_coding").RequiresConfirmation);

        // The settlement cluster gates on ManageSettlement — a site role never sees it.
        var foreman = UserWith(Role.Foreman);
        Assert.DoesNotContain(AiActionRegistry.All,
            a => a.Name == "run_xero_coding" && a.VisibleTo.IncludesAny(foreman.Roles));
    }

    [Fact]
    public void MonthEndReadTools_exist_andStayInternal()
    {
        var director = AiToolCatalogue.ForConnector(UserWith(Role.ManagingDirector)).Select(t => t.Name).ToList();
        var subcontractor = AiToolCatalogue.ForConnector(UserWith(Role.Subcontractor)).Select(t => t.Name).ToList();
        foreach (var name in new[] { "view_settlement_month", "view_worker_month", "get_xero_mappings", "view_labour_chase" })
        {
            Assert.Contains(name, director);
            Assert.DoesNotContain(name, subcontractor);
        }
    }

    [Fact]
    public void WorkerLinkAndChaseActions_areDeclared()
    {
        // The month-end doc's items A–H (2026-08-31): settlement identity fixable where the gap
        // is found, the backfill sweep, and reasoned chase dismissals with their undo.
        var names = AiActionRegistry.All.Select(a => a.Name).ToList();
        foreach (var name in new[]
        {
            "link_worker_to_company", "set_worker_sole_trader",
            "reconcile_worker_directory_links",
            "dismiss_labour_chase_day", "restore_labour_chase_day"
        })
        {
            Assert.Contains(name, names);
        }

        // The bulk sweep writes links for many workers at once — confirm-first, pinned.
        Assert.True(AiActionRegistry.All.Single(a => a.Name == "reconcile_worker_directory_links").RequiresConfirmation);
    }

    [Fact]
    public void AccountantsSeptemberNinthAsks_reachTheConnector()
    {
        // Everything handed to the accountant on 2026-09-09 must be doable from the connector as
        // well as the page: CIS verification, the compliance register, the Xero contact link /
        // pull / push (with its preview), the purchase order email, and the Work Order bill
        // approve / undo with a figure per order.
        var names = AiActionRegistry.All.Select(a => a.Name).ToList();
        foreach (var name in new[]
        {
            "record_cis_verification", "link_directory_record_to_xero_contact",
            "push_directory_contacts_to_xero", "send_work_order_po_email",
            "approve_work_order_bill", "undo_work_order_bill_approval"
        })
        {
            Assert.Contains(name, names);
        }

        var financeDirector = AiToolCatalogue.ForConnector(UserWith(Role.FinanceDirector)).Select(t => t.Name).ToList();
        var subcontractor = AiToolCatalogue.ForConnector(UserWith(Role.Subcontractor)).Select(t => t.Name).ToList();
        foreach (var name in new[] { "list_compliance_register", "preview_xero_contact_push", "list_xero_ledger_lines", "read_source" })
        {
            Assert.Contains(name, financeDirector);
            Assert.DoesNotContain(name, subcontractor);
        }

        // Approve writes tracking to Xero and approves the bill there; undo clears the tracking.
        // Both confirm-first, and the FD's button, never the site's.
        foreach (var name in new[] { "approve_work_order_bill", "undo_work_order_bill_approval" })
        {
            var action = AiActionRegistry.All.Single(a => a.Name == name);
            Assert.True(action.RequiresConfirmation, $"{name} must be confirm-first.");
            Assert.True(action.VisibleTo.IncludesAny(UserWith(Role.FinanceDirector).Roles));
            Assert.False(action.VisibleTo.IncludesAny(UserWith(Role.Foreman).Roles));
        }

        // The actor is stamped server-side — never a schema property the model could supply.
        var approve = AiActionRegistry.All.Single(a => a.Name == "approve_work_order_bill");
        var schema = System.Text.Json.JsonSerializer.Serialize(AiActionSchema.InputSchema(approve));
        Assert.DoesNotContain("approvedBy", schema);
        Assert.Contains("slices", schema);
    }

    [Fact]
    public void SaveSkillReference_isAWriteToolBehindTheSkillGate()
    {
        var admin = AiToolCatalogue.ForConnector(UserWith(Role.Admin));
        var tool = admin.SingleOrDefault(candidate => candidate.Name == "save_skill_reference");
        Assert.NotNull(tool);
        Assert.Equal(AiToolKind.Write, tool!.Kind);
        Assert.DoesNotContain("save_skill_reference",
            AiToolCatalogue.ForConnector(UserWith(Role.QuantitySurveyor)).Select(t => t.Name));
    }
}
