using System.Globalization;

namespace Jewel.JPMS.Features.Site;

// The Gantt's one ruler. Every row — task, baseline, variation push, extension of time — is placed
// on it, so the sections beneath the tasks line up with the tasks above them. Built once per
// render from every date the chart will draw, with a few days' margin either side.
public sealed class ProgrammeTimeline
{
    private const int MarginDays = 3;
    private const double NarrowestBarPercent = 0.5;

    private readonly DateTimeOffset start;
    private readonly DateTimeOffset end;

    private ProgrammeTimeline(DateTimeOffset start, DateTimeOffset end)
    {
        this.start = start;
        this.end = end;
    }

    public static ProgrammeTimeline Spanning(IEnumerable<DateTimeOffset> dates)
    {
        var known = dates.ToList();
        if (known.Count == 0) return new ProgrammeTimeline(DateTimeOffset.UtcNow, DateTimeOffset.UtcNow.AddDays(1));
        var start = known.Min().AddDays(-MarginDays);
        var end = known.Max().AddDays(MarginDays);
        if (end <= start) end = start.AddDays(1);
        return new ProgrammeTimeline(start, end);
    }

    private double TotalDays => (end - start).TotalDays;

    private double PercentAlong(DateTimeOffset date) => (date - start).TotalDays / TotalDays * 100;

    public string BarStyle(DateTimeOffset barStart, DateTimeOffset barEnd)
    {
        var left = Math.Max(PercentAlong(barStart), 0);
        var width = Math.Max((barEnd - barStart).TotalDays / TotalDays * 100, NarrowestBarPercent);
        return $"left:{Percent(left)};width:{Percent(Math.Min(width, 100 - left))}";
    }

    /// <summary>Where a single date sits, or null when it falls off the ruler.</summary>
    public string? PointStyle(DateTimeOffset date)
    {
        if (date < start || date > end) return null;
        return $"left:{Percent(PercentAlong(date))}";
    }

    public string? TodayStyle => PointStyle(DateTimeOffset.UtcNow);

    public IEnumerable<(string Label, string Style)> MonthMarks
    {
        get
        {
            var month = new DateTimeOffset(start.Year, start.Month, 1, 0, 0, 0, TimeSpan.Zero).AddMonths(1);
            while (month < end)
            {
                yield return (month.ToString("MMM yy"), $"left:{Percent(PercentAlong(month))}");
                month = month.AddMonths(1);
            }
        }
    }

    public static string Percent(double value) =>
        value.ToString("0.###", CultureInfo.InvariantCulture) + "%";
}
