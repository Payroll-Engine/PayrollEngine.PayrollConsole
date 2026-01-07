using System;
using System.Globalization;
using System.Collections.Generic;
using NPOI.SS.Util;
using NPOI.SS.UserModel;

namespace PayrollEngine.PayrollConsole.Commands.Excel;

internal static class SheetExtensions
{
    /// <param name="worksheet">The worksheet</param>
    extension(ISheet worksheet)
    {
        internal int? GetHeaderColumn(string name)
        {
            foreach (var cell in worksheet.HeaderCells())
            {
                if (cell == null)
                {
                    continue;
                }
                if (cell.StringCellValue.Trim().Equals(name, StringComparison.InvariantCultureIgnoreCase))
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
                var value = cell.StringCellValue.Trim();
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
                var columnName = cell.StringCellValue.Trim();
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

        private int GetMaxPhysicalNumberOfCells()
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

        internal ICell HeaderCell(int column) =>
            worksheet.GetRow(0).GetCell(column);

        internal List<ICell> HeaderCells() =>
            worksheet.RowCells(0);

        private List<ICell> RowCells(int row)
        {
            var cells = new List<ICell>();
            for (var i = 0; i < worksheet.GetMaxPhysicalNumberOfCells(); i++)
            {
                cells.Add(worksheet.GetRow(row).GetCell(i));
            }
            return cells;
        }

        internal T GetCellValue<T>(string address)
        {
            if (string.IsNullOrWhiteSpace(address))
            {
                throw new ArgumentException(nameof(address));
            }
            return worksheet.GetCellValue<T>(CellRangeAddress.ValueOf(address));
        }

        private T GetCellValue<T>(CellRangeAddress address)
        {
            if (address == null)
            {
                throw new ArgumentNullException(nameof(address));
            }
            if (address.NumberOfCells != 1)
            {
                throw new PayrollException($"Excel region {address} must be a single cell.");
            }

            var cell = worksheet.GetRow(address.FirstRow).GetCell(address.FirstColumn);
            return cell.GetCellValue<T>();
        }
    }

    /// <param name="cell">The source cell</param>
    extension(ICell cell)
    {
        private bool IsDateCell() =>
            cell.DateCellValue.HasValue && cell.DateCellValue.Value.Year > 1950;

        internal ValueType GetValueType()
        {
            switch (cell.CellType)
            {
                case CellType.Boolean:
                    return ValueType.Boolean;
                case CellType.Numeric:
                    if (cell.IsDateCell())
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
                case CellType.Boolean:
                    return cell.BooleanCellValue;
                case CellType.Numeric:
                    if (cell.IsDateCell())
                    {
                        return cell.DateCellValue;
                    }
                    return cell.NumericCellValue;
                case CellType.String:
                    return cell.StringCellValue;
                case CellType.Formula:
                    return cell.CellFormula;
                case CellType.Error:
                    return cell.ErrorCellValue;
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
            if (cell == null || cell.CellType == CellType.Blank)
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

            // int
            if (typeof(T) == typeof(int))
            {
                TestCellType(cell, CellType.Numeric);
                return (T)Convert.ChangeType((int)cell.NumericCellValue, typeof(T));
            }
            if (typeof(T) == typeof(int?))
            {
                TestCellType(cell, CellType.Numeric);
                var value = int.Parse(cell.NumericCellValue.ToString(CultureInfo.InvariantCulture));
                return (T)(object)value;
            }

            // decimal
            if (typeof(T) == typeof(decimal))
            {
                TestCellType(cell, CellType.Numeric);
                return (T)Convert.ChangeType((decimal)cell.NumericCellValue, typeof(T));
            }
            if (typeof(T) == typeof(decimal?))
            {
                TestCellType(cell, CellType.Numeric);
                var value = decimal.Parse(cell.NumericCellValue.ToString(CultureInfo.InvariantCulture), provider: CultureInfo.InvariantCulture);
                return (T)(object)value;
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
    }

    private static void TestCellType(ICell cell, CellType cellType)
    {
        if (cell.CellType != cellType)
        {
            throw new PayrollException($"Invalid cell value type {cell.CellType} on cell {cell.Address}, expected {cellType}.");
        }
    }
}