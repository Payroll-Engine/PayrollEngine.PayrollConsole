using System;
using System.Collections.Generic;
using PayrollEngine.Client;
using PayrollEngine.PayrollConsole.Shared;

namespace PayrollEngine.PayrollConsole.Arguments;

public static class ArgumentManager
{
    // fixed order arguments
    public static Operation GetOperation(Operation defaultOperation) =>
        ConsoleArguments.GetEnum(1, defaultOperation);

    // global enum toggles
    public static ConsoleDisplayMode DisplayMode(ConsoleDisplayMode defaultValue = default) =>
        ConsoleArguments.GetEnumToggle(defaultValue);

    public static ConsoleErrorMode ErrorMode(ConsoleErrorMode defaultValue = default) =>
        ConsoleArguments.GetEnumToggle(defaultValue);

    public static ObjectDeleteMode DeleteMode(ObjectDeleteMode defaultValue = default) =>
        ConsoleArguments.GetEnumToggle(defaultValue);

    public static PayrollConsoleWaitMode WaitMode(PayrollConsoleWaitMode defaultValue = default) =>
        ConsoleArguments.GetEnumToggle(defaultValue);

    public static string TestArguments(Operation operation)
    {
        // argument order
        if (!ConsoleArguments.IsValidOrder())
        {
            return "Invalid argument order";
        }

        // test toggles
        var toggles = new List<Type>
        {
            typeof(ConsoleDisplayMode),
            typeof(ConsoleErrorMode),
            typeof(ObjectDeleteMode),
            typeof(PayrollConsoleWaitMode)
        };

        Type[] operationToggles = null;
        switch (operation)
        {
            // common operations
            case Operation.Help:
                break;
            case Operation.UserVariable:
                operationToggles = UserVariableArguments.Toggles;
                break;
            case Operation.Stopwatch:
                operationToggles = StopwatchArguments.Toggles;
                break;

            // action
            case Operation.ActionReport:
                operationToggles = ActionReportArguments.Toggles;
                break;

            // system
            case Operation.HttpGet:
                operationToggles = HttpGetArguments.Toggles;
                break;
            case Operation.HttpPost:
                operationToggles = HttpPostArguments.Toggles;
                break;
            case Operation.HttpPut:
                operationToggles = HttpPutArguments.Toggles;
                break;
            case Operation.HttpDelete:
                operationToggles = HttpDeleteArguments.Toggles;
                break;
            case Operation.LogTrail:
                operationToggles = LogTrailArguments.Toggles;
                break;

            // payroll
            case Operation.PayrollResults:
                operationToggles = PayrollResultsArguments.Toggles;
                break;
            case Operation.PayrollImport:
                operationToggles = PayrollImportArguments.Toggles;
                break;
            case Operation.PayrollImportExcel:
                operationToggles = PayrollImportExcelArguments.Toggles;
                break;
            case Operation.PayrollExport:
                operationToggles = PayrollExportArguments.Toggles;
                break;

            // report
            case Operation.Report:
                operationToggles = ReportArguments.Toggles;
                break;
            case Operation.DataReport:
                operationToggles = DataReportArguments.Toggles;
                break;

            // test
            case Operation.CaseTest:
                operationToggles = CaseTestArguments.Toggles;
                break;
            case Operation.ReportTest:
                operationToggles = ReportTestArguments.Toggles;
                break;
            case Operation.PayrunTest:
                operationToggles = PayrunTestArguments.Toggles;
                break;
            case Operation.PayrunEmployeeTest:
                operationToggles = PayrunEmployeeTestArguments.Toggles;
                break;

            // statistics
            case Operation.PayrunStatistics:
                operationToggles = PayrunStatisticsArguments.Toggles;
                break;

            // shared regulation regulations
            case Operation.RegulationShare:
                operationToggles = RegulationShareArguments.Toggles;
                break;

            // data management
            case Operation.TenantDelete:
                operationToggles = TenantDeleteArguments.Toggles;
                break;
            case Operation.PayrunJobDelete:
                operationToggles = PayrunJobDeleteArguments.Toggles;
                break;

            // scripting
            case Operation.RegulationRebuild:
                operationToggles = RegulationRebuildArguments.Toggles;
                break;
            case Operation.PayrunRebuild:
                operationToggles = PayrunRebuildArguments.Toggles;
                break;
            case Operation.ScriptPublish:
                operationToggles = ScriptPublishArguments.Toggles;
                break;
            case Operation.ScriptExport:
                operationToggles = ScriptExportArguments.Toggles;
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(operation), operation, "Unknown operation for toggle");
        }
        if (operationToggles != null)
        {
            toggles.AddRange(operationToggles);
        }
        // unknown toggle
        var unknownToggle = ConsoleArguments.TestUnknownToggles(toggles.ToArray());
        if (!string.IsNullOrWhiteSpace(unknownToggle))
        {
            return $"Unknown toggle {unknownToggle}";
        }
        // multiple toggle
        var multipleToggle = ConsoleArguments.TestMultipleToggles(toggles.ToArray());
        if (multipleToggle != null)
        {
            return $"Invalid toggles for {multipleToggle.Name}";
        }

        // mandatory arguments
        return operation switch
        {
            Operation.Help => null,
            Operation.UserVariable => UserVariableArguments.TestArguments(),
            Operation.Stopwatch => StopwatchArguments.TestArguments(),

            Operation.ActionReport => ActionReportArguments.TestArguments(),

            Operation.HttpGet => HttpGetArguments.TestArguments(),
            Operation.HttpPost => HttpPostArguments.TestArguments(),
            Operation.HttpPut => HttpPutArguments.TestArguments(),
            Operation.HttpDelete => HttpDeleteArguments.TestArguments(),
            Operation.LogTrail => LogTrailArguments.TestArguments(),

            Operation.PayrollResults => PayrollResultsArguments.TestArguments(),
            Operation.PayrollImport => PayrollImportArguments.TestArguments(),
            Operation.PayrollImportExcel => PayrollImportExcelArguments.TestArguments(),
            Operation.PayrollExport => PayrollExportArguments.TestArguments(),

            Operation.Report => ReportArguments.TestArguments(),
            Operation.DataReport => DataReportArguments.TestArguments(),

            Operation.CaseTest => CaseTestArguments.TestArguments(),
            Operation.ReportTest => ReportTestArguments.TestArguments(),
            Operation.PayrunTest => PayrunTestArguments.TestArguments(),
            Operation.PayrunEmployeeTest => PayrunEmployeeTestArguments.TestArguments(),

            Operation.PayrunStatistics => null,

            Operation.RegulationShare => null,

            Operation.TenantDelete => TenantDeleteArguments.TestArguments(),
            Operation.PayrunJobDelete => null,

            Operation.RegulationRebuild => RegulationRebuildArguments.TestArguments(),
            Operation.PayrunRebuild => PayrunRebuildArguments.TestArguments(),
            Operation.ScriptPublish => ScriptPublishArguments.TestArguments(),
            Operation.ScriptExport => ScriptExportArguments.TestArguments(),

            _ => throw new ArgumentOutOfRangeException(nameof(operation), operation, "Unknown operation for arguments test")
        };
    }
}