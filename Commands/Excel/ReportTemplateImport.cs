using System;
using System.Linq;
using System.Collections.Generic;
using NPOI.SS.UserModel;
using PayrollEngine.Client.Model;

namespace PayrollEngine.PayrollConsole.Commands.Excel;

internal static class ReportTemplateImport
{
    internal static void Import(IWorkbook workbook, List<ReportSet> reports)
    {
        //  worksheet
        if (!workbook.HasSheet(SheetSpecification.ReportTemplate))
        {
            return;
        }
        var worksheet = workbook.GetSheet(SheetSpecification.ReportTemplate);

        // no data rows
        if (worksheet.PhysicalNumberOfRows < 2)
        {
            return;
        }

        // columns
        var columns = new ReportTemplateSheetColumns(worksheet);
        if (columns.ReportColumn == null)
        {
            throw new PayrollException($"Missing report column {SheetSpecification.ReportRefName}.");
        }
        if (columns.NameColumn == null)
        {
            throw new PayrollException($"Missing report template column {nameof(Case.Name)}.");
        }

        // report templates
        var templates = new List<ReportTemplate>();
        for (var i = 1; i < worksheet.PhysicalNumberOfRows; i++)
        {
            var row = worksheet.GetRow(i);
            if (row.IsBlank())
            {
                continue;
            }

            // report
            var reportName = row.GetCellValue<string>(columns.ReportColumn);
            if (string.IsNullOrWhiteSpace(reportName))
            {
                throw new PayrollException($"Missing report name in row #{i}");
            }
            reportName = workbook.EnsureNamespace(reportName);
            var report = reports.FirstOrDefault(x => string.Equals(x.Name, reportName));
            if (report == null)
            {
                throw new PayrollException($"Unknown report {reportName} in row #{i}");
            }

            // name
            var name = row.GetCellValue<string>(columns.NameColumn);
            if (string.IsNullOrWhiteSpace(name))
            {
                throw new PayrollException($"Missing report template name in row #{i}");
            }
            var existing = templates.FirstOrDefault(x => string.Equals(x.Name, name));
            if (existing != null)
            {
                throw new PayrollException($"Duplicated report template {name} in row #{i}");
            }

            var template = new ReportTemplate
            {
                Created = row.GetCellValue<DateTime>(columns.CreatedColumn),
                Name = name,
                Culture = row.GetCellValue<string>(columns.CultureColumn),
                Content = row.GetCellValue<string>(columns.ContentColumn),
                ContentType = row.GetCellValue<string>(columns.ContentTypeColumn),
                Schema = row.GetCellValue<string>(columns.SchemaColumn),
                Resource = row.GetCellValue<string>(columns.ResourceColumn),
                OverrideType = row.GetEnumValue(columns.OverrideTypeColumn, OverrideType.Active),
                Attributes = row.GetAttributes(columns.AttributesColumn),
            };

            // unique check
            templates.Add(template);

            // case
            report.Templates ??= [];
            report.Templates.Add(template);
        }
    }
}