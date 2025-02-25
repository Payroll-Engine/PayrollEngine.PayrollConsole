using System;
using System.IO;
using System.Threading.Tasks;
using PayrollEngine.Client.Command;
using PayrollEngine.Client.Test;
using PayrollEngine.Serialization;
using PayrollEngine.Client.Test.Case;

namespace PayrollEngine.PayrollConsole.Commands;

/// <summary>
/// Case test command
/// </summary>
[Command("CaseTest")]
// ReSharper disable once UnusedType.Global
internal sealed class CaseTestCommand : TestCommandBase<CaseTestParameters>
{
    /// <summary>Test a case</summary>
    /// <param name="context">Command execution context</param>
    /// <param name="parameters">Command parameters</param>
    /// <returns>Program exit ok, 0 on success</returns>
    protected override async Task<int> Execute(CommandContext context, CaseTestParameters parameters)
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
                DisplayTitle(context.Console, $"Case test - {testFileName}");
                if (context.DisplayLevel == DisplayLevel.Full)
                {
                    context.Console.DisplayTextLine($"Path               {new FileInfo(testFileName).Directory?.FullName}");
                    context.Console.DisplayTextLine($"File               {testFileName}");
                    context.Console.DisplayTextLine($"Display mode       {parameters.DisplayMode}");
                    context.Console.DisplayTextLine($"Url                {context.HttpClient}");
                    context.Console.DisplayTextLine($"Test precision     {parameters.Precision.GetDecimals()}");
                    context.Console.DisplayNewLine();

                    context.Console.DisplayTextLine("Running test...");
                }

                // load test data
                var caseTest = await JsonSerializer.DeserializeFromFileAsync<CaseTest>(testFileName);
                if (caseTest == null)
                {
                    throw new PayrollException($"Invalid case test file {testFileName}.");
                }

                // run test
                var testResult = await new CaseTestRunner(context.HttpClient).TestAsync(caseTest);
                context.Console.DisplayNewLine();

                // display test results
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
        CaseTestParameters.ParserFrom(parser);

    /// <inheritdoc />
    public override void ShowHelp(ICommandConsole console)
    {
        console.DisplayTitleLine("- CaseTest");
        console.DisplayTextLine("      Test case availability, build data and user input validation");
        console.DisplayTextLine("      Arguments:");
        console.DisplayTextLine("          1. JSON file name or file mask [FileMask]");
        console.DisplayTextLine("      Toggles:");
        console.DisplayTextLine("          test display mode: /showfailed or /showall (default: showfailed)");
        console.DisplayTextLine("          test precision: /TestPrecisionOff or /TestPrecision1 to /TestPrecision6 (default: /TestPrecision2)");
        console.DisplayTextLine("      Examples:");
        console.DisplayTextLine("          CaseTest *.ct.json");
        console.DisplayTextLine("          CaseTest Test.ct.json /showall");
        console.DisplayTextLine("          CaseTest Test.ct.json /TestPrecision3");
    }
}