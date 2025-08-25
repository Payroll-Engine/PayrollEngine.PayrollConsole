using System;
using System.Collections.Generic;
using NPOI.SS.UserModel;
using PayrollEngine.Client.Model;

namespace PayrollEngine.PayrollConsole.Commands.Excel;

internal sealed class RegulationSheetColumns
{
    internal int? CreatedColumn { get; }
    internal int? NameColumn { get; }
    internal Dictionary<int,string> NameLocalizationsColumns { get; }
    internal int? VersionColumn { get; }
    internal int? SharedRegulationColumn { get; }
    internal int? ValidFromColumn { get; }
    internal int? OwnerColumn { get; }
    internal int? BaseRegulationsColumn { get; }
    internal int? DescriptionColumn { get; }
    internal Dictionary<int,string> DescriptionLocalizationsColumns { get; }
    internal int? AttributesColumn { get; }

    internal RegulationSheetColumns(ISheet sheet)
    {
        if (sheet == null)
        {
            throw new ArgumentNullException(nameof(sheet));
        }
        CreatedColumn = sheet.GetHeaderColumn(nameof(Regulation.Created));
        NameColumn = sheet.GetHeaderColumn(nameof(Regulation.Name));
        NameLocalizationsColumns = sheet.GetHeaderMultiColumns(nameof(Regulation.Name));
        DescriptionColumn = sheet.GetHeaderColumn(nameof(Regulation.Description));
        DescriptionLocalizationsColumns = sheet.GetHeaderMultiColumns(nameof(Regulation.Description));
        VersionColumn = sheet.GetHeaderColumn(nameof(Regulation.Version));
        SharedRegulationColumn = sheet.GetHeaderColumn(nameof(Regulation.SharedRegulation));
        ValidFromColumn = sheet.GetHeaderColumn(nameof(Regulation.ValidFrom));
        OwnerColumn = sheet.GetHeaderColumn(nameof(Regulation.Owner));
        BaseRegulationsColumn = sheet.GetHeaderColumn(nameof(Regulation.BaseRegulations));
        AttributesColumn = sheet.GetHeaderColumn(nameof(Regulation.Attributes));
    }
}