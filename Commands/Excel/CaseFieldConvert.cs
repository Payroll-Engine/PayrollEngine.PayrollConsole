using System;
using System.Collections.Generic;
using System.Linq;
using NPOI.SS.UserModel;
using PayrollEngine.Client.Model;

namespace PayrollEngine.PayrollConsole.Commands.Excel;

internal static class CaseFieldConvert
{
    internal static void ReadCaseFields(IWorkbook workbook, IList<CaseSet> cases)
    {
        // worksheet
        if (!workbook.HasSheet(SheetSpecification.CaseField))
        {
            return;
        }
        var worksheet = workbook.GetSheet(SheetSpecification.CaseField);

        // no data rows
        if (worksheet.PhysicalNumberOfRows < 2)
        {
            return;
        }

        // columns
        var columns = new CaseFieldSheetColumns(worksheet);
        if (columns.CaseColumn == null)
        {
            throw new PayrollException($"Missing case column {nameof(Case.Name)}.");
        }
        if (columns.NameColumn == null)
        {
            throw new PayrollException($"Missing case field column {nameof(Case.Name)}.");
        }

        // case fields
        var caseFields = new List<CaseFieldSet>();
        for (var i = 1; i < worksheet.PhysicalNumberOfRows; i++)
        {
            var row = worksheet.GetRow(i);
            if (row.IsBlank())
            {
                continue;
            }

            // case
            var caseName = row.GetCellValue<string>(columns.CaseColumn);
            if (string.IsNullOrWhiteSpace(caseName))
            {
                throw new PayrollException($"Missing case name in row #{i}");
            }

            caseName = workbook.EnsureNamespace(caseName);
            var @case = cases.FirstOrDefault(x => string.Equals(x.Name, caseName));
            if (@case == null)
            {
                throw new PayrollException($"Unknown case {caseName} in row #{i}");
            }

            // name
            var name = row.GetCellValue<string>(columns.NameColumn);
            if (string.IsNullOrWhiteSpace(name))
            {
                throw new PayrollException($"Missing case field name in row #{i}");
            }
            var existing = caseFields.FirstOrDefault(x => string.Equals(x.Name, name));
            if (existing != null)
            {
                throw new PayrollException($"Duplicated case field {name} in row #{i}");
            }

            var caseField = new CaseFieldSet
            {
                Created = row.GetCellValue<DateTime>(columns.CreatedColumn),

                Name = workbook.EnsureNamespace(name),
                NameLocalizations = row.GetLocalizations(columns.NameLocalizationsColumns),
                Description = row.GetCellValue<string>(columns.DescriptionColumn),
                DescriptionLocalizations = row.GetLocalizations(columns.DescriptionLocalizationsColumns),
                ValueType = row.GetEnumValue(columns.ValueTypeColumn, ValueType.String),
                ValueScope = row.GetEnumValue(columns.ValueScopeColumn, ValueScope.Local),
                TimeType = row.GetEnumValue(columns.TimeTypeColumn, CaseFieldTimeType.Timeless),
                TimeUnit = row.GetEnumValue(columns.TimeUnitColumn, CaseFieldTimeUnit.Day),
                PeriodAggregation = row.GetEnumValue(columns.PeriodAggregationColumn, CaseFieldAggregationType.Summary),
                OverrideType = row.GetEnumValue(columns.OverrideTypeColumn, OverrideType.Active),
                CancellationMode = row.GetEnumValue(columns.CancellationModeColumn, CaseFieldCancellationMode.TimeType),
                ValueCreationMode = row.GetEnumValue(columns.ValueCreationModeColumn, CaseValueCreationMode.OnChanges),
                Culture = row.GetCellValue<string>(columns.CultureColumn),
                ValueMandatory = row.GetCellValue<bool>(columns.ValueMandatoryColumn),
                Order = row.GetCellValue<int>(columns.OrderColumn),
                StartDateType = row.GetEnumValue(columns.StartDateTypeColumn, CaseFieldDateType.Day),
                EndDateType = row.GetEnumValue(columns.EndDateTypeColumn, CaseFieldDateType.Day),
                EndMandatory = row.GetCellValue<bool>(columns.EndMandatoryColumn),
                DefaultStart = row.GetCellValue<string>(columns.DefaultStartColumn),
                DefaultEnd = row.GetCellValue<string>(columns.DefaultEndColumn),
                DefaultValue = row.GetCellValue<string>(columns.DefaultValueColumn),
                BuildActions = row.GetCellStringValues(columns.BuildActionsColumn),
                ValidateActions = row.GetCellStringValues(columns.ValidateActionsColumn),
                Tags = row.GetCellStringValues(columns.TagsColumn),
                LookupSettings = row.GetJsonObject<LookupSettings>(columns.LookupSettingsColumn),
                Clusters = row.GetCellStringValues(columns.ClustersColumn),
                Attributes = row.GetAttributes(columns.AttributesColumn),
                ValueAttributes = row.GetAttributes(columns.ValueAttributesColumn)
            };

            // unique check
            caseFields.Add(caseField);

            // case
            @case.Fields ??= [];
            @case.Fields.Add(caseField);
        }
    }
}