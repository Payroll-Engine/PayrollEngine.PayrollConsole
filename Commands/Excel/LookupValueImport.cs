using System;
using System.Linq;
using System.Collections.Generic;
using NPOI.SS.UserModel;
using PayrollEngine.Client.Model;

namespace PayrollEngine.PayrollConsole.Commands.Excel;

internal static class LookupValueImport
{
    internal static void Import(IWorkbook workbook, List<LookupSet> lookups)
    {
        ImportDefaultSheet(workbook, lookups);
        ImportLookupSheets(workbook, lookups);
    }

    private static void ImportLookupSheets(IWorkbook workbook, List<LookupSet> lookups)
    {
        // process all Lookup.* sheets
        var sheets = workbook.GetSheetsOf(SheetSpecification.LookupMask);
        foreach (var sheet in sheets)
        {
            var lookupValues = GetSheetLookupValues(sheet);
            if (lookupValues == null)
            {
                continue;
            }

            // lookup
            var lookupName = workbook.EnsureNamespace(lookupValues.Item1);
            var lookup = lookups.FirstOrDefault(x => string.Equals(x.Name, lookupName));
            if (lookup == null)
            {
                throw new PayrollException($"Unknown lookup {lookupValues.Item1}");
            }

            lookup.Values ??= [];
            lookup.Values.AddRange(lookupValues.Item3);
            if (lookupValues.Item2.HasValue)
            {
                lookup.Created = lookupValues.Item2.Value;
            }
        }
    }

    private static Tuple<string, DateTime?, List<LookupValue>> GetSheetLookupValues(ISheet worksheet)
    {
        // lookup name
        var name = worksheet.SheetName.RemoveFromStart(SheetSpecification.LookupMask, StringComparison.InvariantCultureIgnoreCase);
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new PayrollException($"Invalid lookup name {worksheet.SheetName}.");
        }

        // columns
        var columns = new LookupValueSheetColumns(worksheet);
        if (!columns.KeyColumns.Any())
        {
            throw new PayrollException("Missing lookup key column(s).");
        }
        if (!columns.ValueColumns.Any())
        {
            throw new PayrollException("Missing lookup value column(s).");
        }

        // collect lookup values (skip header row)
        DateTime? lookupCreatedDate = null;
        var lookupValues = new List<LookupValue>();
        for (var i = 1; i < worksheet.PhysicalNumberOfRows; i++)
        {
            var row = worksheet.GetRow(i);
            if (row.IsBlank())
            {
                continue;
            }

            // range value
            var rangeValue = columns.RangeColumn.HasValue ?
                GetCellRangeValue(row, columns.RangeColumn.Value) :
                null;

            // key
            var keyValues = GetKeyValues(columns.KeyColumns, row);
            // no keys
            if (!keyValues.Any())
            {
                continue;
            }

            // values
            var values = GetCellValues(worksheet, columns.ValueColumns, row);
            if (!values.Any())
            {
                // no values
                continue;
            }

            // lookup value
            var lookupValue = new LookupValue
            {
                // first key value or key value array
                Key = keyValues.Count == 1 ?
                    $"{keyValues.First()}" :
                    ClientJsonSerializer.Serialize(keyValues.ToArray()),
                // first value without name or name/value dictionary
                Value = values.Count == 1 ?
                    $"{values.First().Value}" :
                    ClientJsonSerializer.Serialize(values),
                RangeValue = rangeValue
            };

            // created date
            if (columns.CreatedColumn.HasValue)
            {
                lookupCreatedDate = GetLookupCreatedDate(row, columns.CreatedColumn.Value,
                    lookupValue, lookupCreatedDate);
            }

            lookupValues.Add(lookupValue);
        }

        // empty
        if (!lookupValues.Any())
        {
            return null;
        }

        // result
        var lookup = new LookupSet
        {
            Name = name,
            Values = lookupValues
        };
        // created
        if (lookupCreatedDate.HasValue)
        {
            lookup.Created = lookupCreatedDate.Value;
        }
        return new(name, lookupCreatedDate, lookupValues);
    }

    private static DateTime? GetLookupCreatedDate(IRow row, int createdColumn, LookupValue lookupValue,
        DateTime? lookupCreatedDate)
    {
        var created = row.GetCell(createdColumn).GetCellValue<DateTime?>();
        if (created.HasValue)
        {
            lookupValue.Created = created.Value;
            if (!lookupCreatedDate.HasValue || created < lookupCreatedDate.Value)
            {
                lookupCreatedDate = created.Value;
            }
        }

        return lookupCreatedDate;
    }

    private static Dictionary<string, object> GetCellValues(ISheet worksheet, Dictionary<int, string> columns, IRow row)
    {
        var values = new Dictionary<string, object>();
        foreach (var column in columns)
        {
            var value = GetCellValue(worksheet, row, column.Key);
            values.Add(value.Item1, value.Item2);
        }
        return values;
    }

    private static Tuple<string, object> GetCellValue(ISheet worksheet, IRow row, int valueColumn)
    {
        var value = row.GetCell(valueColumn).GetCellValue();
        var header = worksheet.HeaderCell(valueColumn).StringCellValue.Trim();
        string valueName;
        if (string.Equals(LookupSheetSpecification.Value, header, StringComparison.InvariantCultureIgnoreCase))
        {
            valueName = header;
        }
        else if (header.StartsWith(LookupSheetSpecification.ValueMask,
                     StringComparison.InvariantCultureIgnoreCase))
        {
            valueName = header.RemoveFromStart(LookupSheetSpecification.ValueMask,
                StringComparison.InvariantCultureIgnoreCase);
        }
        else
        {
            throw new PayrollException($"Invalid lookup value name: {header}.");
        }

        return new(valueName, value);
    }

    private static decimal? GetCellRangeValue(IRow row, int rangeColumn)
    {
        decimal? rangeValue = null;
        var cellValue = row.GetCell(rangeColumn).GetCellValue();
        if (cellValue != null)
        {
            if (cellValue is double)
            {
                // ReSharper disable once PossibleInvalidCastException
                rangeValue = Convert.ToDecimal(cellValue);
            }
            else
            {
                throw new PayrollException($"Invalid lookup range value {cellValue}.");
            }
        }
        return rangeValue;
    }

    private static List<object> GetKeyValues(Dictionary<int, string> keyColumns, IRow row)
    {
        var keyObjects = new List<object>();
        foreach (var keyColumn in keyColumns)
        {
            var keyObject = row.GetCell(keyColumn.Key).GetCellValue();
            keyObjects.Add(keyObject);
        }
        return keyObjects;
    }

    private static void ImportDefaultSheet(IWorkbook workbook, List<LookupSet> lookups)
    {
        // case worksheet
        if (!workbook.HasSheet(SheetSpecification.LookupValue))
        {
            return;
        }

        var worksheet = workbook.GetSheet(SheetSpecification.LookupValue);

        // no data rows
        if (worksheet.PhysicalNumberOfRows < 2)
        {
            return;
        }

        // columns
        var columns = new LookupValueSheetColumns(worksheet);
        if (columns.LookupColumn == null)
        {
            throw new PayrollException($"Missing lookup column {SheetSpecification.LookupRefName}.");
        }
        if (columns.KeyColumn == null)
        {
            throw new PayrollException($"Missing lookup value key column {nameof(Case.Name)}.");
        }

        // lookup values
        var lookupValues = new List<LookupValue>();
        for (var i = 1; i < worksheet.PhysicalNumberOfRows; i++)
        {
            var row = worksheet.GetRow(i);
            if (row.IsBlank())
            {
                continue;
            }

            // lookup
            var lookupName = row.GetCellValue<string>(columns.LookupColumn);
            if (string.IsNullOrWhiteSpace(lookupName))
            {
                throw new PayrollException($"Missing lookup name in row #{i}");
            }
            lookupName = workbook.EnsureNamespace(lookupName);
            var lookup = lookups.FirstOrDefault(x => string.Equals(x.Name, lookupName));
            if (lookup == null)
            {
                throw new PayrollException($"Unknown lookup {lookupName} in row #{i}");
            }

            // key
            var key = row.GetCellValue<string>(columns.KeyColumn);
            if (string.IsNullOrWhiteSpace(key))
            {
                throw new PayrollException($"Missing lookup value key in row #{i}");
            }
            var existing = lookupValues.FirstOrDefault(x => string.Equals(x.Key, key));
            if (existing != null)
            {
                throw new PayrollException($"Duplicated lookup value key {key} in row #{i}");
            }

            var lookupValue = new LookupValue
            {
                Created = row.GetCellValue<DateTime>(columns.CreatedColumn),

                Key = key,
                Value = row.GetCellValue<string>(columns.ValueColumn),
                ValueLocalizations = row.GetLocalizations(columns.ValueLocalizationsColumns),
                RangeValue = row.GetCellValue<decimal?>(columns.RangeValueColumn),
                OverrideType = row.GetEnumValue(columns.OverrideTypeColumn, OverrideType.Active)
            };

            // unique check
            lookupValues.Add(lookupValue);

            // case
            lookup.Values ??= [];
            lookup.Values.Add(lookupValue);
        }
    }
}