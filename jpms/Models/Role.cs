namespace Jewel.JPMS.Models;

public enum Role
{
    Admin,
    ManagingDirector,
    Accountant,
    QuantitySurveyor,
    Architect,
    Subcontractor
}

public static class RoleExtensions
{
    public static string DisplayName(this Role role) => role switch
    {
        Role.Admin            => "Administrator",
        Role.ManagingDirector => "Managing Director",
        Role.Accountant       => "Accountant",
        Role.QuantitySurveyor => "Quantity Surveyor",
        Role.Architect        => "Architect",
        Role.Subcontractor    => "Subcontractor",
        _ => role.ToString()
    };

    public static string PersonaCode(this Role role) => role switch
    {
        Role.Admin            => "ADM",
        Role.ManagingDirector => "P05",
        Role.Accountant       => "P04",
        Role.QuantitySurveyor => "P02",
        Role.Architect        => "P01",
        Role.Subcontractor    => "P03",
        _ => role.ToString().ToUpperInvariant()
    };

    public static bool IsAdministrative(this Role role) => role == Role.Admin;

    public static string AccentDotClass(this Role role) => role switch
    {
        Role.Admin            => "bg-slate-900",
        Role.ManagingDirector => "bg-rose-500",
        Role.Accountant       => "bg-violet-500",
        Role.QuantitySurveyor => "bg-emerald-500",
        Role.Architect        => "bg-sky-500",
        Role.Subcontractor    => "bg-amber-500",
        _ => "bg-slate-400"
    };
}
