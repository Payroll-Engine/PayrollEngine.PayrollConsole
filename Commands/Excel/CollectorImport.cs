using System;
using System.Collections.Generic;
using System.Linq;
using NPOI.SS.UserModel;
using PayrollEngine.Client.Model;

namespace PayrollEngine.PayrollConsole.Commands.Excel;

internal static class CollectorImport
{
    internal static List<Collector> Import(IWorkbook workbook)
    {
        // worksheet
        if (!workbook.HasSheet(SheetSpecification.Collector))
        {
            return null;
        }
        var worksheet = workbook.GetSheet(SheetSpecification.Collector);

        // no data rows
        if (worksheet.PhysicalNumberOfRows < 2)
        {
            return null;
        }

        // columns
        var columns = new CollectorSheetColumns(worksheet);
        if (columns.NameColumn == null)
        {
            throw new PayrollException($"Missing collector column {nameof(Case.Name)}.");
        }

        // collectors
        var collectors = new List<Collector>();
        for (var i = 1; i < worksheet.PhysicalNumberOfRows; i++)
        {
            var row = worksheet.GetRow(i);
            if (row.IsBlank())
            {
                continue;
            }

            // name
            var name = row.GetCellValue<string>(columns.NameColumn);
            if (string.IsNullOrWhiteSpace(name))
            {
                throw new PayrollException($"Missing collector name in row #{i}");
            }
            var existing = collectors.FirstOrDefault(x => string.Equals(x.Name, name));
            if (existing != null)
            {
                throw new PayrollException($"Duplicated collector {name} in row #{i}");
            }

            // groups
            var groups = row.GetCellStringValues(columns.CollectorGroupsColumn);
            if (groups != null)
            {
                groups = groups.Select(workbook.EnsureNamespace).ToList();
            }

            var collector = new Collector
            {
                Created = row.GetCellValue<DateTime>(columns.CreatedColumn),
                Name = workbook.EnsureNamespace(name),
                NameLocalizations = row.GetLocalizations(columns.NameLocalizationsColumns),
                CollectMode = row.GetEnumValue(columns.CollectModeColumn, CollectMode.Summary),
                Negated = row.GetCellValue<bool>(columns.NegatedColumn),
                OverrideType = row.GetEnumValue(columns.OverrideTypeColumn, OverrideType.Active),
                ValueType = row.GetEnumValue(columns.ValueTypeColumn, ValueType.Decimal),
                CollectorGroups = groups,
                Threshold = row.GetCellValue<decimal?>(columns.ThresholdColumn),
                MinResult = row.GetCellValue<decimal?>(columns.MinResultColumn),
                MaxResult = row.GetCellValue<decimal?>(columns.MaxResultColumn),
                StartExpression = row.GetCellValue<string>(columns.StartExpressionColumn),
                ApplyExpression = row.GetCellValue<string>(columns.ApplyExpressionColumn),
                EndExpression = row.GetCellValue<string>(columns.EndExpressionColumn),
                Clusters = row.GetCellStringValues(columns.ClustersColumn),
                Attributes = row.GetAttributes(columns.AttributesColumn)
            };

            collectors.Add(collector);
        }

        return collectors;
    }
}