namespace Jewel.JPMS.Models;

public static class JpmsAdministrators
{
    private static readonly IReadOnlySet<string> Emails = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
    {
        "admin.james@jewelenterprises.co.uk",
        "Nigel.Reilly@jewelenterprises.co.uk"
    };

    public static bool Contains(string email) =>
        !string.IsNullOrWhiteSpace(email) && Emails.Contains(email.Trim());
}
