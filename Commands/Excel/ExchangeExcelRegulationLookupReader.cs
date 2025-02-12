using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using NPOI.SS.UserModel;
using PayrollEngine.Client;
using PayrollEngine.Client.Model;
using PayrollEngine.Client.Service.Api;
using Task = System.Threading.Tasks.Task;

namespace PayrollEngine.PayrollConsole.Commands.Excel;

internal static class ExchangeExcelRegulationLookupReader
{
    internal static async Task ReadAsync(PayrollHttpClient httpClient,
        ExchangeTenant tenant, IWorkbook workbook)
    {
        // payroll
        var regulation = await GetRegulationAsync(httpClient, workbook, tenant.Id);
        var payrolls = new List<RegulationSet>();
        if (tenant.Regulations != null)
        {
            payrolls.AddRange(tenant.Regulations);
        }
        if (!payrolls.Contains(regulation))
        {
            payrolls.Add(regulation);
            tenant.Regulations = payrolls;
        }

        // lookup sets
        var lookupSets = new List<LookupSet>();
        var sheets = workbook.GetSheetsOf(SpecificationSheet.LookupMask);
        // process all Lookup.* sheets
        foreach (var sheet in sheets)
        {
            var lookup = GetRegulationLookupSet(sheet);
            if (lookup != null)
            {
                lookupSets.Add(lookup);
            }
        }
        if (lookupSets.Any())
        {
            regulation.Lookups = lookupSets;
        }
    }

    private static LookupSet GetRegulationLookupSet(ISheet worksheet)
    {
        if (!worksheet.SheetName.StartsWith(SpecificationSheet.LookupMask, StringComparison.InvariantCultureIgnoreCase))
        {
            throw new PayrollException($"Invalid worksheet name {worksheet.SheetName}.");
        }

        // lookup name
        var name = worksheet.SheetName.RemoveFromStart(SpecificationSheet.LookupMask, StringComparison.InvariantCultureIgnoreCase);
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new PayrollException($"Invalid lookup name {worksheet.SheetName}.");
        }

        // columns
        var headerColumns = new HeaderColumns(worksheet);
        if (!headerColumns.KeyColumns.Any())
        {
            throw new PayrollException("Missing lookup key column(s).");
        }
        if (!headerColumns.ValueColumns.Any())
        {
            throw new PayrollException("Missing lookup value column(s).");
        }
        if (!headerColumns.RangeColumn.HasValue)
        {
            throw new PayrollException("Missing lookup range column.");
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

            var rangeValue = GetCellRangeValue(row, headerColumns.RangeColumn.Value);

            // key
            var keyObjects = GetKeyObjects(headerColumns.KeyColumns, row);
            // no keys
            if (!keyObjects.Any())
            {
                continue;
            }

            // values
            var values = GetCellValues(worksheet, headerColumns.ValueColumns, row);
            if (!values.Any())
            {
                // no values
                continue;
            }

            var lookupValue = new LookupValue
            {
                // first key value or key value array
                Key = keyObjects.Count == 1 ?
                    ClientJsonSerializer.Serialize(keyObjects.First()) :
                    ClientJsonSerializer.Serialize(keyObjects.ToArray()),
                // first value without name or name/value dictionary
                Value = values.Count == 1 ?
                    ClientJsonSerializer.Serialize(values.First().Value) :
                    ClientJsonSerializer.Serialize(values),
                RangeValue = rangeValue
            };

            // created date
            if (headerColumns.CreatedColumn.HasValue)
            {
                lookupCreatedDate = SetLookupCreatedDate(row, headerColumns.CreatedColumn.Value,
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
        return lookup;
    }

    private static DateTime? SetLookupCreatedDate(IRow row, int createdColumn, LookupValue lookupValue,
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

    private static List<object> GetKeyObjects(List<int> keyColumns, IRow row)
    {
        var keyObjects = new List<object>();
        foreach (var keyColumn in keyColumns)
        {
            var keyObject = row.GetCell(keyColumn).GetCellValue();
            keyObjects.Add(keyObject);
        }
        return keyObjects;
    }

    private static Dictionary<string, object> GetCellValues(ISheet worksheet, List<int> valueColumns, IRow row)
    {
        var values = new Dictionary<string, object>();
        foreach (var valueColumn in valueColumns)
        {
            var value = GetCellValue(worksheet, row, valueColumn);
            values.Add(value.Item1, value.Item1);
        }

        return values;
    }

    private static Tuple<string, object> GetCellValue(ISheet worksheet, IRow row, int valueColumn)
    {
        var value = row.GetCell(valueColumn).GetCellValue();
        var header = worksheet.HeaderCell(valueColumn).StringCellValue.Trim();
        string valueName;
        if (string.Equals(SpecificationLookup.Value, header, StringComparison.InvariantCultureIgnoreCase))
        {
            valueName = header;
        }
        else if (header.StartsWith(SpecificationLookup.ValueMask,
                     StringComparison.InvariantCultureIgnoreCase))
        {
            valueName = header.RemoveFromStart(SpecificationLookup.ValueMask,
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

    private static async Task<RegulationSet> GetRegulationAsync(PayrollHttpClient httpClient, IWorkbook workbook, int tenantId)
    {
        var regulationName = workbook.GetNamedValue<string>(Specification.RegulationRegionName);
        if (string.IsNullOrWhiteSpace(regulationName))
        {
            throw new PayrollException("Missing regulation name.");
        }

        // get existing regulation
        var regulation = await new RegulationService(httpClient).GetAsync<RegulationSet>(new(tenantId), regulationName);
        if (regulation == null)
        {
            throw new PayrollException($"Unknown regulation with name {regulationName}.");
        }
        return regulation;
    }
}