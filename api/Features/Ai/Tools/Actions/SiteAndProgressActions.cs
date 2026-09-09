using Jewel.JPMS.Api.Features.Closeout.Commands;
using Jewel.JPMS.Api.Features.Drawings.Commands;
using Jewel.JPMS.Api.Features.Progress;
using Jewel.JPMS.Api.Features.Progress.Commands;
using Jewel.JPMS.Api.Features.Requests;
using Jewel.JPMS.Api.Features.Site.Commands;
using Jewel.JPMS.Api.Features.Todos;
using Jewel.JPMS.Api.Features.Todos.Commands;
using Jewel.JPMS.Contracts.Closeout;
using Jewel.JPMS.Contracts.Drawings;
using Jewel.JPMS.Contracts.Progress;
using Jewel.JPMS.Contracts.Site;
using Jewel.JPMS.Contracts.Todos;

namespace Jewel.JPMS.Api.Features.Ai.Tools.Actions;

/// <summary>To-do, site, progress/programme, drawing and closeout commands as connector actions.
/// Mirrors Features/Todos, Features/Site, Features/Progress, Features/Drawings and
/// Features/Closeout — each entry's VisibleTo copies its Authorisation class's role set, and the
/// stamps copy exactly what the endpoint stamps server-side. The to-do commands AiWriteTools
/// already exposes as first-class tools (add_todo → AddTodoItem/AddGeneralTodoItem, complete_todo
/// → UpdateTodoItem, log_todo_progress → LogTodoProgress) are deliberately absent here.</summary>
internal sealed partial class SiteAndProgressActions : IAiActionSource
{
    // Replicates the private Director+PM sets RolesThatMayEditProgramme (AddProgrammeTask,
    // AddProgrammeTaskLink, RemoveProgrammeTask, RemoveProgrammeTaskLink), RolesThatMayBaseline
    // (TakeProgrammeBaseline, RemoveProgrammeBaseline) and RolesThatMayApproveReports
    // (ApproveSiteReport) in the Site authorisations.
    private static readonly RoleSet ProgrammePlanners =
        RoleSet.Of(JpmsRoles.Director, JpmsRoles.ProjectManager);

    // Replicates the private Director+PM+SiteManager sets RolesThatMayAssembleReports
    // (AssembleSiteReportAuthorisation), RolesThatMayEditProgramme
    // (UpdateProgrammeTaskAuthorisation), RolesThatMayRaiseDefectsFromMail
    // (CreateDefectFromMessageAuthorisation) and RolesThatMayUpdateDefects
    // (UpdateDefectAuthorisation).
    private static readonly RoleSet SiteTeamManagers =
        RoleSet.Of(JpmsRoles.Director, JpmsRoles.ProjectManager, JpmsRoles.SiteManager);

    // Replicates the private RolesThatMayManageDrawings set in ApproveDrawingRevision,
    // DeleteDrawing and DeleteDrawingRevision authorisations — Administrator, Managing Director,
    // Project Manager.
    private static readonly RoleSet DrawingManagers =
        RoleSet.Of(Role.Admin, JpmsRoles.Director, JpmsRoles.ProjectManager);

    // Replicates the private RolesThatMayRegisterDrawings (RegisterDrawingAuthorisation) and
    // RolesThatMayManageFolders (the DrawingFolderCommandAuthorisation classes) sets.
    private static readonly RoleSet DrawingRegisterCurators =
        RoleSet.Of(JpmsRoles.Director, JpmsRoles.ProjectManager, JpmsRoles.Estimator);

    // Replicates the private RolesThatMayEditDrawings set in SetDrawingRevisionLabel and
    // UpdateDrawingMetadata authorisations — Director, Project Manager.
    private static readonly RoleSet DrawingEditors =
        RoleSet.Of(JpmsRoles.Director, JpmsRoles.ProjectManager);

    // Replicates the private Director+FinanceDirector sets RolesThatMayAgreeSettlement,
    // RolesThatMayAgreeVat and RolesThatMayReleaseRetention in the Closeout authorisations.
    private static readonly RoleSet CloseoutDirectors =
        RoleSet.Of(JpmsRoles.Director, JpmsRoles.FinanceDirector);

    // Replicates the private RolesThatMayRaiseDefects set in RaiseDefectAuthorisation — includes
    // the external Client and Architect, who may raise defects through their own pages.
    private static readonly RoleSet DefectRaisers = RoleSet.Of(
        JpmsRoles.Director, JpmsRoles.ProjectManager, JpmsRoles.SiteManager,
        JpmsRoles.Client, JpmsRoles.Architect);

    public IEnumerable<AiAction> Build() =>
        ToDosActions()
            .Concat(SiteActions())
            .Concat(ProgressProgrammeActions())
            .Concat(ProgrammeVariationActions())
            .Concat(DrawingsActions())
            .Concat(CloseoutDefectsActions());

    // Skipped: AddTodoItem — already a first-class AiWriteTools tool (add_todo).
    // Skipped: AddGeneralTodoItem — already a first-class AiWriteTools tool (add_todo).
    // Skipped: UpdateTodoItem — already dispatched by AiWriteTools (complete_todo).
    // Skipped: LogTodoProgress — already a first-class AiWriteTools tool (log_todo_progress).
    // Skipped: CreateProgressUpdate — multipart/form-data photo upload; cannot fit the pattern.
    // Skipped: AddProgressPhotos — multipart/form-data photo upload; cannot fit the pattern.
    // Skipped: UploadDrawingRevision — multipart/form-data file upload; cannot fit the pattern.
}
