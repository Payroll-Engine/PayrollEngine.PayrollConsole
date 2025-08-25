using System;
using System.Linq;
using System.Collections.Generic;
using NPOI.SS.UserModel;
using PayrollEngine.Client.Model;

namespace PayrollEngine.PayrollConsole.Commands.Excel;

internal static class ReportImport
{
    internal static List<ReportSet> Import(IWorkbook workbook)
    {
        // worksheet
        if (!workbook.HasSheet(SheetSpecification.Report))
        {
            return null;
        }

        var worksheet = workbook.GetSheet(SheetSpecification.Report);

        // no data rows
        if (worksheet.PhysicalNumberOfRows < 2)
        {
            return null;
        }

        // columns
        var columns = new ReportSheetColumns(worksheet);
        if (columns.NameColumn == null)
        {
            throw new PayrollException($"Missing report name column {nameof(Case.Name)}.");
        }

        // reports
        var reports = new List<ReportSet>();
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
                throw new PayrollException($"Missing report name in row #{i}");
            }
            var existing = reports.FirstOrDefault(x => string.Equals(x.Name, name));
            if (existing != null)
            {
                throw new PayrollException($"Duplicated report {name} in row #{i}");
            }

            var report = new ReportSet
            {
                Created = row.GetCellValue<DateTime>(columns.CreatedColumn),
                Name = workbook.EnsureNamespace(name),
                NameLocalizations = row.GetLocalizations(columns.NameLocalizationsColumns),
                Description = row.GetCellValue<string>(columns.DescriptionColumn),
                DescriptionLocalizations = row.GetLocalizations(columns.DescriptionLocalizationsColumns),
                Category = row.GetCellValue<string>(columns.CategoryColumn),
                AttributeMode = row.GetEnumValue(columns.AttributeModeColumn, ReportAttributeMode.Json),
                UserType = row.GetEnumValue(columns.UserTypeColumn, UserType.Employee),
                BuildExpression = row.GetCellValue<string>(columns.BuildExpressionColumn),
                StartExpression = row.GetCellValue<string>(columns.StartExpressionColumn),
                EndExpression = row.GetCellValue<string>(columns.EndExpressionColumn),
                OverrideType = row.GetEnumValue(columns.OverrideTypeColumn, OverrideType.Active),
                Queries = row.GetJsonObject<Dictionary<string,string>>(columns.QueriesColumn),
                Relations = row.GetJsonObjectArray<Data.DataRelation>(columns.RelationsColumn),
                Clusters = row.GetCellStringValues(columns.ClustersColumn),
                Attributes = row.GetAttributes(columns.AttributesColumn)
            };

            reports.Add(report);
        }

        return reports;
    }
}