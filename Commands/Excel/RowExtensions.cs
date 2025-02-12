using System.Linq;
using NPOI.SS.UserModel;

namespace PayrollEngine.PayrollConsole.Commands.Excel;

public static class RowExtensions
{
    public static bool IsBlank(this IRow row)
    {
        if (row == null || !row.Cells.Any())
        {
            return true;
        }

        foreach (var cell in row.Cells)
        {
            if (cell.CellType is CellType.Blank or CellType.Unknown)
            {
                continue;
            }
            return false;
        }
        return true;
    }
}