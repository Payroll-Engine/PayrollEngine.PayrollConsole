using ClosedXML.Excel;

namespace PayrollEngine.PayrollConsole.Commands.Tests;

/// <summary>Builds small xlsx test workbooks in memory</summary>
internal static class ExcelTestWorkbook
{
    /// <summary>Add a worksheet with a header row and data rows (null values are left empty)</summary>
    internal static IXLWorksheet AddSheet(XLWorkbook workbook, string name, string[] headers, params object[][] rows)
    {
        var sheet = workbook.Worksheets.Add(name);
        for (var x = 0; x < headers.Length; x++)
        {
            sheet.Cell(1, x + 1).Value = headers[x];
        }
        for (var y = 0; y < rows.Length; y++)
        {
            var row = rows[y];
            if (row == null)
            {
                continue;
            }
            for (var x = 0; x < row.Length; x++)
            {
                SetValue(sheet.Cell(y + 2, x + 1), row[x]);
            }
        }
        return sheet;
    }

    /// <summary>Set a cell value, dates get a date format</summary>
    internal static void SetValue(IXLCell cell, object value)
    {
        switch (value)
        {
            case null:
                return;
            case DateTime date:
                cell.Value = date;
                cell.Style.NumberFormat.Format = "yyyy-mm-dd";
                return;
            default:
                cell.Value = XLCellValue.FromObject(value);
                return;
        }
    }

    /// <summary>Save the workbook to a temporary file</summary>
    internal static string SaveTemp(XLWorkbook workbook)
    {
        var fileName = Path.Combine(Path.GetTempPath(), $"pe-excel-test-{Guid.NewGuid():N}.xlsx");
        workbook.SaveAs(fileName);
        return fileName;
    }

    /// <summary>Save the workbook to a memory stream</summary>
    internal static MemoryStream SaveStream(XLWorkbook workbook)
    {
        var stream = new MemoryStream();
        workbook.SaveAs(stream);
        stream.Position = 0;
        return stream;
    }
}
