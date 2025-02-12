using System;
using System.Collections.Generic;
using NPOI.SS.UserModel;

namespace PayrollEngine.PayrollConsole.Commands.Excel;

internal sealed class HeaderColumns
{
    internal List<int> KeyColumns { get; } = [];
    internal List<int> ValueColumns { get; } = [];
    internal int? RangeColumn { get; }
    internal int? CreatedColumn { get; }

    internal HeaderColumns(ISheet sheet)
    {
        if (sheet == null)
        {
            throw new ArgumentNullException(nameof(sheet));
        }

        foreach (var headerCell in sheet.HeaderCells())
        {
            // key column
            if (headerCell.StringCellValue.Trim().StartsWith(SpecificationLookup.Key, StringComparison.InvariantCultureIgnoreCase))
            {
                KeyColumns.Add(headerCell.ColumnIndex);
            }
            // created column
            if (headerCell.StringCellValue.Trim().StartsWith(SpecificationLookup.Created, StringComparison.InvariantCultureIgnoreCase))
            {
                CreatedColumn = headerCell.ColumnIndex;
            }
            // value column
            if (headerCell.StringCellValue.Trim().StartsWith(SpecificationLookup.Value, StringComparison.InvariantCultureIgnoreCase))
            {
                ValueColumns.Add(headerCell.ColumnIndex);
            }
            // range column
            if (headerCell.StringCellValue.Trim().Equals(SpecificationLookup.Range, StringComparison.InvariantCultureIgnoreCase))
            {
                RangeColumn = headerCell.ColumnIndex;
            }
        }
    }
}