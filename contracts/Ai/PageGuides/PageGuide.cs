namespace Jewel.JPMS.Contracts.Ai;

/// <summary>
/// One portal page's working guide — the full version of the one-line note the site map pins into
/// every prompt (jpms PortalMapCapabilities). The map tells the model a page exists; the guide
/// tells it how the page WORKS: what a person does there manually, which of the assistant's verbs
/// apply there, and what is deliberately done elsewhere. Loaded on demand through the
/// load_page_guide tool, never pinned — sixty guides in every prompt would drown the turn.
///
/// <para>Guides are developer-owned mechanics, same side of the ownership split as the tool
/// catalogue: a page change and its guide change ship in the same commit. Domain judgement stays
/// in skills.</para>
/// </summary>
public sealed record PageGuide(
    /// <summary>The route template in the site map's own spelling ("/projects/{project}/requests").
    /// A {parameter} segment matches any real id, so a concrete route resolves to its guide.</summary>
    string RouteTemplate,
    string DisplayName,
    /// <summary>Prose written FOR the model: what the page is, the manual workflow, the assistant's
    /// verbs there, and what is not done there. Facts only — every claim traced to the page source
    /// when the guide was written.</summary>
    string Guide,
    /// <summary>Legacy or alternate routes that resolve to this same guide ("/requests/triage").</summary>
    IReadOnlyList<string>? Aliases = null);

/// <summary>
/// Every page guide, and the route matcher the load_page_guide tool resolves through. Split across
/// one data file per site area so each stays reviewable; this class only concatenates them.
/// </summary>
public static class PageGuideCatalogue
{
    public static IReadOnlyList<PageGuide> All { get; } =
        TriagePageGuides.Guides
            .Concat(RequestPageGuides.Guides)
            .Concat(CommercialPageGuides.Guides)
            .Concat(ProcurementPageGuides.Guides)
            .Concat(FinancePageGuides.Guides)
            .Concat(SitePageGuides.Guides)
            .Concat(OfficePageGuides.Guides)
            .ToList();

    /// <summary>The guide for a route — template or concrete — or null. Query strings ignored.
    /// The MOST LITERAL match wins, exactly as Blazor's router prefers a literal route over a
    /// parameterised one: "/projects/x/documents/ambiguous" belongs to the ambiguous-revisions
    /// guide, not to "/projects/{project}/documents/{drawingId}" swallowing "ambiguous" as an id.</summary>
    public static PageGuide? FindForRoute(string? route)
    {
        if (string.IsNullOrWhiteSpace(route)) return null;

        var path = route.Split('?', 2)[0].Trim().TrimEnd('/');
        if (path.Length == 0) path = "/";

        PageGuide? best = null;
        var bestSpecificity = -1;
        foreach (var guide in All)
        {
            var specificity = SpecificityFor(guide, path);
            if (specificity <= bestSpecificity) continue;
            best = guide;
            bestSpecificity = specificity;
        }
        return best;
    }

    /// <summary>The guide's best specificity over its template and aliases; -1 when none match.</summary>
    private static int SpecificityFor(PageGuide guide, string path)
    {
        var best = Specificity(guide.RouteTemplate, path);
        if (guide.Aliases is null) return best;
        foreach (var alias in guide.Aliases)
            best = Math.Max(best, Specificity(alias, path));
        return best;
    }

    /// <summary>Segment-wise match score: -1 when the template does not match the path at all;
    /// otherwise the count of segments matched LITERALLY. A literal segment must equal the path's;
    /// a {parameter} segment (on either side — the caller may pass the map's own template spelling)
    /// matches anything but scores nothing, so the most literal candidate wins overall.</summary>
    private static int Specificity(string template, string path)
    {
        var templateSegments = template.Trim().TrimEnd('/').Split('/');
        var pathSegments = path.Split('/');
        if (templateSegments.Length != pathSegments.Length) return -1;

        var literalMatches = 0;
        for (var index = 0; index < templateSegments.Length; index++)
        {
            var expected = templateSegments[index];
            var isParameter = expected.StartsWith('{') && expected.EndsWith('}');
            var actualIsParameter = pathSegments[index].StartsWith('{') && pathSegments[index].EndsWith('}');
            if (isParameter || actualIsParameter) continue;
            if (!string.Equals(expected, pathSegments[index], StringComparison.OrdinalIgnoreCase)) return -1;
            literalMatches++;
        }
        return literalMatches;
    }
}
