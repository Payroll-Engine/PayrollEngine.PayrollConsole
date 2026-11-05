using System;
using System.Linq;
using ClosedXML.Excel;

namespace PayrollEngine.PayrollConsole.Commands.Excel;

/// <summary>Read-only Excel row</summary>
internal sealed class ExcelRow
{
    private readonly IXLRow row;

    internal ExcelRow(IXLRow row)
    {
        this.row = row ?? throw new ArgumentNullException(nameof(row));
    }

    /// <summary>True if the row has no cell with a value</summary>
    internal bool IsEmpty => !row.CellsUsed().Any();

    /// <summary>Get a cell</summary>
    /// <param name="index">Zero-based column index</param>
    internal ExcelCell GetCell(int index) =>
        new(row.Cell(index + 1));
}
