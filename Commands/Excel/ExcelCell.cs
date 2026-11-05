using System;
using ClosedXML.Excel;

namespace PayrollEngine.PayrollConsole.Commands.Excel;

/// <summary>Read-only Excel cell</summary>
internal sealed class ExcelCell
{
    private readonly IXLCell cell;

    internal ExcelCell(IXLCell cell)
    {
        this.cell = cell ?? throw new ArgumentNullException(nameof(cell));
    }

    /// <summary>Zero-based column index</summary>
    internal int ColumnIndex => cell.Address.ColumnNumber - 1;

    /// <summary>Cell address (e.g. B2)</summary>
    internal string Address => cell.Address.ToString();

    /// <summary>Cell value type, formula cells are <see cref="ExcelCellType.Formula"/></summary>
    internal ExcelCellType CellType
    {
        get
        {
            if (cell.HasFormula)
            {
                return ExcelCellType.Formula;
            }
            return cell.DataType switch
            {
                XLDataType.Boolean => ExcelCellType.Boolean,
                XLDataType.Number or XLDataType.DateTime or XLDataType.TimeSpan => ExcelCellType.Numeric,
                XLDataType.Text => ExcelCellType.String,
                XLDataType.Error => ExcelCellType.Error,
                _ => ExcelCellType.Blank
            };
        }
    }

    /// <summary>True for a numeric cell with a date format</summary>
    internal bool IsDate => !cell.HasFormula && cell.DataType == XLDataType.DateTime;

    /// <summary>Text value, other value types as formatted text</summary>
    internal string StringValue => cell.GetString();

    /// <summary>Numeric value, dates and times as serial number</summary>
    internal double NumericValue => cell.Value.GetUnifiedNumber();

    /// <summary>Date value, numbers are read as serial date</summary>
    internal DateTime DateValue => IsDate ? cell.GetDateTime() : DateTime.FromOADate(NumericValue);

    /// <summary>Boolean value</summary>
    internal bool BooleanValue => cell.GetBoolean();

    /// <summary>Formula without the leading equal sign</summary>
    internal string Formula => cell.FormulaA1;

    /// <summary>Error code (Excel BIFF error code)</summary>
    internal byte ErrorValue =>
        cell.GetError() switch
        {
            XLError.NullValue => 0x00,
            XLError.DivisionByZero => 0x07,
            XLError.IncompatibleValue => 0x0F,
            XLError.CellReference => 0x17,
            XLError.NameNotRecognized => 0x1D,
            XLError.NumberInvalid => 0x24,
            XLError.NoValueAvailable => 0x2A,
            _ => 0xFF
        };
}
