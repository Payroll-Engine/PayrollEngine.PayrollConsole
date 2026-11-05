using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using ClosedXML.Excel;

namespace PayrollEngine.PayrollConsole.Commands.Excel;

/// <summary>Read-only Excel workbook (xlsx)</summary>
internal sealed class ExcelWorkbook : IDisposable
{
    private readonly XLWorkbook workbook;

    private ExcelWorkbook(XLWorkbook workbook)
    {
        this.workbook = workbook;
        Sheets = workbook.Worksheets.Select(x => new ExcelSheet(x)).ToList();
    }

    /// <summary>Open a workbook from a stream</summary>
    /// <param name="stream">The xlsx stream</param>
    internal static ExcelWorkbook Open(Stream stream)
    {
        ArgumentNullException.ThrowIfNull(stream);
        return new(new XLWorkbook(stream));
    }

    /// <summary>Worksheets in workbook order</summary>
    internal IReadOnlyList<ExcelSheet> Sheets { get; }

    /// <summary>Get a worksheet by name</summary>
    /// <param name="sheetName">The worksheet name</param>
    /// <returns>The worksheet, null if not present</returns>
    internal ExcelSheet GetSheet(string sheetName) =>
        Sheets.FirstOrDefault(x => string.Equals(x.SheetName, sheetName));

    /// <summary>Get the cell of a defined name (workbook scope first, then worksheet scope)</summary>
    /// <param name="name">The defined name</param>
    /// <returns>The cell, null if the name is missing or has no valid reference</returns>
    /// <exception cref="PayrollException">The name refers to more than one cell</exception>
    internal ExcelCell GetNamedCell(string name)
    {
        if (!workbook.DefinedNames.TryGetValue(name, out var definedName))
        {
            definedName = workbook.Worksheets
                .Select(x => x.DefinedNames.TryGetValue(name, out var sheetName) ? sheetName : null)
                .FirstOrDefault(x => x != null);
        }
        var range = definedName?.Ranges.FirstOrDefault();
        if (range == null)
        {
            return null;
        }
        if (range.RowCount() != 1 || range.ColumnCount() != 1)
        {
            throw new PayrollException($"Excel region {range.RangeAddress} must be a single cell.");
        }
        return new(range.FirstCell());
    }

    /// <inheritdoc />
    public void Dispose() =>
        workbook.Dispose();
}
