using System;
using NPOI.SS.UserModel;
using PayrollEngine.Client.Model;

namespace PayrollEngine.PayrollConsole.Commands.Excel;

internal static class RegulationImport
{
    internal static RegulationSet Import(IWorkbook workbook)
    {
        // regulation worksheet
        if (!workbook.HasSheet(SheetSpecification.Regulation))
        {
            throw new PayrollException($"Missing sheet {SheetSpecification.Regulation}.");
        }

        var worksheet = workbook.GetSheet(SheetSpecification.Regulation);

        // only one row allowed
        if (worksheet.PhysicalNumberOfRows > 2)
        {
            throw new PayrollException($"Only one row allowed in sheet {SheetSpecification.Regulation}.");
        }

        // columns
        var columns = new RegulationSheetColumns(worksheet);
        if (columns.NameColumn == null)
        {
            throw new PayrollException($"Missing regulation column {nameof(Regulation.Name)}.");
        }

        var row = worksheet.GetRow(1);
        if (row.IsBlank())
        {
            throw new PayrollException($"Missing regulation in sheet {SheetSpecification.Regulation}.");
        }

        // regulation set
        var regulation = new RegulationSet
        {
            Name = workbook.EnsureNamespace(row.GetCellValue<string>(columns.NameColumn)),
            NameLocalizations = row.GetLocalizations(columns.NameLocalizationsColumns),
            Version = row.GetCellValue<int>(columns.VersionColumn),
            SharedRegulation = row.GetCellValue<bool>(columns.SharedRegulationColumn),
            ValidFrom = row.GetCellValue<DateTime?>(columns.ValidFromColumn),
            Owner = row.GetCellValue<string>(columns.OwnerColumn),
            BaseRegulations = row.GetCellStringValues(columns.BaseRegulationsColumn),
            Description = row.GetCellValue<string>(columns.DescriptionColumn),
            DescriptionLocalizations = row.GetLocalizations(columns.DescriptionLocalizationsColumns),
            Created = row.GetCellValue<DateTime>(columns.CreatedColumn),
            Attributes = row.GetAttributes(columns.AttributesColumn)
        };

        // mandatory test
        if (string.IsNullOrWhiteSpace(regulation.Name))
        {
            throw new PayrollException("Missing regulation name");
        }

        return regulation;
    }
}