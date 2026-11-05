using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using ClosedXML.Excel;

namespace PayrollEngine.PayrollConsole.Commands.LoadTestCommands;

/// <summary>Writes payrun load test results to an Excel workbook</summary>
internal sealed class PayrunLoadTestExcelWriter
{
    private readonly string path;
    private readonly PayrunLoadTestParameters parameters;
    private readonly List<PayrunLoadTestResult> results;

    // colors (Excel indexed palette)
    private static readonly XLColor HeaderFillColor = XLColor.FromIndex(18);      // dark blue
    private static readonly XLColor HeaderFontColor = XLColor.FromIndex(9);       // white
    private static readonly XLColor HighlightFillColor = XLColor.FromIndex(43);   // light yellow
    private static readonly XLColor HighlightFontColor = XLColor.FromIndex(16);   // dark red

    private const string DecimalFormat = "0.0";

    private enum CellStyle
    {
        Header,
        MetaLabel,
        MetaValue,
        Number,
        Decimal,
        Period,
        Timestamp,
        Highlight
    }

    internal PayrunLoadTestExcelWriter(
        string path,
        PayrunLoadTestParameters parameters,
        List<PayrunLoadTestResult> results)
    {
        this.path = path;
        this.parameters = parameters;
        this.results = results;
    }

    /// <summary>Write the Excel report</summary>
    internal void Write()
    {
        using var workbook = new XLWorkbook();

        WriteSetupSheet(workbook);
        WriteResultsSheet(workbook);
        WriteSummarySheet(workbook);

        var directory = Path.GetDirectoryName(Path.GetFullPath(path));
        if (!string.IsNullOrEmpty(directory))
        {
            Directory.CreateDirectory(directory);
        }

        using var stream = new FileStream(path, FileMode.Create, FileAccess.Write);
        workbook.SaveAs(stream);
    }

    // setup sheet: machine, timing config, test parameters
    private void WriteSetupSheet(XLWorkbook workbook)
    {
        var sheet = workbook.Worksheets.Add("Setup");
        sheet.Column(1).Width = 32;
        sheet.Column(2).Width = 40;

        var row = 0;

        // title
        WriteHeader(sheet, row++, "Payrun Load Test — Setup");
        row++;

        // machine info
        WriteHeader(sheet, row++, "Machine");
        WriteMeta(sheet, row++, "Machine Name", Environment.MachineName);
        WriteMeta(sheet, row++, "OS", RuntimeInformation.OSDescription);
        WriteMeta(sheet, row++, "Framework", RuntimeInformation.FrameworkDescription);
        WriteMeta(sheet, row++, "Processor Count", Environment.ProcessorCount.ToString());
        row++;

        // backend config
        WriteHeader(sheet, row++, "Backend Configuration");
        WriteMeta(sheet, row++, "MaxParallelEmployees",
            string.IsNullOrWhiteSpace(parameters.ParallelSetting) ? "— (not specified)" : parameters.ParallelSetting);
        row++;

        // test parameters
        WriteHeader(sheet, row++, "Test Parameters");
        WriteMeta(sheet, row++, "Invocation File", parameters.InvocationFile);
        WriteMeta(sheet, row++, "Employee Count", parameters.EmployeeCount.ToString());
        WriteMeta(sheet, row++, "Repetitions", parameters.Repetitions.ToString());
        WriteMeta(sheet, row++, "Result File (CSV)", parameters.ResultFile);
        WriteMeta(sheet, row++, "Result File (Excel)", path);
        row++;

        // timing summary
        WriteHeader(sheet, row++, "Summary");
        var periods = results.Select(r => r.Period).Distinct().Count();
        WriteMeta(sheet, row++, "Periods", periods.ToString());

        var runTotals = results
            .GroupBy(r => r.RunNumber)
            .Select(g => (ServerTotal: g.Sum(r => r.ServerJobDurationMs),
                          EmployeeTotal: g.Sum(r => r.EmployeeCount)))
            .OrderBy(r => r.ServerTotal)
            .ToList();
        var median = runTotals[runTotals.Count / 2];
        WriteMeta(sheet, row++, "Median Server Total (ms)", median.ServerTotal.ToString());
        WriteMeta(sheet, row++, "Median Avg ms/Employee",
            (median.EmployeeTotal > 0 ? median.ServerTotal / (double)median.EmployeeTotal : 0).ToString("F1", CultureInfo.InvariantCulture));
        WriteMeta(sheet, row, "Test Date",
            results.FirstOrDefault()?.Timestamp.ToString("yyyy-MM-dd HH:mm", CultureInfo.InvariantCulture) ?? "—");
    }

    // results sheet: one row per period/run, same structure as CSV
    private void WriteResultsSheet(XLWorkbook workbook)
    {
        var sheet = workbook.Worksheets.Add("Results");
        sheet.Column(1).Width = 22;  // Timestamp
        sheet.Column(2).Width = 8;   // Run
        sheet.Column(3).Width = 12;  // Period
        sheet.Column(4).Width = 16;  // EmployeeCount
        sheet.Column(5).Width = 20;  // ClientDuration_ms
        sheet.Column(6).Width = 24;  // ServerJobDuration_ms
        sheet.Column(7).Width = 24;  // ServerAvgMs_PerEmployee

        // header row
        WriteCell(sheet, 0, 0, "Timestamp", CellStyle.Header);
        WriteCell(sheet, 0, 1, "Run", CellStyle.Header);
        WriteCell(sheet, 0, 2, "Period", CellStyle.Header);
        WriteCell(sheet, 0, 3, "EmployeeCount", CellStyle.Header);
        WriteCell(sheet, 0, 4, "ClientDuration_ms", CellStyle.Header);
        WriteCell(sheet, 0, 5, "ServerJobDuration_ms", CellStyle.Header);
        WriteCell(sheet, 0, 6, "ServerAvgMs_PerEmployee", CellStyle.Header);

        // data rows
        var rowIndex = 1;
        foreach (var r in results)
        {
            WriteCell(sheet, rowIndex, 0, r.Timestamp.ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture), CellStyle.Timestamp);
            WriteCell(sheet, rowIndex, 1, r.RunNumber, CellStyle.Number);
            WriteCell(sheet, rowIndex, 2, r.Period.ToString("yyyy-MM", CultureInfo.InvariantCulture), CellStyle.Period);
            WriteCell(sheet, rowIndex, 3, r.EmployeeCount, CellStyle.Number);
            WriteCell(sheet, rowIndex, 4, r.ClientDurationMs, CellStyle.Number);
            WriteCell(sheet, rowIndex, 5, r.ServerJobDurationMs, CellStyle.Number);
            WriteCell(sheet, rowIndex, 6, r.ServerAvgMsPerEmployee, CellStyle.Decimal);
            rowIndex++;
        }

        // auto-filter on header row
        sheet.Range(1, 1, rowIndex, 7).SetAutoFilter();
        sheet.SheetView.FreezeRows(1);
    }

    // summary sheet: pivot — period × run, ServerAvgMs/Employee
    private void WriteSummarySheet(XLWorkbook workbook)
    {
        var sheet = workbook.Worksheets.Add("Avg ms per Employee");

        var runs = results.Select(r => r.RunNumber).Distinct().OrderBy(x => x).ToList();
        var periods = results.Select(r => r.Period).Distinct().OrderBy(x => x).ToList();

        // column widths
        sheet.Column(1).Width = 16;
        for (var i = 0; i < runs.Count; i++)
        {
            sheet.Column(i + 2).Width = 14;
        }

        // header row: Period | Run 1 | Run 2 | ...
        WriteCell(sheet, 0, 0, "Period", CellStyle.Header);
        for (var i = 0; i < runs.Count; i++)
        {
            WriteCell(sheet, 0, i + 1, $"Run {runs[i]}", CellStyle.Header);
        }

        // data rows
        var lookup = results.ToDictionary(
            r => (r.Period, r.RunNumber),
            r => r.ServerAvgMsPerEmployee);

        var rowIndex = 1;
        foreach (var period in periods)
        {
            WriteCell(sheet, rowIndex, 0, period.ToString("yyyy-MM", CultureInfo.InvariantCulture), CellStyle.Period);
            for (var i = 0; i < runs.Count; i++)
            {
                var value = lookup.GetValueOrDefault((period, runs[i]), 0);
                // highlight outliers: >2× median avg
                var allValues = lookup.Values.Where(x => x > 0).OrderBy(x => x).ToList();
                var medianVal = allValues.Count > 0 ? allValues[allValues.Count / 2] : 0;
                var style = value > medianVal * 2 ? CellStyle.Highlight : CellStyle.Decimal;
                WriteCell(sheet, rowIndex, i + 1, value, style);
            }
            rowIndex++;
        }

        sheet.SheetView.FreezeRows(1);
    }

    // style helpers
    private static void ApplyStyle(IXLCell cell, CellStyle style)
    {
        var cellStyle = cell.Style;
        switch (style)
        {
            case CellStyle.Header:
                cellStyle.Font.Bold = true;
                cellStyle.Font.FontColor = HeaderFontColor;
                cellStyle.Fill.BackgroundColor = HeaderFillColor;
                cellStyle.Alignment.Horizontal = XLAlignmentHorizontalValues.Left;
                break;
            case CellStyle.MetaLabel:
                cellStyle.Font.Bold = true;
                break;
            case CellStyle.Number:
                cellStyle.Alignment.Horizontal = XLAlignmentHorizontalValues.Right;
                break;
            case CellStyle.Decimal:
                cellStyle.NumberFormat.Format = DecimalFormat;
                cellStyle.Alignment.Horizontal = XLAlignmentHorizontalValues.Right;
                break;
            case CellStyle.Period:
                cellStyle.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                break;
            case CellStyle.Highlight:
                cellStyle.NumberFormat.Format = DecimalFormat;
                cellStyle.Alignment.Horizontal = XLAlignmentHorizontalValues.Right;
                cellStyle.Font.Bold = true;
                cellStyle.Font.FontColor = HighlightFontColor;
                cellStyle.Fill.BackgroundColor = HighlightFillColor;
                break;
        }
    }

    private static void WriteHeader(IXLWorksheet sheet, int rowIndex, string text)
    {
        WriteCell(sheet, rowIndex, 0, text, CellStyle.Header);
        // extend header across both columns
        ApplyStyle(sheet.Cell(rowIndex + 1, 2), CellStyle.Header);
    }

    private static void WriteMeta(IXLWorksheet sheet, int rowIndex, string label, string value)
    {
        WriteCell(sheet, rowIndex, 0, label, CellStyle.MetaLabel);
        WriteCell(sheet, rowIndex, 1, value == null ? Blank.Value : value, CellStyle.MetaValue);
    }

    private static void WriteCell(IXLWorksheet sheet, int rowIndex, int col, XLCellValue value, CellStyle style)
    {
        var cell = sheet.Cell(rowIndex + 1, col + 1);
        cell.Value = value;
        ApplyStyle(cell, style);
    }
}
