using System;
using System.Collections.Generic;
using NPOI.SS.UserModel;
using PayrollEngine.Client.Model;

namespace PayrollEngine.PayrollConsole.Commands.Excel;

internal sealed class WageTypeSheetColumns
{
    internal int? CreatedColumn { get; }
    internal int? WageTypeNumberColumn { get; }
    internal int? NameColumn { get; }
    internal Dictionary<int, string> NameLocalizationsColumns { get; }
    internal int? DescriptionColumn { get; }
    internal Dictionary<int, string> DescriptionLocalizationsColumns { get; }
    internal int? OverrideTypeColumn { get; }
    internal int? ValueTypeColumn { get; }
    internal int? CalendarColumn { get; }
    internal int? CollectorsColumn { get; }
    internal int? CollectorGroupsColumn { get; }
    internal int? ValueExpressionColumn { get; }
    internal int? ResultExpressionColumn { get; }
    internal int? ClustersColumn { get; }
    internal int? AttributesColumn { get; }

    internal WageTypeSheetColumns(ISheet sheet)
    {
        if (sheet == null)
        {
            throw new ArgumentNullException(nameof(sheet));
        }
        CreatedColumn = sheet.GetHeaderColumn(nameof(WageType.Created));
        WageTypeNumberColumn = sheet.GetHeaderColumn(nameof(WageType.WageTypeNumber));
        NameColumn = sheet.GetHeaderColumn(nameof(WageType.Name));
        NameLocalizationsColumns = sheet.GetHeaderMultiColumns(nameof(WageType.Name));
        DescriptionColumn = sheet.GetHeaderColumn(nameof(WageType.Description));
        DescriptionLocalizationsColumns = sheet.GetHeaderMultiColumns(nameof(WageType.Description));
        OverrideTypeColumn = sheet.GetHeaderColumn(nameof(WageType.OverrideType));
        ValueTypeColumn = sheet.GetHeaderColumn(nameof(WageType.ValueType));
        CalendarColumn = sheet.GetHeaderColumn(nameof(WageType.Calendar));
        CollectorsColumn = sheet.GetHeaderColumn(nameof(WageType.Collectors));
        CollectorGroupsColumn = sheet.GetHeaderColumn(nameof(WageType.CollectorGroups));
        ValueExpressionColumn = sheet.GetHeaderColumn(nameof(WageType.ValueExpression));
        ResultExpressionColumn = sheet.GetHeaderColumn(nameof(WageType.ResultExpression));
        ClustersColumn = sheet.GetHeaderColumn(nameof(WageType.Clusters));
        AttributesColumn = sheet.GetHeaderColumn(nameof(WageType.Attributes));
    }
}