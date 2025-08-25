using System;
using System.Linq;
using System.Collections.Generic;
using NPOI.SS.UserModel;
using PayrollEngine.Serialization;

namespace PayrollEngine.PayrollConsole.Commands.Excel;

internal static class RowExtensions
{
    internal static bool IsBlank(this IRow row)
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

    internal static T GetCellValue<T>(this IRow row, int? column, T defaultValue = default)
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

    internal static T? GetEnumValue<T>(this IRow row, int? column) where T : struct, Enum
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

    internal static T GetEnumValue<T>(this IRow row, int? column, T defaultValue) where T : struct, Enum
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

    internal static List<T> GetEnumArrayValue<T>(this IRow row, int? column) where T : struct, Enum
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

    internal static List<string> GetCellStringValues(this IRow row, int? column) =>
        column == null ? null : row.GetCell(column.Value).GetCellValue<string>()?.Split(';').ToList();

    internal static Dictionary<string, string> GetLocalizations(this IRow row, Dictionary<int, string> columns)
    {
        var localizations = new Dictionary<string, string>();
        if (row == null || !row.Cells.Any())
        {
            return null;
        }
        foreach (var column in columns)
        {
            var value = GetCellValue<string>(row, column.Key);
            if (!string.IsNullOrWhiteSpace(value))
            {
                localizations.Add(column.Value, value);
            }
        }
        return localizations.Any() ? localizations : null;
    }

    internal static Dictionary<string, object> GetAttributes(this IRow row, int? column)
    {
        var attributes = GetJsonObject<Dictionary<string, object>>(row, column);
        return attributes != null && attributes.Any() ? attributes : null;
    }

    internal static T GetJsonObject<T>(this IRow row, int? column) where T : class
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

    internal static List<T> GetJsonObjectArray<T>(this IRow row, int? column)
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