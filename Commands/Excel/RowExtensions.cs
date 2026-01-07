using System;
using System.Linq;
using System.Collections.Generic;
using NPOI.SS.UserModel;
using PayrollEngine.Serialization;

namespace PayrollEngine.PayrollConsole.Commands.Excel;

internal static class RowExtensions
{
    extension(IRow row)
    {
        internal bool IsBlank()
        {
            if (row == null || !row.Cells.Any())
            {
                return true;
            }

            foreach (var cell in row.Cells)
            {
                if (cell.CellType is CellType.Blank or CellType.Unknown)
                {
                    continue;
                }
                return false;
            }
            return true;
        }

        internal T GetCellValue<T>(int? column, T defaultValue = default)
        {
            if (column == null)
            {
                return defaultValue;
            }

            // bool
            if (typeof(T) == typeof(bool) || typeof(T) == typeof(bool?))
            {
                var boolValue = row.GetCell(column.Value).GetCellValue();
                if (boolValue == null)
                {
                    return defaultValue;
                }
                //var value = bool.TryParse(boolText, out var result);
                return (T)Convert.ChangeType(boolValue, typeof(T));
            }
            return row.GetCell(column.Value).GetCellValue<T>();
        }

        internal T? GetEnumValue<T>(int? column) where T : struct, Enum
        {
            if (column == null)
            {
                return null;
            }

            // enum
            var enumValue = row.GetCell(column.Value).GetCellValue<string>()?.Trim();
            if (string.IsNullOrWhiteSpace(enumValue) || !Enum.TryParse<T>(enumValue, ignoreCase: true, out var value))
            {
                return null;
            }
            return value;
        }

        internal T GetEnumValue<T>(int? column, T defaultValue) where T : struct, Enum
        {
            if (column == null)
            {
                return defaultValue;
            }

            // enum
            var enumValue = row.GetCell(column.Value).GetCellValue<string>()?.Trim();
            if (string.IsNullOrWhiteSpace(enumValue) || !Enum.TryParse<T>(enumValue, ignoreCase: true, out var value))
            {
                return defaultValue;
            }
            return value;
        }

        internal List<T> GetEnumArrayValue<T>(int? column) where T : struct, Enum
        {
            if (column == null)
            {
                return null;
            }

            // enum
            var enumValue = row.GetCell(column.Value).GetCellValue<string>()?.Trim();
            if (string.IsNullOrWhiteSpace(enumValue))
            {
                return null;
            }

            var tokens = enumValue.Split(';', StringSplitOptions.RemoveEmptyEntries).ToList();
            var values = new List<T>();
            foreach (var enumToken in tokens)
            {
                if (Enum.TryParse<T>(enumToken, ignoreCase: true, out var value))
                {
                    values.Add(value);
                }
            }
            return values;
        }

        internal List<string> GetCellStringValues(int? column) =>
            column == null ? null : row.GetCell(column.Value).GetCellValue<string>()?.Split(';').ToList();

        internal Dictionary<string, string> GetLocalizations(Dictionary<int, string> columns)
        {
            var localizations = new Dictionary<string, string>();
            if (row == null || !row.Cells.Any())
            {
                return null;
            }
            foreach (var column in columns)
            {
                var value = row.GetCellValue<string>(column.Key);
                if (!string.IsNullOrWhiteSpace(value))
                {
                    localizations.Add(column.Value, value);
                }
            }
            return localizations.Any() ? localizations : null;
        }

        internal Dictionary<string, object> GetAttributes(int? column)
        {
            var attributes = row.GetJsonObject<Dictionary<string, object>>(column);
            return attributes != null && attributes.Any() ? attributes : null;
        }

        internal T GetJsonObject<T>(int? column) where T : class
        {
            if (column == null)
            {
                return null;
            }

            var json = row.GetCell(column.Value).GetCellValue<string>();
            if (string.IsNullOrWhiteSpace(json))
            {
                return null;
            }

            json = json.Trim().EnsureStart("{").EnsureEnd("}");

            return DefaultJsonSerializer.Deserialize<T>(json);
        }

        internal List<T> GetJsonObjectArray<T>(int? column)
        {
            if (column == null)
            {
                return null;
            }

            var json = row.GetCell(column.Value).GetCellValue<string>();
            if (string.IsNullOrWhiteSpace(json))
            {
                return null;
            }

            json = json.Trim().EnsureStart("[").EnsureEnd("]");

            return DefaultJsonSerializer.Deserialize<List<T>>(json);
        }
    }
}