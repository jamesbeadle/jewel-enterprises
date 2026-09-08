using Jewel.JPMS.Api.Features.Xero;
using static Jewel.JPMS.Api.Features.Labour.Commands.XeroCodingWording;

namespace Jewel.JPMS.Api.Features.Labour.Commands;

public sealed partial class RunXeroCodingHandler
{
    /// <summary>One schedule line as the Xero line it becomes, and whose month it settles — the
    /// cover the recode marks on the resulting line is that worker's.</summary>
    private sealed record CodedLine(WorkerRun Worker, XeroScheduleLine Line);

    /// <summary>Gate 3 for the party: every worker's schedule as the Xero lines it would become,
    /// worker by worker, or the gap that names every mapping hole per worker.</summary>
    private (List<CodedLine> Lines, Dictionary<string, string> Gaps) ScheduleAsXeroLines(CodingParty party)
    {
        var lines = new List<CodedLine>();
        var gaps = new Dictionary<string, string>();
        foreach (var worker in party.Workers)
        {
            var (workerLines, gap) = ScheduleAsXeroLines(worker);
            if (gap is not null) { gaps[worker.WorkerId] = gap; continue; }
            lines.AddRange(workerLines.Select(line => new CodedLine(worker, line)));
        }
        return (lines, gaps);
    }

    /// <summary>Every schedule line as the Xero line it would become, or the skip that names
    /// every mapping gap.</summary>
    private (List<XeroScheduleLine> Lines, string? Gap) ScheduleAsXeroLines(WorkerRun run)
    {
        var gaps = new List<string>();
        var xeroLines = new List<XeroScheduleLine>();
        foreach (var line in run.Schedule.Lines)
        {
            var (xeroLine, gap) = XeroLineFor(run, line);
            if (gap is not null) { gaps.Add(gap); continue; }
            xeroLines.Add(xeroLine!);
        }
        if (gaps.Count > 0)
            return (xeroLines, "Mapping gaps: " + string.Join("; ", gaps.Distinct()) + ". Fix the Xero mapping and re-run.");
        if (xeroLines.Count == 0)
            return (xeroLines, "Nothing to code — the schedule has no lines.");
        return (xeroLines, null);
    }

    private (XeroScheduleLine? Line, string? Gap) XeroLineFor(WorkerRun run, ScheduleLine line)
    {
        var site = FindSiteMapping(line.ProjectId);
        if (site is null) return (null, $"site \"{line.ProjectName}\" has no Xero tracking option mapped");
        var code = FindCodeMapping(line.CostCode);
        if (code is null) return (null, $"cost code {line.CostCode} has no Xero mapping");
        var account = line.Nature switch
        {
            SettlementLineNature.CisLabour => code.LabourAccountCode,
            SettlementLineNature.CisMaterials => code.MaterialsAccountCode,
            _ => code.TravelAccountCode,
        };
        if (string.IsNullOrWhiteSpace(account)) return (null, $"cost code {line.CostCode} has no {line.Nature} account code");
        var description = $"{run.WorkerName} — {line.ProjectName} [{line.CostCode}] {NatureLabel(line.Nature)} {run.MonthStart:MMM yyyy}";
        var costCodeOption = string.IsNullOrWhiteSpace(code.XeroTrackingOptionName) ? line.CostCode : code.XeroTrackingOptionName;
        return (new XeroScheduleLine(description, line.Amount, account, site.XeroTrackingOptionName, costCodeOption), null);
    }
}
