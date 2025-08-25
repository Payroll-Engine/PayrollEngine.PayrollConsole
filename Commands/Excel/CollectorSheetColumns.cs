using System;
using System.Collections.Generic;
using NPOI.SS.UserModel;
using PayrollEngine.Client.Model;

namespace PayrollEngine.PayrollConsole.Commands.Excel;

internal sealed class CollectorSheetColumns
{
    internal int? CreatedColumn { get; }
    internal int? NameColumn { get; }
    internal Dictionary<int, string> NameLocalizationsColumns { get; }
    internal int? CollectModeColumn { get; }
    internal int? NegatedColumn { get; }
    internal int? OverrideTypeColumn { get; }
    internal int? ValueTypeColumn { get; }
    internal int? CollectorGroupsColumn { get; }
    internal int? ThresholdColumn { get; }
    internal int? MinResultColumn { get; }
    internal int? MaxResultColumn { get; }
    internal int? StartExpressionColumn { get; }
    internal int? ApplyExpressionColumn { get; }
    internal int? EndExpressionColumn { get; }
    internal int? ClustersColumn { get; }
    internal int? AttributesColumn { get; }

    internal CollectorSheetColumns(ISheet sheet)
    {
        if (sheet == null)
        {
            throw new ArgumentNullException(nameof(sheet));
        }
        CreatedColumn = sheet.GetHeaderColumn(nameof(Collector.Created));
        NameColumn = sheet.GetHeaderColumn(nameof(Collector.Name));
        NameLocalizationsColumns = sheet.GetHeaderMultiColumns(nameof(Collector.Name));
        CollectModeColumn = sheet.GetHeaderColumn(nameof(Collector.CollectMode));
        NegatedColumn = sheet.GetHeaderColumn(nameof(Collector.Negated));
        OverrideTypeColumn = sheet.GetHeaderColumn(nameof(Collector.OverrideType));
        ValueTypeColumn = sheet.GetHeaderColumn(nameof(Collector.ValueType));
        CollectorGroupsColumn = sheet.GetHeaderColumn(nameof(Collector.CollectorGroups));
        ThresholdColumn = sheet.GetHeaderColumn(nameof(Collector.Threshold));
        MinResultColumn = sheet.GetHeaderColumn(nameof(Collector.MinResult));
        MaxResultColumn = sheet.GetHeaderColumn(nameof(Collector.MaxResult));
        StartExpressionColumn = sheet.GetHeaderColumn(nameof(Collector.StartExpression));
        ApplyExpressionColumn = sheet.GetHeaderColumn(nameof(Collector.ApplyExpression));
        EndExpressionColumn = sheet.GetHeaderColumn(nameof(Collector.EndExpression));
        ClustersColumn = sheet.GetHeaderColumn(nameof(Collector.Clusters));
        AttributesColumn = sheet.GetHeaderColumn(nameof(Collector.Attributes));
    }
}