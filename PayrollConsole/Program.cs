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

    /// <summary>Mandatory argument: command</summary>
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

        // command
        var command = ArgumentManager.GetCommand(Shared.Command.Help);
        // test command arguments
        var argumentError = ArgumentManager.TestArguments(command);
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
            var command = ArgumentManager.GetCommand(Shared.Command.Help);
            switch (command)
            {
                case Shared.Command.Help:
                case Shared.Command.UserVariable:
                case Shared.Command.Stopwatch:
                case Shared.Command.ActionReport:
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

        // execution command
        var command = ArgumentManager.GetCommand(Shared.Command.Help);
        if (command == Shared.Command.Help)
        {
            var firstArgument = ConsoleArguments.Get(1);
            if (!string.Equals(firstArgument, nameof(Shared.Command.Help), StringComparison.InvariantCultureIgnoreCase))
            {
                WriteErrorLine($"Unknown command {firstArgument}");
                PressAnyKey();
                return;
            }
        }

        // test command arguments
        var argumentError = ArgumentManager.TestArguments(command);
        if (!string.IsNullOrWhiteSpace(argumentError))
        {
            WriteErrorLine($"Argument error: {argumentError}");
            PressAnyKey();
            return;
        }

        ProgramExitCode exitCode;

        // command help (without backend connection)
        switch (command)
        {
            // common commands
            case Shared.Command.Help:
                // handled by base class
                ProgramEnd(ProgramExitCode.Ok);
                return;
            case Shared.Command.UserVariable:
                exitCode = await new UserVariableCommand().ProcessVariableAsync(
                    UserVariableArguments.VariableName,
                    UserVariableArguments.VariableValue,
                    UserVariableArguments.VariableMode());
                ProgramEnd(exitCode);
                return;
            case Shared.Command.Stopwatch:
                exitCode = StopwatchCommand.Stopwatch(
                    StopwatchArguments.VariableName,
                    StopwatchArguments.StopwatchMode());
                ProgramEnd(exitCode);
                return;

            // action
            case Shared.Command.ActionReport:
                exitCode = await new ActionReportCommand().ReportAsync(
                    ActionReportArguments.FileName);
                ProgramEnd(exitCode);
                return;
        }

        var failedCommand = false;
        // API operations
        switch (command)
        {
            // system
            case Shared.Command.HttpGet:
                exitCode = await new HttpRequestCommand(HttpClient).GetRequestAsync(
                    HttpGetArguments.Url);
                break;
            case Shared.Command.HttpPost:
                exitCode = await new HttpRequestCommand(HttpClient).PostRequestAsync(
                    HttpPostArguments.Url,
                    HttpPostArguments.FileName);
                break;
            case Shared.Command.HttpPut:
                exitCode = await new HttpRequestCommand(HttpClient).PutRequestAsync(
                    HttpPutArguments.Url,
                    HttpPutArguments.FileName);
                break;
            case Shared.Command.HttpDelete:
                exitCode = await new HttpRequestCommand(HttpClient).DeleteRequestAsync(
                    HttpDeleteArguments.Url);
                break;

            case Shared.Command.LogTrail:
                exitCode = await new LogTrailCommand(HttpClient).StartLogTrailAsync(
                    LogTrailArguments.Tenant,
                    LogTrailArguments.Interval());
                break;

            // payroll
            case Shared.Command.PayrollResults:
                exitCode = await new PayrollResultsCommand(HttpClient).CreateReportAsync(
                    PayrollResultsArguments.Tenant,
                    PayrollResultsArguments.TopFilter(),
                    PayrollResultsArguments.ResultExportMode());
                break;
            case Shared.Command.PayrollImport:
                exitCode = await new PayrollImportCommand(HttpClient).ImportAsync(
                    PayrollImportArguments.SourceFileName,
                    PayrollImportArguments.DataImportMode(),
                    PayrollImportArguments.OptionsFileName,
                    PayrollImportArguments.Namespace);
                break;
            case Shared.Command.PayrollImportExcel:
                exitCode = await new PayrollImportExcelCommand(HttpClient).ImportAsync(
                    PayrollImportExcelArguments.FileName,
                    PayrollImportExcelArguments.DataImportMode(),
                    PayrollImportExcelArguments.Tenant);
                break;
            case Shared.Command.PayrollExport:
                exitCode = await new PayrollExportCommand(HttpClient).ExportAsync(
                    PayrollExportArguments.Tenant,
                    PayrollExportArguments.TargetFileName,
                    PayrollExportArguments.OptionsFileName,
                    PayrollExportArguments.Namespace);
                break;

            // report
            case Shared.Command.Report:
                exitCode = await new ReportCommand(HttpClient).ReportAsync(new()
                {
                    TenantIdentifier = ReportArguments.Tenant,
                    UserIdentifier = ReportArguments.User,
                    RegulationName = ReportArguments.Regulation,
                    ReportName = ReportArguments.Report,
                    DocumentType = ReportArguments.DocumentType(),
                    Culture = ReportArguments.Culture,
                    PostAction = ReportArguments.PostAction(),
                    ParameterFile = ReportArguments.ParameterFile,
                    TargetFile = ReportArguments.TargetFile
                });
                break;
            case Shared.Command.DataReport:
                exitCode = await new DataReportCommand(HttpClient).ReportAsync(new()
                {
                    OutputFile = DataReportArguments.OutputFile,
                    TenantIdentifier = DataReportArguments.Tenant,
                    UserIdentifier = DataReportArguments.User,
                    RegulationName = DataReportArguments.Regulation,
                    ReportName = DataReportArguments.Report,
                    Culture = DataReportArguments.Culture,
                    PostAction = DataReportArguments.PostAction(),
                    ParameterFile = ReportArguments.ParameterFile
                });
                break;

            // test
            case Shared.Command.CaseTest:
                exitCode = await new CaseTestCommand(HttpClient,
                    CaseTestArguments.TestPrecision()).TestAsync(
                    CaseTestArguments.FileMask,
                    CaseTestArguments.TestDisplayMode());
                failedCommand = exitCode == ProgramExitCode.FailedTest;
                break;
            case Shared.Command.ReportTest:
                exitCode = await new ReportTestCommand(HttpClient,
                    ReportTestArguments.TestPrecision()).TestAsync(
                    ReportTestArguments.FileMask,
                    ReportTestArguments.TestDisplayMode());
                failedCommand = exitCode == ProgramExitCode.FailedTest;
                break;
            case Shared.Command.PayrunTest:
                exitCode = await new PayrunTestCommand(HttpClient,
                    PayrunTestArguments.TestPrecision()).TestAsync(new()
                    {
                        FileMask = PayrunTestArguments.FileMask,
                        ImportMode = PayrunTestArguments.DataImportMode(),
                        DisplayMode = PayrunTestArguments.TestDisplayMode(),
                        ResultMode = PayrunTestArguments.TestResultMode(),
                        Namespace = PayrunTestArguments.Namespace,
                        Owner = PayrunTestArguments.Owner
                    });
                failedCommand = exitCode == ProgramExitCode.FailedTest;
                break;
            case Shared.Command.PayrunEmployeeTest:
                exitCode = await new PayrunEmployeeTestCommand(HttpClient,
                    PayrunEmployeeTestArguments.TestPrecision()).TestAsync(new()
                    {
                        FileMask = PayrunEmployeeTestArguments.FileMask,
                        DisplayMode = PayrunEmployeeTestArguments.TestDisplayMode(),
                        TestMode = PayrunEmployeeTestArguments.EmployeeTestMode(),
                        Namespace = PayrunEmployeeTestArguments.Namespace,
                        Owner = PayrunEmployeeTestArguments.Owner
                    });
                failedCommand = exitCode == ProgramExitCode.FailedTest;
                break;

            // statistics
            case Shared.Command.PayrunStatistics:
                exitCode = await new PayrunStatisticsCommand(HttpClient).PayrunStatisticsAsync(
                    PayrunStatisticsArguments.Tenant,
                    PayrunStatisticsArguments.CreatedSinceMinutes());
                failedCommand = exitCode == ProgramExitCode.FailedTest;
                break;

            // regulation shares
            case Shared.Command.RegulationShare:
                exitCode = await new RegulationShareCommand(HttpClient).ChangeAsync(new()
                {
                    ProviderTenant = RegulationShareArguments.ProviderTenant,
                    ProviderRegulation = RegulationShareArguments.ProviderRegulation,
                    ConsumerTenant = RegulationShareArguments.ConsumerTenant,
                    ConsumerDivision = RegulationShareArguments.ConsumerDivision,
                    ShareMode = RegulationShareArguments.ShareMode()
                });
                break;

            // data management
            case Shared.Command.TenantDelete:
                exitCode = await new TenantDeleteCommand(HttpClient).DeleteAsync(
                    TenantDeleteArguments.Tenant,
                    TenantDeleteArguments.ObjectDeleteMode());
                break;
            case Shared.Command.PayrunJobDelete:
                exitCode = await new PayrunJobDeleteCommand(HttpClient).DeleteAsync(
                    PayrunJobDeleteArguments.Tenant);
                break;

            // user
            case Shared.Command.ChangePassword:
                exitCode = await new ChangePasswordCommand(HttpClient).ChangeAsync(
                    ChangePasswordArguments.Tenant,
                    ChangePasswordArguments.User,
                    ChangePasswordArguments.NewPassword,
                    ChangePasswordArguments.ExistingPassword);
                break;

            // scripting
            case Shared.Command.RegulationRebuild:
                exitCode = await new RegulationRebuildCommand(HttpClient).RebuildAsync(
                    RegulationRebuildArguments.Tenant,
                    RegulationRebuildArguments.RegulationName,
                    RegulationRebuildArguments.ScriptObject,
                    RegulationRebuildArguments.ObjectKey);
                break;
            case Shared.Command.PayrunRebuild:
                exitCode = await new PayrunRebuildCommand(HttpClient).RebuildAsync(
                    PayrunRebuildArguments.Tenant,
                    PayrunRebuildArguments.PayrunName);
                break;

            case Shared.Command.ScriptPublish:
                exitCode = await new ScriptPublishCommand(HttpClient).PublishAsync(
                    ScriptPublishArguments.SourceFile,
                    ScriptPublishArguments.SourceScript);
                break;
            case Shared.Command.ScriptExport:
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
                throw new PayrollException($"Unknown command: {command}");
        }

        ProgramEnd(exitCode, failedCommand);
    }

    /// <summary>Show the help screen</summary>
    protected override Task HelpAsync()
    {
        ConsoleHelpCommand.ShowHelp();
        return base.HelpAsync();
    }

    private static void ProgramEnd(ProgramExitCode exitCode, bool failedCommand = false)
    {
        // enforced wait
        SetExitCode(exitCode);
        if (ConsoleTool.WaitMode == PayrollConsoleWaitMode.Wait ||
            // system error
            (exitCode != ProgramExitCode.Ok && ConsoleTool.WaitMode != PayrollConsoleWaitMode.NoWait) ||
            // failed command
            (ConsoleTool.WaitMode == PayrollConsoleWaitMode.WaitError && failedCommand))
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