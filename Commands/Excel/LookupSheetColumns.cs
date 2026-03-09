using System;
using System.Collections.Generic;
using NPOI.SS.UserModel;
using PayrollEngine.Client.Model;

namespace PayrollEngine.PayrollConsole.Commands.Excel;

/// <summary>Maps the <see cref="SheetSpecification.Lookup"/> Excel sheet columns to their indexes.</summary>
internal sealed class LookupSheetColumns
{
    internal int? CreatedColumn { get; }
    internal int? NameColumn { get; }
    internal Dictionary<int, string> NameLocalizationsColumns { get; }
    internal int? DescriptionColumn { get; }
    internal Dictionary<int, string> DescriptionLocalizationsColumns { get; }
    internal int? RangeSizeColumn { get; }
    internal int? OverrideTypeColumn { get; }
    internal int? AttributesColumn { get; }

    internal LookupSheetColumns(ISheet sheet)
    {
        ArgumentNullException.ThrowIfNull(sheet);
        CreatedColumn = sheet.GetHeaderColumn(nameof(Lookup.Created));
        NameColumn = sheet.GetHeaderColumn(nameof(Lookup.Name));
        NameLocalizationsColumns = sheet.GetHeaderMultiColumns(nameof(Lookup.Name));
        DescriptionColumn = sheet.GetHeaderColumn(nameof(Lookup.Description));
        DescriptionLocalizationsColumns = sheet.GetHeaderMultiColumns(nameof(Lookup.Description));
        RangeSizeColumn = sheet.GetHeaderColumn(nameof(Lookup.RangeSize));
        OverrideTypeColumn = sheet.GetHeaderColumn(nameof(Lookup.OverrideType));
        AttributesColumn = sheet.GetHeaderColumn(nameof(Lookup.Attributes));
    }
}
