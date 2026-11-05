using System;
using System.Globalization;
using System.Collections.Generic;

namespace PayrollEngine.PayrollConsole.Commands.Excel;

internal static class SheetExtensions
{
    /// <param name="worksheet">The worksheet</param>
    extension(ExcelSheet worksheet)
    {
        internal int? GetHeaderColumn(string name)
        {
            foreach (var cell in worksheet.HeaderCells())
            {
                if (cell == null)
                {
                    continue;
                }
                if (cell.StringValue.Trim().Equals(name, StringComparison.InvariantCultureIgnoreCase))
                {
                    return cell.ColumnIndex;
                }
            }
            return null;
        }

        internal Dictionary<int, string> GetHeaderMultiColumns(string name, bool dotSeparator = true)
        {
            var columns = new Dictionary<int, string>();
            if (dotSeparator)
            {
                name = name.EnsureEnd(".");
            }
            foreach (var cell in worksheet.HeaderCells())
            {
                if (cell == null)
                {
                    continue;
                }
                var value = cell.StringValue.Trim();
                if (value.StartsWith(name, StringComparison.InvariantCultureIgnoreCase))
                {
                    columns.Add(cell.ColumnIndex, value.RemoveFromStart(name));
                }
            }
            return columns;
        }

        /// <summary>
        /// Gets sheet column indexed by column name
        /// </summary>
        /// <param name="titles">The column titles</param>
        internal IDictionary<string, int> GetColumnIndexes(ICollection<string> titles)
        {
            var columns = new Dictionary<string, int>();
            foreach (var cell in worksheet.HeaderCells())
            {
                if (cell == null)
                {
                    continue;
                }
                var columnName = cell.StringValue.Trim();
                if (titles.Contains(columnName))
                {
                    columns.Add(columnName, cell.ColumnIndex);
                }
            }

            // verify that all columns are present
            foreach (var title in titles)
            {
                if (!columns.ContainsKey(title))
                {
                    throw new PayrollException($"Missing Excel column {title}.");
                }
            }

            return columns;
        }

        internal ExcelCell HeaderCell(int column) =>
            worksheet.GetRow(0).GetCell(column);

        /// <summary>Header cells up to the last used column, null for empty cells</summary>
        internal List<ExcelCell> HeaderCells()
        {
            var cells = new List<ExcelCell>();
            var header = worksheet.GetRow(0);
            for (var i = 0; i < worksheet.ColumnCount; i++)
            {
                var cell = header.GetCell(i);
                cells.Add(cell.CellType == ExcelCellType.Blank ? null : cell);
            }
            return cells;
        }
    }

    /// <param name="cell">The source cell</param>
    extension(ExcelCell cell)
    {
        internal ValueType GetValueType()
        {
            switch (cell.CellType)
            {
                case ExcelCellType.Boolean:
                    return ValueType.Boolean;
                case ExcelCellType.Numeric:
                    if (cell.IsDate)
                    {
                        return ValueType.DateTime;
                    }
                    return ValueType.Decimal;
                default:
                    return ValueType.String;
            }
        }

        internal object GetCellValue()
        {
            switch (cell.CellType)
            {
                case ExcelCellType.Boolean:
                    return cell.BooleanValue;
                case ExcelCellType.Numeric:
                    if (cell.IsDate)
                    {
                        return cell.DateValue;
                    }
                    return cell.NumericValue;
                case ExcelCellType.String:
                    return cell.StringValue;
                case ExcelCellType.Formula:
                    return cell.Formula;
                case ExcelCellType.Error:
                    return cell.ErrorValue;
                default:
                    return null;
            }
        }

        /// <summary>
        /// Get the cell value
        /// </summary>
        /// <param name="provider">The format provider</param>
        /// <returns>The cell value</returns>
        internal T GetCellValue<T>(IFormatProvider provider = null)
        {
            if (cell == null || cell.CellType == ExcelCellType.Blank)
            {
                return default;
            }

            // string
            if (typeof(T) == typeof(string))
            {
                TestCellType(cell, ExcelCellType.String);
                return (T)Convert.ChangeType(cell.StringValue, typeof(T));
            }

            // boolean
            if (typeof(T) == typeof(bool) || typeof(T) == typeof(bool?))
            {
                TestCellType(cell, ExcelCellType.Boolean);
                return (T)(object)cell.BooleanValue;
            }

            // date time
            if (typeof(T) == typeof(DateTime) || typeof(T) == typeof(DateTime?))
            {
                if (cell.CellType == ExcelCellType.Numeric)
                {
                    return (T)(object)cell.DateValue;
                }
                TestCellType(cell, ExcelCellType.String);
                if (typeof(T) == typeof(DateTime?) && string.IsNullOrWhiteSpace(cell.StringValue))
                {
                    return default;
                }
                var date = DateTime.Parse(cell.StringValue, provider);
                return (T)(object)date;
            }

            // int
            if (typeof(T) == typeof(int))
            {
                TestCellType(cell, ExcelCellType.Numeric);
                return (T)Convert.ChangeType((int)cell.NumericValue, typeof(T));
            }
            if (typeof(T) == typeof(int?))
            {
                TestCellType(cell, ExcelCellType.Numeric);
                var value = int.Parse(cell.NumericValue.ToString(CultureInfo.InvariantCulture));
                return (T)(object)value;
            }

            // decimal
            if (typeof(T) == typeof(decimal))
            {
                TestCellType(cell, ExcelCellType.Numeric);
                return (T)Convert.ChangeType((decimal)cell.NumericValue, typeof(T));
            }
            if (typeof(T) == typeof(decimal?))
            {
                TestCellType(cell, ExcelCellType.Numeric);
                var value = decimal.Parse(cell.NumericValue.ToString(CultureInfo.InvariantCulture), provider: CultureInfo.InvariantCulture);
                return (T)(object)value;
            }

            // double
            if (typeof(T) == typeof(double) || typeof(T) == typeof(double?))
            {
                TestCellType(cell, ExcelCellType.Numeric);
                return (T)(object)cell.NumericValue;
            }

            return default;
        }
    }

    private static void TestCellType(ExcelCell cell, ExcelCellType cellType)
    {
        if (cell.CellType != cellType)
        {
            throw new PayrollException($"Invalid cell value type {cell.CellType} on cell {cell.Address}, expected {cellType}.");
        }
    }
}
