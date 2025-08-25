using System;
using System.Linq;
using System.Collections.Generic;
using NPOI.SS.UserModel;
using PayrollEngine.Client.Model;

namespace PayrollEngine.PayrollConsole.Commands.Excel;

internal static class WageTypeImport
{
    internal static List<WageType> Import(IWorkbook workbook)
    {
        // worksheet
        if (!workbook.HasSheet(SheetSpecification.WageType))
        {
            return null;
        }
        var worksheet = workbook.GetSheet(SheetSpecification.WageType);

        // no data rows
        if (worksheet.PhysicalNumberOfRows < 2)
        {
            return null;
        }

        // columns
        var columns = new WageTypeSheetColumns(worksheet);
        if (columns.WageTypeNumberColumn == null)
        {
            throw new PayrollException($"Missing wage type number column {nameof(Case.Name)}.");
        }
        if (columns.NameColumn == null)
        {
            throw new PayrollException($"Missing wage type name column {nameof(Case.Name)}.");
        }

        // wage types
        var wageTypes = new List<WageType>();
        for (var i = 1; i < worksheet.PhysicalNumberOfRows; i++)
        {
            var row = worksheet.GetRow(i);
            if (row.IsBlank())
            {
                continue;
            }

            // number
            var number = row.GetCellValue<decimal?>(columns.WageTypeNumberColumn);
            if (number == null || number == 0)
            {
                throw new PayrollException($"Missing wage type number in row #{i}");
            }
            var existing = wageTypes.FirstOrDefault(x => x.WageTypeNumber == number.Value);
            if (existing != null)
            {
                throw new PayrollException($"Duplicated wage type {number} in row #{i}");
            }

            // name
            var name = row.GetCellValue<string>(columns.NameColumn);
            if (string.IsNullOrWhiteSpace(name))
            {
                throw new PayrollException($"Missing wage type name in row #{i}");
            }

            // collectors
            var collectors = row.GetCellStringValues(columns.CollectorsColumn);
            if (collectors != null)
            {
                collectors = collectors.Select(workbook.EnsureNamespace).ToList();
            }

            // collector groups
            var collectorGroups = row.GetCellStringValues(columns.CollectorGroupsColumn);
            if (collectorGroups != null)
            {
                collectorGroups = collectorGroups.Select(workbook.EnsureNamespace).ToList();
            }

            var wageType = new WageType
            {
                Created = row.GetCellValue<DateTime>(columns.CreatedColumn),
                WageTypeNumber = number.Value,
                Name = workbook.EnsureNamespace(name),
                NameLocalizations = row.GetLocalizations(columns.NameLocalizationsColumns),
                Description = row.GetCellValue<string>(columns.DescriptionColumn),
                DescriptionLocalizations = row.GetLocalizations(columns.DescriptionLocalizationsColumns),
                OverrideType = row.GetEnumValue(columns.OverrideTypeColumn, OverrideType.Active),
                ValueType = row.GetEnumValue(columns.ValueTypeColumn, ValueType.Decimal),
                Collectors = collectors,
                CollectorGroups = collectorGroups,
                Calendar = row.GetCellValue<string>(columns.CalendarColumn),
                ValueExpression = row.GetCellValue<string>(columns.ValueExpressionColumn),
                ResultExpression = row.GetCellValue<string>(columns.ResultExpressionColumn),
                Clusters = row.GetCellStringValues(columns.ClustersColumn),
                Attributes = row.GetAttributes(columns.AttributesColumn)
            };

            wageTypes.Add(wageType);
        }

        return wageTypes;
    }
}