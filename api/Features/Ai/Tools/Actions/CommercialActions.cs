using Jewel.JPMS.Api.Features.Cashflow.Commands;
using Jewel.JPMS.Api.Features.Commercial.Commands;
using Jewel.JPMS.Api.Features.CommercialInputs.Commands;
using Jewel.JPMS.Api.Features.Cvr.Commands;
using Jewel.JPMS.Contracts.Cashflow;
using Jewel.JPMS.Contracts.Commercial;
using Jewel.JPMS.Contracts.CommercialInputs;
using Jewel.JPMS.Contracts.Cvr;

namespace Jewel.JPMS.Api.Features.Ai.Tools.Actions;

/// <summary>Commercial, CommercialInputs, CVR and Cashflow commands as connector actions.
/// Mirrors Features/Commercial/Commands, Features/CommercialInputs/Commands,
/// Features/Cvr/Commands and Features/Cashflow/Commands. Every authorisation in these areas
/// keeps its role set as a private field, so each VisibleTo below replicates the identical
/// roles with RoleSet.Of(...) — the field name comments say which authorisation each copies.
/// None of these endpoints stamp the signed-in user onto the command, so every entry's
/// stamp lists are empty — except the Xero allocation (CommercialActions.XeroAllocation,
/// 2026-09-03), whose endpoint stamps AllocatedBy from the signed-in user's email, and the Work
/// Order bill approve/undo (CommercialActions.WorkOrderBills, 2026-09-09), stamped ApprovedBy /
/// UndoneBy the same way.</summary>
internal sealed partial class CommercialActions : IAiActionSource
{
    // Replica of AddClaimPeriodAuthorisation.RolesThatMayDefineClaimPeriods.
    private static readonly RoleSet ClaimPeriodDefiners =
        RoleSet.Of(JpmsRoles.Director, JpmsRoles.ProjectManager, JpmsRoles.Estimator);

    // Replica of ValuationReportAuthorisation.RolesThatMayEditValuationBill.
    private static readonly RoleSet ValuationBillEditors =
        RoleSet.Of(JpmsRoles.Director, JpmsRoles.ProjectManager, JpmsRoles.Estimator);

    // Replica of ValuationReportAuthorisation.RolesThatMayManageClaimLifecycle (identical to
    // its RolesThatMayManageSnapshots, RolesThatMayRecordClaimEntries and
    // RolesThatMayMapClientReferences sets).
    private static readonly RoleSet ClaimLifecycleManagers =
        RoleSet.Of(JpmsRoles.Director, JpmsRoles.ProjectManager, JpmsRoles.Estimator, JpmsRoles.FinanceDirector);

    // Replica of ValuationReportAuthorisation.RolesThatMayRecodeCostCentres.
    private static readonly RoleSet CostCentreRecoders =
        RoleSet.Of(JpmsRoles.Director, JpmsRoles.FinanceDirector, JpmsRoles.ProjectManager);

    // Replica of CreateCostCentreGroupAuthorisation.RolesThatMayManageGroups,
    // ReconciliationPackageAuthorisation.RolesThatMayManagePackages,
    // SetCostCentreCostCompletionAuthorisation.RolesThatMaySetCostCompletion and
    // SetCostCentreFinalisationAuthorisation.RolesThatMayFinalise (all identical).
    private static readonly RoleSet FinancialsTabManagers =
        RoleSet.Of(Role.Admin, JpmsRoles.Director, JpmsRoles.FinanceDirector, JpmsRoles.ProjectManager, JpmsRoles.Estimator);

    // Replica of DraftValuationAuthorisation.RolesThatMayDraftValuations (identical to
    // ReviseValuationAuthorisation.RolesThatMayReviseValuations and
    // SetCostCodeBudgetAuthorisation.RolesThatMaySetBudgets).
    private static readonly RoleSet ValuationDrafters =
        RoleSet.Of(JpmsRoles.Director, JpmsRoles.ProjectManager, JpmsRoles.Estimator);

    // Replica of IssueValuationAuthorisation.RolesThatMayIssueValuations (identical to
    // GrantEotAuthorisation.RolesThatMayGrantEots and UpdateEotAuthorisation.RolesThatMayUpdateEots).
    private static readonly RoleSet DirectorsOnly = RoleSet.Of(JpmsRoles.Director);

    // Replica of PrepareValuationReportSnapshotEmailDraftAuthorisation.RolesThatMayEmailSnapshots.
    private static readonly RoleSet SnapshotEmailDrafters =
        RoleSet.Of(JpmsRoles.Director, JpmsRoles.FinanceDirector, JpmsRoles.ProjectManager);

    // Replica of SetXeroLineWorkOrderLinksAuthorisation.RolesThatMayLink.
    private static readonly RoleSet XeroWorkOrderLinkers =
        RoleSet.Of(JpmsRoles.Director, JpmsRoles.FinanceDirector, JpmsRoles.ProjectManager, JpmsRoles.Estimator);

    // Replica of LogDayworkAuthorisation.RolesThatMayLogDayworks.
    private static readonly RoleSet DayworkLoggers =
        RoleSet.Of(JpmsRoles.Director, JpmsRoles.ProjectManager, JpmsRoles.Estimator, JpmsRoles.SiteManager);

    // Replica of RecordContraChargeAuthorisation.RolesThatMayRecordContraCharges.
    private static readonly RoleSet ContraChargeRecorders =
        RoleSet.Of(JpmsRoles.Director, JpmsRoles.ProjectManager, JpmsRoles.Estimator);

    // Replica of RecordSubcontractorRetentionAuthorisation.RolesThatMayRecordRetention.
    private static readonly RoleSet RetentionRecorders =
        RoleSet.Of(JpmsRoles.Director, JpmsRoles.FinanceDirector, JpmsRoles.Estimator);

    // Replica of CaptureCvrSnapshotAuthorisation.RolesThatMayCaptureSnapshots (identical to the
    // RecordCvrPackageRow, RecordForecastComponent, RecordPrelimForecastForWeek, RecordQsAccrual
    // and UpdateQsAccrual authorisation sets).
    private static readonly RoleSet CvrEditors = RoleSet.Of(JpmsRoles.Director, JpmsRoles.Estimator);

    // Replica of CaptureCashflowSnapshotAuthorisation.RolesThatMayCaptureCashflow.
    private static readonly RoleSet CashflowCapturers =
        RoleSet.Of(JpmsRoles.Director, JpmsRoles.FinanceDirector);

    public IEnumerable<AiAction> Build() =>
        ClaimsAndValuationsActions()
            .Concat(SnapshotsActions())
            .Concat(FinancialsActions())
            .Concat(InputsActions())
            .Concat(CvrActions())
            .Concat(CashflowActions())
            .Concat(XeroAllocationActions())
            .Concat(WorkOrderBillActions());

    // No skipped endpoints: every command endpoint under Features/Commercial,
    // Features/CommercialInputs, Features/Cvr and Features/Cashflow dispatches an
    // ICommandHandler with JSON-body or route-parameter binding, and none are already
    // exposed by AiWriteTools.
}
