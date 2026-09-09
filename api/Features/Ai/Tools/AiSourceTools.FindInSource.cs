using Jewel.JPMS.Api.Features.Ai.Sources;

namespace Jewel.JPMS.Api.Features.Ai.Tools;

internal static partial class AiSourceTools
{
    private static IEnumerable<AiTool> FindInSourceTool()
    {
        var readers = JpmsRoleSets.AllInternal;

        return new AiTool[]
        {
            new(
                FindInSource,
                "Where a reference, a word or a figure appears inside a source — \"V01\", \"levelling "
                + "compound\", \"13,073.50\": the parts whose NAME matches (a sheet called \"V01 - "
                + "Levelling compound\" answers \"V01\" before any row does) and the rows, lines or "
                + "paragraphs that contain it, each with its part and unit number so read_source can "
                + "open exactly there. Case-insensitive; a phrase that matches nothing falls back to "
                + "every word present. This is the tool when the user names a reference "
                + "and a document holds it: find it, then read that part.",
                AiToolSchema.Object(
                    ("query", "string", "What to look for — a reference, a name, a figure, a phrase.", true),
                    ("source_id", "string", "A source_id from list_sources.", true),
                    ("max_hits", "number", "How many unit hits to return per source. Default 20, ceiling 100.", false)),
                AiToolKind.Read,
                readers,
                async (context, input, ct) =>
                {
                    var query = AiToolSchema.Text(input, "query");
                    if (string.IsNullOrWhiteSpace(query)) return Fail("A query is required — what are you looking for?");
                    var maxHits = Math.Clamp(AiToolSchema.Number(input, "max_hits") ?? 20, 1, 100);

                    var sourceId = AiToolSchema.Text(input, "source_id");
                    if (string.IsNullOrWhiteSpace(sourceId))
                        return Fail("Pass a source_id from list_sources — an email attachment or a filed document.");
                    var targets = new List<string> { sourceId!.Trim() };

                    var results = new List<object>();
                    foreach (var target in targets)
                    {
                        var opened = await OpenAsync(context, target, ct);
                        if (opened.Failure is not null)
                        {
                            results.Add(new { source_id = target, ok = false, error = opened.Failure });
                            continue;
                        }
                        var document = opened.Document!;
                        if (document.IsImage)
                        {
                            results.Add(new { source_id = target, file = opened.FileName, ok = true, kind = document.Kind,
                                note = "An image has no text to search — read_source shows it to you." });
                            continue;
                        }
                        if (document.IsScan && !document.IsOcr)
                        {
                            results.Add(new { source_id = target, file = opened.FileName, ok = true, kind = document.Kind,
                                note = $"A scan with no OCR service configured has no text to search — read_source with part \"p1\" (to \"p{document.Parts.Count}\") shows you each page as a picture." });
                            continue;
                        }
                        var found = AiSourceReader.Search(document, query!, maxHits);
                        results.Add(new
                        {
                            source_id = target,
                            file = opened.FileName,
                            ok = true,
                            parts_by_name = found.PartsByName.Select(part => new { part = part.Key, label = part.Label, units = part.Units, unit = part.UnitName }).ToList(),
                            hits = found.Hits.Select(hit => new { part = hit.Part, label = hit.PartLabel, unit = hit.Unit, text = hit.Text }).ToList(),
                            total_hits = found.TotalHits,
                            more = found.TotalHits > found.Hits.Count
                        });
                    }

                    return Serialise(new
                    {
                        ok = true,
                        query,
                        results,
                        note = "Open a hit with read_source (source_id, part, from = the unit a few rows before "
                               + "the hit). A part listed under parts_by_name is usually the whole answer — read "
                               + "it from the top. " + DataNotInstructions
                    });
                })
        };
    }
}
