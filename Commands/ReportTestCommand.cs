using System;
using System.IO;
using System.Threading.Tasks;
using PayrollEngine.Client.Test;
using PayrollEngine.Serialization;
using PayrollEngine.Client.Command;
using PayrollEngine.Client.Test.Report;

namespace PayrollEngine.PayrollConsole.Commands;

/// <summary>
/// Report test command
/// </summary>
[Command("ReportTest")]
// ReSharper disable once UnusedType.Global
internal sealed class ReportTestCommand : TestCommandBase<ReportTestParameters>
{
    /// <summary>Test a report</summary>
    /// <param name="context">Command execution context</param>
    /// <param name="parameters">Command parameters</param>
    /// <returns>Program exit ok, 0 on success</returns>
    protected override async Task<int> Execute(CommandContext context, ReportTestParameters parameters)
    {
        if (string.IsNullOrWhiteSpace(parameters.FileMask))
        {
            throw new PayrollException($"Missing test file {parameters.FileMask}.");
        }

        // test files by mask
        var testFileNames = GetTestFileNames(parameters.FileMask);
        if (testFileNames.Count == 0)
        {
            throw new PayrollException($"Missing test file {parameters.FileMask}.");
        }

        try
        {
            foreach (var testFileName in testFileNames)
            {
                DisplayTitle(context.Console, $"Report test - {testFileName}");
                if (context.DisplayLevel == DisplayLevel.Full)
                {
                    context.Console.DisplayTextLine($"Path               {new FileInfo(testFileName).Directory?.FullName}");
                    context.Console.DisplayTextLine($"File               {testFileName}");
                    context.Console.DisplayTextLine($"Display mode       {parameters.DisplayMode}");
                    context.Console.DisplayTextLine($"Url                {context.HttpClient}");
                    context.Console.DisplayTextLine($"Test precision     {parameters.Precision.GetDecimals()}");
                }

                context.Console.DisplayNewLine();

                context.Console.DisplayTextLine("Running test...");

                // load test data
                var reportTest = await JsonSerializer.DeserializeFromFileAsync<ReportTest>(testFileName);
                if (reportTest == null)
                {
                    throw new PayrollException($"Invalid case test file {testFileName}.");
                }

                // run test
                var testRunner = new ReportTestRunner(context.HttpClient);
                var testResult = await testRunner.TestAsync(reportTest);

                // display test results
                context.Console.DisplayNewLine();
                DisplayTestResults(
                    logger: context.Logger,
                    console: context.Console,
                    displayMode: parameters.DisplayMode,
                    results: testResult.Results);

                // failed test
                if (testResult.IsFailed())
                {
                    return (int)ProgramExitCode.FailedTest;
                }
            }
            return (int)ProgramExitCode.Ok;
        }
        catch (Exception exception)
        {
            ProcessError(context.Console, exception);
            return (int)ProgramExitCode.GenericError;
        }
    }

    /// <inheritdoc />
    public override ICommandParameters GetParameters(CommandLineParser parser) =>
        ReportTestParameters.ParserFrom(parser);

    /// <inheritdoc />
    public override void ShowHelp(ICommandConsole console)
    {
        console.DisplayTitleLine("- ReportTest");
        console.DisplayTextLine("      Test report output data");
        console.DisplayTextLine("      Arguments:");
        console.DisplayTextLine("          1. JSON file name or file mask [FileMask]");
        console.DisplayTextLine("      Toggles:");
        console.DisplayTextLine("          test display mode: /showfailed or /showall (default: showfailed)");
        console.DisplayTextLine("          test precision: /TestPrecisionOff or /TestPrecision1 to /TestPrecision6 (default: /TestPrecision2)");
        console.DisplayTextLine("      Examples:");
        console.DisplayTextLine("          ReportTest *.rt.json");
        console.DisplayTextLine("          ReportTest Test.rt.json /showall");
        console.DisplayTextLine("          ReportTest Test.rt.json /TestPrecision3");
    }
}