namespace Jewel.JPMS.Models;

public enum ProjectStage
{
    Lead,
    PreConstruction,
    Procurement,
    Mobilisation,
    LiveDelivery,
    CloseOut,
    DefectsPeriod,
    Completed
}

public static class ProjectStageExtensions
{
    public static string DisplayName(this ProjectStage stage) => stage switch
    {
        ProjectStage.Lead             => "Lead",
        ProjectStage.PreConstruction  => "Pre-Construction",
        ProjectStage.Procurement      => "Procurement",
        ProjectStage.Mobilisation     => "Mobilisation",
        ProjectStage.LiveDelivery     => "Live Delivery",
        ProjectStage.CloseOut         => "Close-Out",
        ProjectStage.DefectsPeriod    => "Defects Period",
        ProjectStage.Completed        => "Completed",
        _ => stage.ToString()
    };

    public static string AccentDotClass(this ProjectStage stage) => stage switch
    {
        ProjectStage.Lead             => "bg-slate-400",
        ProjectStage.PreConstruction  => "bg-violet-500",
        ProjectStage.Procurement      => "bg-indigo-500",
        ProjectStage.Mobilisation     => "bg-amber-500",
        ProjectStage.LiveDelivery     => "bg-emerald-500",
        ProjectStage.CloseOut         => "bg-sky-500",
        ProjectStage.DefectsPeriod    => "bg-rose-400",
        ProjectStage.Completed        => "bg-slate-900",
        _ => "bg-slate-400"
    };
}
