﻿using System;
using PayrollEngine.PayrollConsole.Shared;

namespace PayrollEngine.PayrollConsole.Command;

internal sealed class ConsoleHelpCommand : CommandBase
{
    /// <summary>
    /// Show the application help
    /// </summary>
    internal static void ShowHelp()
    {
        ConsoleTool.DisplayNewLine();
        ConsoleTool.DisplayTextLine("Usage:");
        ConsoleTool.DisplayTextLine("  PayrollConsole Command [Arg1]...[argX] [/Toggle]");
        ConsoleTool.DisplayTextLine("  PayrollConsole Command [Arg1Name:Arg1Value]...[ArgXName:ArgXValue] [/Toggle]");
        ConsoleTool.DisplayNewLine();
        ConsoleTool.DisplayTextLine("Commands:");

        // common commands
        ConsoleTool.DisplayTitleLine("- Help (this screen)");
        UserVariableCommand.ShowHelp();
        StopwatchCommand.ShowHelp();

        // action
        ActionReportCommand.ShowHelp();

        // system commands
        HttpRequestCommand.ShowGetRequestHelp();
        HttpRequestCommand.ShowPostRequestHelp();
        HttpRequestCommand.ShowPutRequestHelp();
        HttpRequestCommand.ShowDeleteRequestHelp();
        LogTrailCommand.ShowHelp();

        // payroll commands
        PayrollResultsCommand.ShowHelp();
        PayrollImportCommand.ShowHelp();
        PayrollImportExcelCommand.ShowHelp();
        PayrollExportCommand.ShowHelp();

        // report
        ReportCommand.ShowHelp();
        DataReportCommand.ShowHelp();

        // test commands
        CaseTestCommand.ShowHelp();
        ReportTestCommand.ShowHelp();
        PayrunTestCommand.ShowHelp();
        PayrunEmployeeTestCommand.ShowHelp();

        // statistics commands
        PayrunStatisticsCommand.ShowHelp();

        // shared regulation regulations commands
        RegulationShareCommand.ShowHelp();

        // data management commands
        TenantDeleteCommand.ShowHelp();
        PayrunJobDeleteCommand.ShowHelp();

        // user commands
        ChangePasswordCommand.ShowHelp();

        // scripting commands
        RegulationRebuildCommand.ShowHelp();
        PayrunRebuildCommand.ShowHelp();
        ScriptPublishCommand.ShowHelp();
        ScriptExportCommand.ShowHelp();

        // command toggles
        ConsoleTool.DisplayNewLine();
        ConsoleTool.DisplayTitleLine("- Global toggles");
        ConsoleTool.DisplayTextLine("      display mode /show or /silent: show text on screen (default: /show)");
        ConsoleTool.DisplayTextLine("      error mode: /errors or /noerrors: show failed test and error (default: /errors)");
        ConsoleTool.DisplayTextLine("      wait mode: /waiterror, /wait or /nowait: wait at the program end (default: /waiterror)");

        // program exit codes
        ConsoleTool.DisplayNewLine();
        ConsoleTool.DisplayTitleLine("- Exit codes");
        foreach (var value in Enum.GetValues<ProgramExitCode>())
        {
            ConsoleTool.DisplayTextLine($"{(int)value}     {Enum.GetName(typeof(ProgramExitCode), value)}");
        }
    }
}