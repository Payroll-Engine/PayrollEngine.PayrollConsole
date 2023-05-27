﻿using System;
using System.IO;
using System.Threading.Tasks;
using PayrollEngine.Client;
using PayrollEngine.Client.Test;
using PayrollEngine.Client.Test.Report;
using PayrollEngine.PayrollConsole.Shared;

namespace PayrollEngine.PayrollConsole.Command;

internal sealed class ReportTestCommand : TestCommandBase
{
    internal ReportTestCommand(PayrollHttpClient httpClient, TestPrecision testPrecision) :
        base(httpClient, testPrecision)
    {
    }

    /// <summary>
    /// Test a report
    /// </summary>
    /// <param name="fileMask">The test file mask</param>
    /// <param name="displayMode">Show results</param>
    /// <returns>True if no test failed</returns>
    internal async Task<ProgramExitCode> TestAsync(string fileMask, TestDisplayMode displayMode)
    {
        if (string.IsNullOrWhiteSpace(fileMask))
        {
            throw new PayrollException($"Missing test file {fileMask}");
        }

        // test files by mask
        var testFileNames = GetTestFileNames(fileMask);
        if (testFileNames.Count == 0)
        {
            throw new PayrollException($"Missing test file {fileMask}");
        }

        try
        {
            foreach (var testFileName in testFileNames)
            {
                DisplayTitle("Test report");
                ConsoleTool.DisplayTextLine($"Path               {Path.GetFileName(new FileInfo(testFileName).DirectoryName)}");
                ConsoleTool.DisplayTextLine($"File               {testFileName}");
                ConsoleTool.DisplayTextLine($"Display mode       {displayMode}");
                ConsoleTool.DisplayTextLine($"Url                {HttpClient}");
                ConsoleTool.DisplayTextLine($"Test precision     {TestPrecision.GetDecimals()}");
                ConsoleTool.DisplayNewLine();

                ConsoleTool.DisplayTextLine("Running test...");

                // run test
                var testRunner = new ReportTestRunner(HttpClient, testFileName);
                var testResult = await testRunner.TestAsync();
                DisplayTestResults(testFileName, displayMode, testResult.Results);

                // failed test
                if (testResult.IsFailed())
                {
                    return ProgramExitCode.FailedTest;
                }
            }
            return ProgramExitCode.Ok;
        }
        catch (Exception exception)
        {
            ProcessError(exception);
            return ProgramExitCode.GenericError;
        }
    }

    internal static void ShowHelp()
    {
        ConsoleTool.DisplayTitleLine("- ReportTest");
        ConsoleTool.DisplayTextLine("      Test a report");
        ConsoleTool.DisplayTextLine("      Arguments:");
        ConsoleTool.DisplayTextLine("          1. JSON file name or file mask");
        ConsoleTool.DisplayTextLine("      Toggles:");
        ConsoleTool.DisplayTextLine("          test display mode: /showfailed or /showall (default: showfailed)");
        ConsoleTool.DisplayTextLine("          test precision: /TestPrecisionOff or /TestPrecision1 to /TestPrecision6 (default: /TestPrecision2)");
        ConsoleTool.DisplayTextLine("      Examples:");
        ConsoleTool.DisplayTextLine("          ReportTest *.rt.json");
        ConsoleTool.DisplayTextLine("          ReportTest Test.rt.json /showall");
        ConsoleTool.DisplayTextLine("          ReportTest Test.rt.json /TestPrecision3");
    }
}