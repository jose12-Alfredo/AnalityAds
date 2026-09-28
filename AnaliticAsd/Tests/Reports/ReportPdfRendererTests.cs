using System.Text;
using AnaliticAsd.Application.Analysis;
using AnaliticAsd.Application.Reports;
using AnaliticAsd.Infrastructure.Reports;

namespace AnaliticAsd.Tests.Reports;

public sealed class ReportPdfRendererTests
{
    [Fact]
    public void Renderer_paginates_long_snapshots_and_writes_pdf_page_tree()
    {
        var campaigns = Enumerable.Range(1, 80).Select(i => Entity(Guid.NewGuid(), $"Campaign {i}")).ToArray();
        var account = Entity(Guid.NewGuid(), "Account");
        var analysis = new AnalysisResultModel(account.Id, Guid.NewGuid(), new(2026, 9, 1), new(2026, 9, 5),
            null, ["spend"], DateTimeOffset.Parse("2026-09-15T12:00:00Z"), account, campaigns, [], [], [], [], []);
        var report = new ReportDataModel(Guid.NewGuid(), 1, "Long report", analysis.ClientId, analysis.AdAccountId,
            Guid.NewGuid(), DateTimeOffset.Parse("2026-09-15T12:00:00Z"), new string('A', 64), analysis);

        var bytes = new SimpleReportPdfRenderer().Render(report);
        var text = Encoding.Latin1.GetString(bytes);

        Assert.StartsWith("%PDF-1.7", text);
        Assert.Contains("/Type /Pages /Count ", text);
        Assert.DoesNotContain("/Type /Pages /Count 1 ", text);
        Assert.EndsWith("%%EOF", text);
    }

    private static AnalysisEntityModel Entity(Guid id, string name) => new("Campaign", id, null, name, "LEADS", "USD",
        new(5, 5, 0, new(2026, 9, 1), new(2026, 9, 5)), new("Sufficient", []),
        new Dictionary<string, AnalysisMetricModel> { ["spend"] = new(10m, "CompleteForSnapshots", "Observed") },
        null, null);
}
