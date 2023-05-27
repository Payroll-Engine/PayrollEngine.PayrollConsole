using System;
using System.Collections.Generic;
using PayrollEngine.Client;
using PayrollEngine.PayrollConsole.Shared;

namespace PayrollEngine.PayrollConsole.Arguments;

public static class ArgumentManager
{
    // fixed order arguments
    public static Shared.Command GetCommand(Shared.Command defaultCommand) =>
        ConsoleArguments.GetEnum(1, defaultCommand);

    // global enum toggles
    public static ConsoleDisplayMode DisplayMode(ConsoleDisplayMode defaultValue = default) =>
        ConsoleArguments.GetEnumToggle(defaultValue);

    public static ConsoleErrorMode ErrorMode(ConsoleErrorMode defaultValue = default) =>
        ConsoleArguments.GetEnumToggle(defaultValue);

    public static ObjectDeleteMode DeleteMode(ObjectDeleteMode defaultValue = default) =>
        ConsoleArguments.GetEnumToggle(defaultValue);

    public static PayrollConsoleWaitMode WaitMode(PayrollConsoleWaitMode defaultValue = default) =>
        ConsoleArguments.GetEnumToggle(defaultValue);

    public static string TestArguments(Shared.Command command)
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

        Type[] commandToggles = null;
        switch (command)
        {
            // common commands
            case Shared.Command.Help:
                break;
            case Shared.Command.UserVariable:
                commandToggles = UserVariableArguments.Toggles;
                break;
            case Shared.Command.Stopwatch:
                commandToggles = StopwatchArguments.Toggles;
                break;

            // action
            case Shared.Command.ActionReport:
                commandToggles = ActionReportArguments.Toggles;
                break;

            // system
            case Shared.Command.HttpGet:
                commandToggles = HttpGetArguments.Toggles;
                break;
            case Shared.Command.HttpPost:
                commandToggles = HttpPostArguments.Toggles;
                break;
            case Shared.Command.HttpPut:
                commandToggles = HttpPutArguments.Toggles;
                break;
            case Shared.Command.HttpDelete:
                commandToggles = HttpDeleteArguments.Toggles;
                break;
            case Shared.Command.LogTrail:
                commandToggles = LogTrailArguments.Toggles;
                break;

            // payroll
            case Shared.Command.PayrollResults:
                commandToggles = PayrollResultsArguments.Toggles;
                break;
            case Shared.Command.PayrollImport:
                commandToggles = PayrollImportArguments.Toggles;
                break;
            case Shared.Command.PayrollImportExcel:
                commandToggles = PayrollImportExcelArguments.Toggles;
                break;
            case Shared.Command.PayrollExport:
                commandToggles = PayrollExportArguments.Toggles;
                break;

            // report
            case Shared.Command.Report:
                commandToggles = ReportArguments.Toggles;
                break;
            case Shared.Command.DataReport:
                commandToggles = DataReportArguments.Toggles;
                break;

            // test
            case Shared.Command.CaseTest:
                commandToggles = CaseTestArguments.Toggles;
                break;
            case Shared.Command.ReportTest:
                commandToggles = ReportTestArguments.Toggles;
                break;
            case Shared.Command.PayrunTest:
                commandToggles = PayrunTestArguments.Toggles;
                break;
            case Shared.Command.PayrunEmployeeTest:
                commandToggles = PayrunEmployeeTestArguments.Toggles;
                break;

            // statistics
            case Shared.Command.PayrunStatistics:
                commandToggles = PayrunStatisticsArguments.Toggles;
                break;

            // shared regulation regulations
            case Shared.Command.RegulationShare:
                commandToggles = RegulationShareArguments.Toggles;
                break;

            // data management
            case Shared.Command.TenantDelete:
                commandToggles = TenantDeleteArguments.Toggles;
                break;
            case Shared.Command.PayrunJobDelete:
                commandToggles = PayrunJobDeleteArguments.Toggles;
                break;

            // scripting
            case Shared.Command.RegulationRebuild:
                commandToggles = RegulationRebuildArguments.Toggles;
                break;
            case Shared.Command.PayrunRebuild:
                commandToggles = PayrunRebuildArguments.Toggles;
                break;
            case Shared.Command.ScriptPublish:
                commandToggles = ScriptPublishArguments.Toggles;
                break;
            case Shared.Command.ScriptExport:
                commandToggles = ScriptExportArguments.Toggles;
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(command), command, "Unknown command for toggle");
        }
        if (commandToggles != null)
        {
            toggles.AddRange(commandToggles);
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
        return command switch
        {
            Shared.Command.Help => null,
            Shared.Command.UserVariable => UserVariableArguments.TestArguments(),
            Shared.Command.Stopwatch => StopwatchArguments.TestArguments(),

            Shared.Command.ActionReport => ActionReportArguments.TestArguments(),

            Shared.Command.HttpGet => HttpGetArguments.TestArguments(),
            Shared.Command.HttpPost => HttpPostArguments.TestArguments(),
            Shared.Command.HttpPut => HttpPutArguments.TestArguments(),
            Shared.Command.HttpDelete => HttpDeleteArguments.TestArguments(),
            Shared.Command.LogTrail => LogTrailArguments.TestArguments(),

            Shared.Command.PayrollResults => PayrollResultsArguments.TestArguments(),
            Shared.Command.PayrollImport => PayrollImportArguments.TestArguments(),
            Shared.Command.PayrollImportExcel => PayrollImportExcelArguments.TestArguments(),
            Shared.Command.PayrollExport => PayrollExportArguments.TestArguments(),

            Shared.Command.Report => ReportArguments.TestArguments(),
            Shared.Command.DataReport => DataReportArguments.TestArguments(),

            Shared.Command.CaseTest => CaseTestArguments.TestArguments(),
            Shared.Command.ReportTest => ReportTestArguments.TestArguments(),
            Shared.Command.PayrunTest => PayrunTestArguments.TestArguments(),
            Shared.Command.PayrunEmployeeTest => PayrunEmployeeTestArguments.TestArguments(),

            Shared.Command.PayrunStatistics => null,

            Shared.Command.RegulationShare => null,

            Shared.Command.TenantDelete => TenantDeleteArguments.TestArguments(),
            Shared.Command.PayrunJobDelete => null,

            Shared.Command.RegulationRebuild => RegulationRebuildArguments.TestArguments(),
            Shared.Command.PayrunRebuild => PayrunRebuildArguments.TestArguments(),
            Shared.Command.ScriptPublish => ScriptPublishArguments.TestArguments(),
            Shared.Command.ScriptExport => ScriptExportArguments.TestArguments(),

            _ => throw new ArgumentOutOfRangeException(nameof(command), command, "Unknown command for arguments test")
        };
    }
}