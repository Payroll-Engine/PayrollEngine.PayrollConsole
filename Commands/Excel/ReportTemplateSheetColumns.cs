using System;
using NPOI.SS.UserModel;
using PayrollEngine.Client.Model;

namespace PayrollEngine.PayrollConsole.Commands.Excel;

internal sealed class ReportTemplateSheetColumns
{
    internal int? CreatedColumn { get; }
    internal int? ReportColumn { get; }
    internal int? NameColumn { get; }
    internal int? CultureColumn { get; }
    internal int? ContentColumn { get; }
    internal int? ContentTypeColumn { get; }
    internal int? SchemaColumn { get; }
    internal int? ResourceColumn { get; }
    internal int? OverrideTypeColumn { get; }
    internal int? AttributesColumn { get; }

    internal ReportTemplateSheetColumns(ISheet sheet)
    {
        if (sheet == null)
        {
            throw new ArgumentNullException(nameof(sheet));
        }
        CreatedColumn = sheet.GetHeaderColumn(nameof(ReportTemplate.Created));
        ReportColumn = sheet.GetHeaderColumn(SheetSpecification.ReportRefName);
        NameColumn = sheet.GetHeaderColumn(nameof(ReportTemplate.Name));
        CultureColumn = sheet.GetHeaderColumn(nameof(ReportTemplate.Culture));
        ContentColumn = sheet.GetHeaderColumn(nameof(ReportTemplate.Content));
        ContentTypeColumn = sheet.GetHeaderColumn(nameof(ReportTemplate.ContentType));
        SchemaColumn = sheet.GetHeaderColumn(nameof(ReportTemplate.Schema));
        ResourceColumn = sheet.GetHeaderColumn(nameof(ReportTemplate.Resource));
        OverrideTypeColumn = sheet.GetHeaderColumn(nameof(ReportTemplate.OverrideType));
        AttributesColumn = sheet.GetHeaderColumn(nameof(ReportTemplate.Attributes));
    }
}