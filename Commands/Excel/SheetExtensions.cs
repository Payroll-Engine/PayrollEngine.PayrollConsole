using System;
using System.Collections.Generic;
using NPOI.SS.UserModel;
using NPOI.SS.Util;

namespace PayrollEngine.PayrollConsole.Commands.Excel;

internal static class SheetExtensions
{
    private static int GetMaxPhysicalNumberOfCells(this ISheet worksheet)
    {
        var maxColumnCount = 0;
        for (var i = 0; i < worksheet.PhysicalNumberOfRows; i++)
        {
            var row = worksheet.GetRow(i);
            if (row.PhysicalNumberOfCells > maxColumnCount)
            {
                maxColumnCount = row.PhysicalNumberOfCells;
            }
        }
        return maxColumnCount;
    }

    internal static ICell HeaderCell(this ISheet worksheet, int column) =>
        worksheet.GetRow(0).GetCell(column);

    internal static List<ICell> HeaderCells(this ISheet worksheet) =>
        worksheet.RowCells(0);

    private static List<ICell> RowCells(this ISheet worksheet, int row)
    {
        var cells = new List<ICell>();
        for (var i = 0; i < worksheet.GetMaxPhysicalNumberOfCells(); i++)
        {
            cells.Add(worksheet.GetRow(row).GetCell(i));
        }
        return cells;
    }

    internal static T GetCellValue<T>(this ISheet sheet, string address)
    {
        if (string.IsNullOrWhiteSpace(address))
        {
            throw new ArgumentException(nameof(address));
        }
        return sheet.GetCellValue<T>(CellRangeAddress.ValueOf(address));
    }

    private static T GetCellValue<T>(this ISheet sheet, CellRangeAddress address)
    {
        if (address == null)
        {
            throw new ArgumentNullException(nameof(address));
        }
        if (address.NumberOfCells != 1)
        {
            throw new PayrollException($"Excel region {address} must be a single cell.");
        }

        var cell = sheet.GetRow(address.FirstRow).GetCell(address.FirstColumn);
        return cell.GetCellValue<T>();
    }

    internal static object GetCellValue(this ICell cell)
    {
        return cell.CellType switch
        {
            CellType.Boolean => cell.BooleanCellValue,
            CellType.Numeric => cell.NumericCellValue,
            CellType.String => cell.StringCellValue,
            CellType.Formula => cell.CellFormula,
            CellType.Error => cell.ErrorCellValue,
            _ => null
        };
    }

    /// <summary>
    /// Get the cell value
    /// </summary>
    /// <param name="cell">The source cell</param>
    /// <param name="provider">The format provider</param>
    /// <returns>The cell value</returns>
    internal static T GetCellValue<T>(this ICell cell, IFormatProvider provider = null)
    {
        if (cell == null)
        {
            return default;
        }

        // string
        if (typeof(T) == typeof(string))
        {
            TestCellType(cell, CellType.String);
            return (T)Convert.ChangeType(cell.StringCellValue, typeof(T));
        }

        // boolean
        if (typeof(T) == typeof(bool))
        {
            TestCellType(cell, CellType.Boolean);
            return (T)Convert.ChangeType(cell.BooleanCellValue, typeof(T));
        }
        if (typeof(T) == typeof(bool?))
        {
            TestCellType(cell, CellType.Boolean);
            return !string.IsNullOrWhiteSpace(cell.StringCellValue) ?
                (T)Convert.ChangeType(cell.BooleanCellValue, typeof(T)) :
                default;
        }

        // date time
        if (typeof(T) == typeof(DateTime))
        {
            if (cell.CellType == CellType.Numeric)
            {
                return (T)(object)cell.DateCellValue;
            }
            TestCellType(cell, CellType.String);
            var date = DateTime.Parse(cell.StringCellValue, provider);
            return (T)(object)date;
        }
        if (typeof(T) == typeof(DateTime?))
        {
            if (cell.CellType == CellType.Numeric)
            {
                return (T)(object)cell.DateCellValue;
            }
            if (cell.CellType == CellType.Blank)
            {
                return default;
            }
            TestCellType(cell, CellType.String);
            if (string.IsNullOrWhiteSpace(cell.StringCellValue))
            {
                return default;
            }
            var date = DateTime.Parse(cell.StringCellValue, provider);
            return (T)(object)date;
        }

        // double
        if (typeof(T) == typeof(double))
        {
            TestCellType(cell, CellType.Numeric);
            return (T)Convert.ChangeType(cell.NumericCellValue, typeof(T));
        }
        if (typeof(T) == typeof(double?))
        {
            TestCellType(cell, CellType.Numeric);
            return !string.IsNullOrWhiteSpace(cell.StringCellValue) ?
                (T)Convert.ChangeType(cell.NumericCellValue, typeof(T)) :
                default;
        }

        return default;
    }

    private static void TestCellType(ICell cell, CellType cellType)
    {
        if (cell.CellType != cellType)
        {
            throw new PayrollException($"Invalid cell value type {cell.CellType} on cell {cell.Address}, expected {cellType}.");
        }
    }
}