using System;
using System.Collections.Generic;
using NPOI.SS.UserModel;
using PayrollEngine.Client.Model;

namespace PayrollEngine.PayrollConsole.Commands.Excel;

internal sealed class ReportParameterSheetColumns
{
    internal int? CreatedColumn { get; }
    internal int? ReportColumn { get; }
    internal int? NameColumn { get; }
    internal Dictionary<int, string> NameLocalizationsColumns { get; }
    internal int? DescriptionColumn { get; }
    internal Dictionary<int, string> DescriptionLocalizationsColumns { get; }
    internal int? MandatoryColumn { get; }
    internal int? HiddenColumn { get; }
    internal int? ValueColumn { get; }
    internal int? ValueTypeColumn { get; }
    internal int? ParameterTypeColumn { get; }
    internal int? OverrideTypeColumn { get; }
    internal int? AttributesColumn { get; }

    internal ReportParameterSheetColumns(ISheet sheet)
    {
        if (sheet == null)
        {
            throw new ArgumentNullException(nameof(sheet));
        }
        CreatedColumn = sheet.GetHeaderColumn(nameof(ReportParameter.Created));
        ReportColumn = sheet.GetHeaderColumn(SheetSpecification.ReportRefName);
        NameColumn = sheet.GetHeaderColumn(nameof(ReportParameter.Name));
        NameLocalizationsColumns = sheet.GetHeaderMultiColumns(nameof(ReportParameter.Name));
        DescriptionColumn = sheet.GetHeaderColumn(nameof(ReportParameter.Description));
        DescriptionLocalizationsColumns = sheet.GetHeaderMultiColumns(nameof(ReportParameter.Description));
        MandatoryColumn = sheet.GetHeaderColumn(nameof(ReportParameter.Mandatory));
        HiddenColumn = sheet.GetHeaderColumn(nameof(ReportParameter.Hidden));
        ValueColumn = sheet.GetHeaderColumn(nameof(ReportParameter.Value));
        ValueTypeColumn = sheet.GetHeaderColumn(nameof(ReportParameter.ValueType));
        ParameterTypeColumn = sheet.GetHeaderColumn(nameof(ReportParameter.ParameterType));
        OverrideTypeColumn = sheet.GetHeaderColumn(nameof(ReportParameter.OverrideType));
        AttributesColumn = sheet.GetHeaderColumn(nameof(ReportParameter.Attributes));
    }
}