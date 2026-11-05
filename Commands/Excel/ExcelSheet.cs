using System;
using ClosedXML.Excel;

namespace PayrollEngine.PayrollConsole.Commands.Excel;

/// <summary>Read-only Excel worksheet</summary>
internal sealed class ExcelSheet
{
    private readonly IXLWorksheet worksheet;

    internal ExcelSheet(IXLWorksheet worksheet)
    {
        this.worksheet = worksheet ?? throw new ArgumentNullException(nameof(worksheet));
        RowCount = worksheet.LastRowUsed()?.RowNumber() ?? 0;
        ColumnCount = worksheet.LastColumnUsed()?.ColumnNumber() ?? 0;
    }

    /// <summary>Worksheet name</summary>
    internal string SheetName => worksheet.Name;

    /// <summary>Number of rows up to the last row with a value, including the header row</summary>
    internal int RowCount { get; }

    /// <summary>Number of columns up to the last column with a value</summary>
    internal int ColumnCount { get; }

    /// <summary>Get a row</summary>
    /// <param name="index">Zero-based row index</param>
    internal ExcelRow GetRow(int index) =>
        new(worksheet.Row(index + 1));
}
