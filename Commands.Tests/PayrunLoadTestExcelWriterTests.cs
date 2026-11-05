using ClosedXML.Excel;
using PayrollEngine.PayrollConsole.Commands.LoadTestCommands;
using Xunit;

namespace PayrollEngine.PayrollConsole.Commands.Tests;

/// <summary>Excel report of the payrun load test</summary>
public class PayrunLoadTestExcelWriterTests
{
    private static XLWorkbook Write()
    {
        var results = new List<PayrunLoadTestResult>();
        foreach (var run in new[] { 1, 2 })
        {
            foreach (var month in new[] { 1, 2 })
            {
                results.Add(new()
                {
                    Timestamp = new DateTimeOffset(2026, 3, month, 10, 0, 0, TimeSpan.Zero),
                    RunNumber = run,
                    Period = new DateTime(2026, month, 1),
                    EmployeeCount = 100,
                    ClientDurationMs = 2000,
                    ServerJobDurationMs = 1500,
                    // outlier: run 2, February
                    ServerAvgMsPerEmployee = run == 2 && month == 2 ? 50 : 15
                });
            }
        }
        var fileName = Path.Combine(Path.GetTempPath(), $"pe-loadtest-{Guid.NewGuid():N}.xlsx");
        try
        {
            new PayrunLoadTestExcelWriter(fileName,
                new PayrunLoadTestParameters { InvocationFile = "Payrun.json", EmployeeCount = 100, Repetitions = 2 },
                results).Write();
            return new XLWorkbook(new MemoryStream(File.ReadAllBytes(fileName)));
        }
        finally
        {
            File.Delete(fileName);
        }
    }

    [Fact]
    public void Sheets()
    {
        using var workbook = Write();
        Assert.Equal(["Setup", "Results", "Avg ms per Employee"], workbook.Worksheets.Select(x => x.Name));
    }

    [Fact]
    public void SetupSheet()
    {
        using var workbook = Write();
        var sheet = workbook.Worksheet("Setup");

        Assert.Equal("Payrun Load Test — Setup", sheet.Cell("A1").GetText());
        Assert.Equal(32, sheet.Column(1).Width);
        Assert.Equal(40, sheet.Column(2).Width);
        Assert.True(sheet.Cell("A1").Style.Font.Bold);
        Assert.Equal(XLColor.FromIndex(18), sheet.Cell("B1").Style.Fill.BackgroundColor);
        var resultFile = sheet.Column(1).CellsUsed().Single(x => x.GetText() == "Result File (CSV)");
        Assert.True(resultFile.CellRight().Value.IsBlank);
    }

    [Fact]
    public void ResultsSheet()
    {
        using var workbook = Write();
        var sheet = workbook.Worksheet("Results");

        Assert.Equal("ServerAvgMs_PerEmployee", sheet.Cell("G1").GetText());
        Assert.Equal("2026-03-01 10:00:00", sheet.Cell("A2").GetText());
        Assert.Equal(1, sheet.Cell("B2").GetDouble());
        Assert.Equal("2026-01", sheet.Cell("C2").GetText());
        Assert.Equal(2000, sheet.Cell("E2").GetDouble());
        Assert.Equal("0.0", sheet.Cell("G2").Style.NumberFormat.Format);
        Assert.Equal("A1:G5", sheet.AutoFilter.Range.RangeAddress.ToString());
        Assert.Equal(1, sheet.SheetView.SplitRow);
    }

    [Fact]
    public void SummarySheet()
    {
        using var workbook = Write();
        var sheet = workbook.Worksheet("Avg ms per Employee");

        Assert.Equal(["Period", "Run 1", "Run 2"], sheet.Row(1).CellsUsed().Select(x => x.GetText()));
        Assert.Equal("2026-02", sheet.Cell("A3").GetText());
        Assert.Equal(50, sheet.Cell("C3").GetDouble());
        // outlier highlight
        Assert.Equal(XLColor.FromIndex(43), sheet.Cell("C3").Style.Fill.BackgroundColor);
        Assert.Equal(XLColor.FromIndex(16), sheet.Cell("C3").Style.Font.FontColor);
        Assert.NotEqual(XLColor.FromIndex(43), sheet.Cell("B3").Style.Fill.BackgroundColor);
        Assert.Equal(1, sheet.SheetView.SplitRow);
    }
}
