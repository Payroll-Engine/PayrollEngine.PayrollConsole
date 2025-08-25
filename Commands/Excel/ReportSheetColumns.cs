using System;
using System.Collections.Generic;
using NPOI.SS.UserModel;
using PayrollEngine.Client.Model;

namespace PayrollEngine.PayrollConsole.Commands.Excel;

internal sealed class ReportSheetColumns
{
    internal int? CreatedColumn { get; }
    internal int? NameColumn { get; }
    internal Dictionary<int, string> NameLocalizationsColumns { get; }
    internal int? DescriptionColumn { get; }
    internal Dictionary<int, string> DescriptionLocalizationsColumns { get; }
    internal int? CategoryColumn { get; }
    internal int? AttributeModeColumn { get; }
    internal int? UserTypeColumn { get; }
    internal int? BuildExpressionColumn { get; }
    internal int? StartExpressionColumn { get; }
    internal int? EndExpressionColumn { get; }
    internal int? OverrideTypeColumn { get; }
    internal int? QueriesColumn { get; }
    internal int? RelationsColumn { get; }
    internal int? ClustersColumn { get; }
    internal int? AttributesColumn { get; }

    internal ReportSheetColumns(ISheet sheet)
    {
        if (sheet == null)
        {
            throw new ArgumentNullException(nameof(sheet));
        }
        CreatedColumn = sheet.GetHeaderColumn(nameof(Report.Created));
        NameColumn = sheet.GetHeaderColumn(nameof(Report.Name));
        NameLocalizationsColumns = sheet.GetHeaderMultiColumns(nameof(Report.Name));
        DescriptionColumn = sheet.GetHeaderColumn(nameof(Report.Description));
        DescriptionLocalizationsColumns = sheet.GetHeaderMultiColumns(nameof(Report.Description));
        CategoryColumn = sheet.GetHeaderColumn(nameof(Report.Category));
        AttributeModeColumn = sheet.GetHeaderColumn(nameof(Report.AttributeMode));
        UserTypeColumn = sheet.GetHeaderColumn(nameof(Report.UserType));
        BuildExpressionColumn = sheet.GetHeaderColumn(nameof(Report.BuildExpression));
        StartExpressionColumn = sheet.GetHeaderColumn(nameof(Report.StartExpression));
        EndExpressionColumn = sheet.GetHeaderColumn(nameof(Report.EndExpression));
        OverrideTypeColumn = sheet.GetHeaderColumn(nameof(Report.OverrideType));
        QueriesColumn = sheet.GetHeaderColumn(nameof(Report.Queries));
        RelationsColumn = sheet.GetHeaderColumn(nameof(Report.Relations));
        ClustersColumn = sheet.GetHeaderColumn(nameof(Report.Clusters));
        AttributesColumn = sheet.GetHeaderColumn(nameof(Report.Attributes));
    }
}