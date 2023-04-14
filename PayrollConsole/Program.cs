using System;
using System.Net;
using System.Threading.Tasks;
using PayrollEngine.Client;
using PayrollEngine.PayrollConsole.Arguments;
using PayrollEngine.PayrollConsole.Command;
using PayrollEngine.PayrollConsole.Shared;
using PayrollEngine.Serilog;

namespace PayrollEngine.PayrollConsole;

sealed class Program : ConsoleProgram<Program>
{
    /// <inheritdoc />
    protected override bool LogLifecycle => false;

    /// <summary>Mandatory argument: operation</summary>
    protected override int MandatoryArgumentCount => 1;

    /// <inheritdoc />
    protected override Task SetupLogAsync()
    {
        // logger setup
        Configuration.Configuration.SetupSerilog();
        return base.SetupLogAsync();
    }

    /// <inheritdoc />
    protected override async Task<bool> InitializeAsync()
    {
        // security
        // error An existing connection was forcibly closed by the remote host
        // https://www.thetechminute.com/existing-connection-was-forcibly-closed-by-the-remote-host/
        ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12 | SecurityProtocolType.Tls11 | SecurityProtocolType.Tls;

        // operation
        var operation = ArgumentManager.GetOperation(Operation.Help);
        // test operation arguments
        var argumentError = ArgumentManager.TestArguments(operation);
        if (!string.IsNullOrWhiteSpace(argumentError))
        {
            WriteErrorLine($"Argument error: {argumentError}");
            PressAnyKey();
            return false;
        }

        return await base.InitializeAsync();
    }

    /// <inheritdoc />
    protected override bool UseHttpClient
    {
        get
        {
            var operation = ArgumentManager.GetOperation(Operation.Help);
            switch (operation)
            {
                case Operation.Help:
                case Operation.UserVariable:
                case Operation.Stopwatch:
                case Operation.ActionReport:
                    return false;
            }
            return base.UseHttpClient;
        }
    }

    /// <inheritdoc />
    protected override async Task RunAsync()
    {
        // global toggles
        ConsoleTool.DisplayMode = ArgumentManager.DisplayMode();
        ConsoleTool.ErrorMode = ArgumentManager.ErrorMode();
        ConsoleTool.WaitMode = ArgumentManager.WaitMode();

        // execution operation
        var operation = ArgumentManager.GetOperation(Operation.Help);
        // test operation arguments
        var argumentError = ArgumentManager.TestArguments(operation);
        if (!string.IsNullOrWhiteSpace(argumentError))
        {
            WriteErrorLine($"Argument error: {argumentError}");
            PressAnyKey();
            return;
        }

        ProgramExitCode exitCode;

        // operation help (without backend connection)
        switch (operation)
        {
            // common operations
            case Operation.Help:
                // handled by base class
                ProgramEnd(ProgramExitCode.Ok);
                return;
            case Operation.UserVariable:
                exitCode = new UserVariableCommand().ProcessVariable(
                    UserVariableArguments.VariableName,
                    UserVariableArguments.VariableValue,
                    UserVariableArguments.VariableMode());
                ProgramEnd(exitCode);
                return;
            case Operation.Stopwatch:
                exitCode = new StopwatchCommand().Stopwatch(
                    StopwatchArguments.VariableName,
                    StopwatchArguments.StopwatchMode());
                ProgramEnd(exitCode);
                return;

            // action
            case Operation.ActionReport:
                exitCode = await new ActionReportCommand().ReportAsync(
                    ActionReportArguments.FileName);
                ProgramEnd(exitCode);
                return;
        }

        var failedOperation = false;
        // API operations
        switch (operation)
        {
            // system
            case Operation.HttpGet:
                exitCode = await new HttpRequestCommand(HttpClient).GetRequestAsync(
                    HttpGetArguments.Url);
                break;
            case Operation.HttpPost:
                exitCode = await new HttpRequestCommand(HttpClient).PostRequestAsync(
                    HttpPostArguments.Url,
                    HttpPostArguments.FileName);
                break;
            case Operation.HttpPut:
                exitCode = await new HttpRequestCommand(HttpClient).PutRequestAsync(
                    HttpPutArguments.Url,
                    HttpPutArguments.FileName);
                break;
            case Operation.HttpDelete:
                exitCode = await new HttpRequestCommand(HttpClient).DeleteRequestAsync(
                    HttpDeleteArguments.Url);
                break;

            case Operation.LogTrail:
                exitCode = await new LogTrailCommand(HttpClient).StartLogTrailAsync(
                    LogTrailArguments.Tenant,
                    LogTrailArguments.Interval());
                break;

            // payroll
            case Operation.PayrollResults:
                exitCode = await new PayrollResultsCommand(HttpClient).CreateReportAsync(
                    PayrollResultsArguments.Tenant,
                    PayrollResultsArguments.TopFilter(),
                    PayrollResultsArguments.ResultExportMode());
                break;
            case Operation.PayrollImport:
                exitCode = await new PayrollImportCommand(HttpClient).ImportAsync(
                    PayrollImportArguments.FileName,
                    PayrollImportArguments.DataImportMode(),
                    PayrollImportArguments.Namespace);
                break;
            case Operation.PayrollImportExcel:
                exitCode = await new PayrollImportExcelCommand(HttpClient).ImportAsync(
                    PayrollImportExcelArguments.FileName,
                    PayrollImportExcelArguments.DataImportMode(),
                    PayrollImportExcelArguments.Tenant);
                break;
            case Operation.PayrollExport:
                exitCode = await new PayrollExportCommand(HttpClient).ExportAsync(
                    PayrollExportArguments.Tenant,
                    PayrollExportArguments.FileName,
                    PayrollExportArguments.ResultExportMode(),
                    PayrollExportArguments.Namespace);
                break;

            // report
            case Operation.Report:
                exitCode = await new ReportCommand(HttpClient).ReportAsync(
                    ReportArguments.Tenant,
                    ReportArguments.User,
                    ReportArguments.Regulation,
                    ReportArguments.Report,
                    ReportArguments.DocumentType(),
                    ReportArguments.Language());
                break;
            case Operation.DataReport:
                exitCode = await new DataReportCommand(HttpClient).ReportAsync(
                    DataReportArguments.OutputFile,
                    DataReportArguments.Tenant,
                    DataReportArguments.User,
                    DataReportArguments.Regulation,
                    DataReportArguments.Report,
                    DataReportArguments.Language,
                    DataReportArguments.ParametersFile);
                break;

            // test
            case Operation.CaseTest:
                exitCode = await new CaseTestCommand(HttpClient,
                    CaseTestArguments.TestPrecision()).TestAsync(
                    CaseTestArguments.FileMask,
                    CaseTestArguments.TestDisplayMode());
                failedOperation = exitCode == ProgramExitCode.FailedTest;
                break;
            case Operation.ReportTest:
                exitCode = await new ReportTestCommand(HttpClient,
                    ReportTestArguments.TestPrecision()).TestAsync(
                    ReportTestArguments.FileMask,
                    ReportTestArguments.TestDisplayMode());
                failedOperation = exitCode == ProgramExitCode.FailedTest;
                break;
            case Operation.PayrunTest:
                exitCode = await new PayrunTestCommand(HttpClient,
                    PayrunTestArguments.TestPrecision()).TestAsync(
                    PayrunTestArguments.FileMask,
                    PayrunTestArguments.DataImportMode(),
                    PayrunTestArguments.TestDisplayMode(),
                    PayrunTestArguments.TestResultMode(),
                    PayrunTestArguments.Namespace,
                    PayrunTestArguments.Owner);
                failedOperation = exitCode == ProgramExitCode.FailedTest;
                break;
            case Operation.PayrunEmployeeTest:
                exitCode = await new PayrunEmployeeTestCommand(HttpClient,
                    PayrunEmployeeTestArguments.TestPrecision()).TestAsync(
                    PayrunEmployeeTestArguments.FileMask,
                    PayrunEmployeeTestArguments.TestDisplayMode(),
                    PayrunEmployeeTestArguments.EmployeeTestMode(),
                    PayrunEmployeeTestArguments.Namespace,
                    PayrunEmployeeTestArguments.Owner);
                failedOperation = exitCode == ProgramExitCode.FailedTest;
                break;

            // statistics
            case Operation.PayrunStatistics:
                exitCode = await new PayrunStatisticsCommand(HttpClient).PayrunStatisticsAsync(
                    PayrunStatisticsArguments.Tenant,
                    PayrunStatisticsArguments.CreatedSinceMinutes());
                failedOperation = exitCode == ProgramExitCode.FailedTest;
                break;

            // shared regulation permission
            case Operation.RegulationPermission:
                exitCode = await new RegulationPermissionCommand(HttpClient).ChangeAsync(
                    RegulationPermissionArguments.Tenant,
                    RegulationPermissionArguments.Regulation,
                    RegulationPermissionArguments.PermissionTenant,
                    RegulationPermissionArguments.PermissionDivision,
                    RegulationPermissionArguments.PermissionMode());
                break;

            // data management
            case Operation.TenantDelete:
                exitCode = await new TenantDeleteCommand(HttpClient).DeleteAsync(
                    TenantDeleteArguments.Tenant,
                    TenantDeleteArguments.ObjectDeleteMode());
                break;
            case Operation.PayrunJobDelete:
                exitCode = await new PayrunJobDeleteCommand(HttpClient).DeleteAsync(
                    PayrunJobDeleteArguments.Tenant);
                break;

            // scripting
            case Operation.RegulationRebuild:
                exitCode = await new RegulationRebuildCommand(HttpClient).RebuildAsync(
                    RegulationRebuildArguments.Tenant,
                    RegulationRebuildArguments.RegulationName,
                    RegulationRebuildArguments.ScriptObject,
                    RegulationRebuildArguments.ObjectKey);
                break;
            case Operation.PayrunRebuild:
                exitCode = await new PayrunRebuildCommand(HttpClient).RebuildAsync(
                    PayrunRebuildArguments.Tenant,
                    PayrunRebuildArguments.PayrunName);
                break;

            case Operation.ScriptPublish:
                exitCode = await new ScriptPublishCommand(HttpClient).PublishAsync(
                    ScriptPublishArguments.SourceFile,
                    ScriptPublishArguments.SourceScript);
                break;
            case Operation.ScriptExport:
                exitCode = await new ScriptExportCommand(HttpClient).ExportAsync(
                    new()
                    {
                        TargetFolder = ScriptExportArguments.TargetFolder,
                        TenantIdentifier = ScriptExportArguments.Tenant,
                        UserIdentifier = ScriptExportArguments.User,
                        EmployeeIdentifier = ScriptExportArguments.Employee,
                        PayrollName = ScriptExportArguments.Payroll,
                        RegulationName = ScriptExportArguments.Regulation,
                        ScriptExportMode = ScriptExportArguments.ScriptExportMode(),
                        ScriptObject = ScriptExportArguments.ScriptObject(),
                        Namespace = ScriptExportArguments.Namespace
                    });
                break;

            default:
                throw new PayrollException($"Unknown operation: {operation}");
        }

        ProgramEnd(exitCode, failedOperation);
    }

    /// <summary>Show the help screen</summary>
    protected override Task HelpAsync()
    {
        ConsoleHelpCommand.ShowHelp();
        return base.HelpAsync();
    }

    private static void ProgramEnd(ProgramExitCode exitCode, bool failedOperation = false)
    {
        // enforced wait
        SetExitCode(exitCode);
        if (ConsoleTool.WaitMode == PayrollConsoleWaitMode.Wait ||
            // system error
            (exitCode != ProgramExitCode.Ok && ConsoleTool.WaitMode != PayrollConsoleWaitMode.NoWait) ||
            // failed operation
            (ConsoleTool.WaitMode == PayrollConsoleWaitMode.WaitError && failedOperation))
        {
            PressAnyKey();
        }
        else if (exitCode != ProgramExitCode.Ok)
        {
            ConsoleTool.DisplayInfoLine($"done with exit code #{ExitCode} ({ExitCode}).");
        }
        ConsoleTool.DisplayNewLine();
    }

    protected override Task NotifyGlobalErrorAsync(Exception exception)
    {
        SetExitCode(ProgramExitCode.GenericError);
        return base.NotifyGlobalErrorAsync(exception);
    }

    protected override Task NotifyConnectionErrorAsync()
    {
        SetExitCode(ProgramExitCode.ConnectionError);
        return base.NotifyConnectionErrorAsync();
    }

    protected override Task<string> GetProgramCultureAsync()
    {
        var reportConfiguration = Configuration.GetConfiguration<PayrollConsoleConfiguration>();
        var culture = reportConfiguration?.StartupCulture;
        if (!string.IsNullOrWhiteSpace(culture))
        {
            return Task.FromResult(culture);
        }
        return base.GetProgramCultureAsync();
    }

    private static void SetExitCode(ProgramExitCode exitCode)
    {
        ExitCode = (int)exitCode;
    }

    static async Task Main()
    {
        using var program = new Program();
        await program.ExecuteAsync();
    }
}