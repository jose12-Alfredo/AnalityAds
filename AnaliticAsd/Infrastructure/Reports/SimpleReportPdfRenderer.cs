using System.Globalization;
using System.Text;
using AnaliticAsd.Application.Analysis;
using AnaliticAsd.Application.Reports;

namespace AnaliticAsd.Infrastructure.Reports;

public sealed class SimpleReportPdfRenderer : IReportPdfRenderer
{
    private const decimal PageHeight = 792m;
    private const decimal Top = 738m;
    private const decimal Bottom = 54m;
    private const int WrapWidth = 88;

    public byte[] Render(ReportDataModel report)
    {
        var lines = BuildLines(report);
        var pages = Paginate(lines);
        return Pdf(pages, report.Title);
    }

    private static List<Line> BuildLines(ReportDataModel report)
    {
        var a = report.Analysis;
        var lines = new List<Line>
        {
            new(report.Title, 20, 29),
            new($"Period: {a.Since:yyyy-MM-dd} to {a.Until:yyyy-MM-dd}", 10, 17),
            new($"Generated: {report.CreatedAtUtc:yyyy-MM-dd HH:mm} UTC | Report {report.ReportId}", 8, 14),
            new("EXECUTIVE SUMMARY", 13, 23)
        };
        foreach (var metric in a.Account.Metrics.Where(x => x.Value.Value is not null))
            lines.Add(new($"{metric.Key}: {Number(metric.Value.Value)} [{metric.Value.Availability}]", 10, 15));
        lines.Add(new("DATA QUALITY", 13, 23));
        lines.Add(new($"Coverage: {a.Account.Coverage.SnapshotDays}/{a.Account.Coverage.RequestedDays} days | Sufficiency: {a.Account.Sufficiency.Status}", 10, 15));
        foreach (var reason in a.Account.Sufficiency.Reasons) lines.Add(new($"- {reason}", 9, 14));
        lines.Add(new("INSIGHTS", 13, 23));
        if (a.Insights.Count == 0) lines.Add(new("No conclusion met the configured evidence threshold.", 10, 16));
        foreach (var insight in a.Insights)
        {
            lines.Add(new($"{insight.RuleId} | {insight.Severity} | Confidence {insight.Confidence}", 11, 18));
            AddWrapped(lines, insight.Message, 10, 15);
            foreach (var e in insight.Evidence)
                lines.Add(new($"- {e.Metric}: {Number(e.Value)} | reference {Number(e.ReferenceValue)} | difference {Number(e.PercentageDifference)}%", 9, 14));
        }
        lines.Add(new("RECOMMENDATIONS", 13, 23));
        if (a.Recommendations.Count == 0) lines.Add(new("No recommendation was generated with sufficient evidence.", 10, 16));
        foreach (var recommendation in a.Recommendations)
        {
            lines.Add(new($"{recommendation.RuleId} | Priority {recommendation.Priority}", 11, 18));
            AddWrapped(lines, recommendation.Message, 10, 15);
            foreach (var action in recommendation.Actions) AddWrapped(lines, $"- {action}", 9, 14);
        }
        lines.Add(new("CAMPAIGNS", 13, 23));
        foreach (var campaign in a.Campaigns)
        {
            lines.Add(new($"{campaign.Name} | {campaign.Objective} | {campaign.Sufficiency.Status}", 11, 18));
            var metrics = string.Join(" | ", campaign.Metrics.Where(x => x.Value.Value is not null)
                .Select(x => $"{x.Key}: {Number(x.Value.Value)}"));
            AddWrapped(lines, metrics.Length == 0 ? "No complete selected metrics." : metrics, 9, 14);
        }
        lines.Add(new("METHODOLOGY", 13, 23));
        AddWrapped(lines, "This report is an immutable snapshot of the authorized AnalysisEngine result. Observed and derived data remain distinguished. Empty or unavailable evidence is never converted to zero. Insights are deterministic and do not establish causality.", 9, 14);
        AddWrapped(lines, $"Schema {report.SchemaVersion} | Snapshot SHA-256 {report.SnapshotHash}", 7, 12);
        return lines;
    }

    private static void AddWrapped(ICollection<Line> lines, string text, int size, decimal leading)
    {
        var words = text.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        var current = new StringBuilder();
        foreach (var word in words)
        {
            if (current.Length > 0 && current.Length + word.Length + 1 > WrapWidth)
            {
                lines.Add(new(current.ToString(), size, leading)); current.Clear();
            }
            if (current.Length > 0) current.Append(' ');
            current.Append(word);
        }
        if (current.Length > 0) lines.Add(new(current.ToString(), size, leading));
    }

    private static List<List<Line>> Paginate(IEnumerable<Line> lines)
    {
        var pages = new List<List<Line>> { new() };
        var y = Top;
        foreach (var line in lines)
        {
            if (y - line.Leading < Bottom) { pages.Add([]); y = Top; }
            pages[^1].Add(line); y -= line.Leading;
        }
        return pages;
    }

    private static byte[] Pdf(IReadOnlyList<List<Line>> pages, string title)
    {
        var objects = new List<byte[]>();
        objects.Add(Bytes("<< /Type /Catalog /Pages 2 0 R >>"));
        var kids = string.Join(' ', Enumerable.Range(0, pages.Count).Select(i => $"{4 + i * 2} 0 R"));
        objects.Add(Bytes($"<< /Type /Pages /Count {pages.Count} /Kids [{kids}] >>"));
        objects.Add(Bytes("<< /Type /Font /Subtype /Type1 /BaseFont /Helvetica /Encoding /WinAnsiEncoding >>"));
        for (var i = 0; i < pages.Count; i++)
        {
            var pageObject = 4 + i * 2;
            var contentObject = pageObject + 1;
            objects.Add(Bytes($"<< /Type /Page /Parent 2 0 R /MediaBox [0 0 612 {(int)PageHeight}] /Resources << /Font << /F1 3 0 R >> >> /Contents {contentObject} 0 R >>"));
            var content = Content(pages[i], i + 1, pages.Count, title);
            objects.Add(Concat(Bytes($"<< /Length {content.Length} >>\nstream\n"), content, Bytes("\nendstream")));
        }
        using var output = new MemoryStream();
        Write(output, Bytes("%PDF-1.7\n%\xE2\xE3\xCF\xD3\n"));
        var offsets = new List<long> { 0 };
        for (var i = 0; i < objects.Count; i++)
        {
            offsets.Add(output.Position);
            Write(output, Bytes($"{i + 1} 0 obj\n")); Write(output, objects[i]); Write(output, Bytes("\nendobj\n"));
        }
        var xref = output.Position;
        Write(output, Bytes($"xref\n0 {objects.Count + 1}\n0000000000 65535 f \n"));
        foreach (var offset in offsets.Skip(1)) Write(output, Bytes($"{offset:0000000000} 00000 n \n"));
        Write(output, Bytes($"trailer\n<< /Size {objects.Count + 1} /Root 1 0 R /Info << /Title ({Escape(title)}) >> >>\nstartxref\n{xref}\n%%EOF"));
        return output.ToArray();
    }

    private static byte[] Content(IReadOnlyList<Line> lines, int page, int pages, string title)
    {
        var b = new StringBuilder();
        b.AppendLine("0.12 0.24 0.42 rg 0 760 612 32 re f");
        b.AppendLine($"1 1 1 rg BT /F1 9 Tf 54 772 Td ({Escape(title)}) Tj ET");
        decimal y = Top;
        foreach (var line in lines)
        {
            b.AppendLine($"0.12 0.15 0.20 rg BT /F1 {line.Size} Tf 54 {y.ToString(CultureInfo.InvariantCulture)} Td ({Escape(line.Text)}) Tj ET");
            y -= line.Leading;
        }
        b.AppendLine($"0.35 0.38 0.42 rg BT /F1 8 Tf 510 28 Td (Page {page} of {pages}) Tj ET");
        return Encoding.Latin1.GetBytes(b.ToString());
    }

    private static string Escape(string value)
    {
        var normalized = value.Replace('\u2013', '-').Replace('\u2014', '-').Replace('\u2011', '-');
        return normalized.Replace("\\", "\\\\").Replace("(", "\\(").Replace(")", "\\)");
    }
    private static string Number(decimal? value) => value?.ToString("0.##", CultureInfo.InvariantCulture) ?? "-";
    private static byte[] Bytes(string value) => Encoding.Latin1.GetBytes(value);
    private static byte[] Concat(params byte[][] values) => values.SelectMany(x => x).ToArray();
    private static void Write(Stream stream, byte[] bytes) => stream.Write(bytes, 0, bytes.Length);
    private sealed record Line(string Text, int Size, decimal Leading);
}
