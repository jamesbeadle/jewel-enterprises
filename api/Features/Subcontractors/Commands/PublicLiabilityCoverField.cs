using System.Globalization;
using Jewel.JPMS.Contracts.Subcontractors;
using Microsoft.AspNetCore.Http;

namespace Jewel.JPMS.Api.Features.Subcontractors.Commands;

/// <summary>
/// The one reading of a public liability figure as it arrives — the multipart uploads' optional
/// <c>publicLiabilityCover</c> form field (office "Add document…", the portal self-upload) and
/// the JSON commands' decimal — so every route accepts the same spellings ("5000000",
/// "£5,000,000", "5000000.00") and refuses the same nonsense with the same words.
/// </summary>
public static class PublicLiabilityCoverField
{
    public const string FormFieldName = "publicLiabilityCover";

    /// <summary>The column is decimal(18,4): fourteen integer digits. A limit of indemnity is
    /// never anywhere near it, so anything larger is a typo, not a policy.</summary>
    public const decimal Maximum = 99_999_999_999_999m;

    /// <summary>Reads the optional form field. An absent or blank field is null (not recorded);
    /// a value that is not a non-negative amount answers with the message to return as a 400.</summary>
    public static bool TryRead(IFormCollection form, out decimal? cover, out string? error)
    {
        cover = null;
        error = null;
        var raw = form[FormFieldName].ToString();
        if (string.IsNullOrWhiteSpace(raw)) return true;

        var cleaned = raw.Trim().Replace("£", "").Replace(",", "").Trim();
        if (!decimal.TryParse(cleaned, NumberStyles.Number, CultureInfo.InvariantCulture, out var parsed))
        {
            error = $"{FormFieldName} must be an amount in pounds (e.g. 5000000).";
            return false;
        }
        var problem = Check(parsed);
        if (problem is not null) { error = problem; return false; }
        cover = parsed;
        return true;
    }

    /// <summary>The rule a JSON command's figure is held to (null passes — not recorded).</summary>
    public static string? Check(decimal? cover)
    {
        if (cover is not { } amount) return null;
        if (amount < 0) return "Public liability cover can't be negative.";
        if (amount > Maximum) return "Public liability cover is implausibly large — enter the figure in pounds.";
        return null;
    }
}
