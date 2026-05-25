namespace Jewel.JPMS.Models;

public enum Organisation
{
    JewelBespokeBuild,
    JewelPrestige,
    JewelPropertyFinance
}

public static class OrganisationExtensions
{
    public static string ShortCode(this Organisation organisation) => organisation switch
    {
        Organisation.JewelBespokeBuild    => "JBB",
        Organisation.JewelPrestige        => "JPS",
        Organisation.JewelPropertyFinance => "JPF",
        _ => organisation.ToString()
    };

    public static string DisplayName(this Organisation organisation) => organisation switch
    {
        Organisation.JewelBespokeBuild    => "Jewel Bespoke Build",
        Organisation.JewelPrestige        => "Jewel Prestige",
        Organisation.JewelPropertyFinance => "Jewel Property Finance",
        _ => organisation.ToString()
    };
}
