using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using NPOI.SS.UserModel;
using NPOI.SS.Util;
using NPOI.XSSF.UserModel;

namespace PayrollEngine.PayrollConsole.Commands.LoadTestCommands;

/// <summary>Writes payrun load test results to an Excel workbook</summary>
internal sealed class PayrunLoadTestExcelWriter
{
    private readonly string path;
    private readonly PayrunLoadTestParameters parameters;
    private readonly List<PayrunLoadTestResult> results;

    // styles
    private ICellStyle headerStyle;
    private ICellStyle metaLabelStyle;
    private ICellStyle metaValueStyle;
    private ICellStyle dataStyle;
    private ICellStyle dataNumberStyle;
    private ICellStyle dataDecimalStyle;
    private ICellStyle dataPeriodStyle;
    private ICellStyle dataTimestampStyle;
    private ICellStyle highlightStyle;

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
        var workbook = new XSSFWorkbook();
        CreateStyles(workbook);

        WriteSetupSheet(workbook);
        WriteResultsSheet(workbook);
        WriteSummarySheet(workbook);

        var directory = Path.GetDirectoryName(Path.GetFullPath(path));
        if (!string.IsNullOrEmpty(directory))
        {
            Directory.CreateDirectory(directory);
        }

        using var stream = new FileStream(path, FileMode.Create, FileAccess.Write);
        workbook.Write(stream);
    }

    // setup sheet: machine, timing config, test parameters
    private void WriteSetupSheet(IWorkbook workbook)
    {
        var sheet = workbook.CreateSheet("Setup");
        sheet.SetColumnWidth(0, 32 * 256);
        sheet.SetColumnWidth(1, 40 * 256);

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
    private void WriteResultsSheet(IWorkbook workbook)
    {
        var sheet = workbook.CreateSheet("Results");
        sheet.SetColumnWidth(0, 22 * 256);  // Timestamp
        sheet.SetColumnWidth(1, 8 * 256);   // Run
        sheet.SetColumnWidth(2, 12 * 256);  // Period
        sheet.SetColumnWidth(3, 16 * 256);  // EmployeeCount
        sheet.SetColumnWidth(4, 20 * 256);  // ClientDuration_ms
        sheet.SetColumnWidth(5, 24 * 256);  // ServerJobDuration_ms
        sheet.SetColumnWidth(6, 24 * 256);  // ServerAvgMs_PerEmployee

        // header row
        var header = sheet.CreateRow(0);
        WriteCell(header, 0, "Timestamp", headerStyle);
        WriteCell(header, 1, "Run", headerStyle);
        WriteCell(header, 2, "Period", headerStyle);
        WriteCell(header, 3, "EmployeeCount", headerStyle);
        WriteCell(header, 4, "ClientDuration_ms", headerStyle);
        WriteCell(header, 5, "ServerJobDuration_ms", headerStyle);
        WriteCell(header, 6, "ServerAvgMs_PerEmployee", headerStyle);

        // data rows
        var rowIndex = 1;
        foreach (var r in results)
        {
            var row = sheet.CreateRow(rowIndex++);
            WriteCell(row, 0, r.Timestamp.ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture), dataTimestampStyle);
            WriteCell(row, 1, r.RunNumber, dataNumberStyle);
            WriteCell(row, 2, r.Period.ToString("yyyy-MM", CultureInfo.InvariantCulture), dataPeriodStyle);
            WriteCell(row, 3, r.EmployeeCount, dataNumberStyle);
            WriteCell(row, 4, r.ClientDurationMs, dataNumberStyle);
            WriteCell(row, 5, r.ServerJobDurationMs, dataNumberStyle);
            WriteCell(row, 6, r.ServerAvgMsPerEmployee, dataDecimalStyle);
        }

        // auto-filter on header row
        sheet.SetAutoFilter(new CellRangeAddress(0, rowIndex - 1, 0, 6));
        sheet.CreateFreezePane(0, 1);
    }

    // summary sheet: pivot — period × run, ServerAvgMs/Employee
    private void WriteSummarySheet(IWorkbook workbook)
    {
        var sheet = workbook.CreateSheet("Avg ms per Employee");

        var runs = results.Select(r => r.RunNumber).Distinct().OrderBy(x => x).ToList();
        var periods = results.Select(r => r.Period).Distinct().OrderBy(x => x).ToList();

        // column widths
        sheet.SetColumnWidth(0, 16 * 256);
        for (var i = 0; i < runs.Count; i++)
        {
            sheet.SetColumnWidth(i + 1, 14 * 256);
        }

        // header row: Period | Run 1 | Run 2 | ...
        var header = sheet.CreateRow(0);
        WriteCell(header, 0, "Period", headerStyle);
        for (var i = 0; i < runs.Count; i++)
        {
            WriteCell(header, i + 1, $"Run {runs[i]}", headerStyle);
        }

        // data rows
        var lookup = results.ToDictionary(
            r => (r.Period, r.RunNumber),
            r => r.ServerAvgMsPerEmployee);

        var rowIndex = 1;
        foreach (var period in periods)
        {
            var row = sheet.CreateRow(rowIndex++);
            WriteCell(row, 0, period.ToString("yyyy-MM", CultureInfo.InvariantCulture), dataPeriodStyle);
            for (var i = 0; i < runs.Count; i++)
            {
                var value = lookup.GetValueOrDefault((period, runs[i]), 0);
                // highlight outliers: >2× median avg
                var allValues = lookup.Values.Where(x => x > 0).OrderBy(x => x).ToList();
                var medianVal = allValues.Count > 0 ? allValues[allValues.Count / 2] : 0;
                var style = value > medianVal * 2 ? highlightStyle : dataDecimalStyle;
                WriteCell(row, i + 1, value, style);
            }
        }

        sheet.CreateFreezePane(0, 1);
    }

    // style helpers
    private void CreateStyles(IWorkbook workbook)
    {
        var boldFont = workbook.CreateFont();
        boldFont.IsBold = true;

        var headerFont = workbook.CreateFont();
        headerFont.IsBold = true;
        headerFont.Color = IndexedColors.White.Index;

        var highlightFont = workbook.CreateFont();
        highlightFont.IsBold = true;
        highlightFont.Color = IndexedColors.DarkRed.Index;

        headerStyle = workbook.CreateCellStyle();
        headerStyle.SetFont(headerFont);
        headerStyle.FillForegroundColor = IndexedColors.DarkBlue.Index;
        headerStyle.FillPattern = FillPattern.SolidForeground;
        headerStyle.Alignment = HorizontalAlignment.Left;

        metaLabelStyle = workbook.CreateCellStyle();
        metaLabelStyle.SetFont(boldFont);

        metaValueStyle = workbook.CreateCellStyle();

        dataStyle = workbook.CreateCellStyle();
        dataStyle.Alignment = HorizontalAlignment.Left;

        dataNumberStyle = workbook.CreateCellStyle();
        dataNumberStyle.Alignment = HorizontalAlignment.Right;

        var decimalFormat = workbook.CreateDataFormat();
        dataDecimalStyle = workbook.CreateCellStyle();
        dataDecimalStyle.DataFormat = decimalFormat.GetFormat("0.0");
        dataDecimalStyle.Alignment = HorizontalAlignment.Right;

        dataPeriodStyle = workbook.CreateCellStyle();
        dataPeriodStyle.Alignment = HorizontalAlignment.Center;

        dataTimestampStyle = workbook.CreateCellStyle();

        highlightStyle = workbook.CreateCellStyle();
        highlightStyle.DataFormat = decimalFormat.GetFormat("0.0");
        highlightStyle.Alignment = HorizontalAlignment.Right;
        highlightStyle.SetFont(highlightFont);
        highlightStyle.FillForegroundColor = IndexedColors.LightYellow.Index;
        highlightStyle.FillPattern = FillPattern.SolidForeground;
    }

    private void WriteHeader(ISheet sheet, int rowIndex, string text)
    {
        var row = sheet.CreateRow(rowIndex);
        WriteCell(row, 0, text, headerStyle);
        // extend header across both columns
        var cell1 = row.CreateCell(1);
        cell1.CellStyle = headerStyle;
    }

    private void WriteMeta(ISheet sheet, int rowIndex, string label, string value)
    {
        var row = sheet.CreateRow(rowIndex);
        WriteCell(row, 0, label, metaLabelStyle);
        WriteCell(row, 1, value, metaValueStyle);
    }

    private static void WriteCell(IRow row, int col, string value, ICellStyle style)
    {
        var cell = row.CreateCell(col);
        cell.SetCellValue(value);
        cell.CellStyle = style;
    }

    private static void WriteCell(IRow row, int col, long value, ICellStyle style)
    {
        var cell = row.CreateCell(col);
        cell.SetCellValue(value);
        cell.CellStyle = style;
    }

    private static void WriteCell(IRow row, int col, double value, ICellStyle style)
    {
        var cell = row.CreateCell(col);
        cell.SetCellValue(value);
        cell.CellStyle = style;
    }
}
