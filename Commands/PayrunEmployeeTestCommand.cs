using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using PayrollEngine.Client.Command;
using PayrollEngine.Client.Test;
using PayrollEngine.Serialization;
using PayrollEngine.Client.Script;
using PayrollEngine.Client.Test.Payrun;
using PayrollEngine.Client.Scripting.Script;

namespace PayrollEngine.PayrollConsole.Commands;

[Command("PayrunEmployeeTest")]
// ReSharper disable once UnusedType.Global
internal sealed class PayrunEmployeeTestCommand : PayrunTestCommandBase<PayrunEmployeeTestParameters>
{
    private IScriptParser ScriptParser { get; } = new ScriptParser();

    /// <summary>
    /// Test employee payrun specified in a payroll exchange JSON file
    /// The test sequence is:
    ///   1. Import employee case changes, payrun jobs and expected results (payroll, payrun job and employee must be present)
    ///   2. Create test employee, copy of existing employee
    ///   3. Setup test employee on jobs and results
    ///   4. Execute payrun jobs
    ///   5. Compare the file results with the calculated employee results (results must be present)
    /// </summary>
    protected override async Task<int> Execute(CommandContext context, PayrunEmployeeTestParameters parameters)
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
                DisplayTitle(context.Console, "Payrun employee test");
                if (context.DisplayLevel == DisplayLevel.Full)
                {
                    if (!string.IsNullOrWhiteSpace(parameters.Owner))
                    {
                        context.Console.DisplayTextLine($"Owner              {parameters.Owner}");
                    }

                    context.Console.DisplayTextLine($"Path               {new FileInfo(testFileName).Directory?.FullName}");
                    context.Console.DisplayTextLine($"File               {testFileName}");
                    context.Console.DisplayTextLine($"Test mode          {parameters.TestMode}");
                    context.Console.DisplayTextLine($"Running mode       {parameters.RunMode}");
                    context.Console.DisplayTextLine($"Display mode       {parameters.DisplayMode}");
                    context.Console.DisplayTextLine($"Url                {context.HttpClient}");
                    context.Console.DisplayTextLine($"Test precision     {parameters.Precision.GetDecimals()}");
                }

                context.Console.DisplayNewLine();

                context.Console.DisplayTextLine("Running test...");

                // load test data
                var exchange = await JsonSerializer.DeserializeFromFileAsync<Client.Model.Exchange>(testFileName);
                if (exchange == null)
                {
                    throw new PayrollException($"Invalid employee payrun test file {testFileName}.");
                }

                // run test
                var testRunner = new PayrunEmployeeTestRunner(context.HttpClient, ScriptParser,
                    parameters.Precision, parameters.Owner, parameters.TestMode, parameters.RunMode);
                var results = await testRunner.TestAllAsync(exchange);

                // display test results
                context.Console.DisplayNewLine();
                DisplayTestResults(context.Logger, context.Console, testFileName, parameters.DisplayMode, results);

                // failed test
                foreach (var resultValues in results.Values)
                {
                    if (resultValues.Any(x => x.Failed))
                    {
                        return (int)ProgramExitCode.FailedTest;
                    }
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

    public override ICommandParameters GetParameters(CommandLineParser parser) =>
        PayrunEmployeeTestParameters.ParserFrom(parser);

    public override void ShowHelp(ICommandConsole console)
    {
        console.DisplayTitleLine("- PayrunEmployeeTest");
        console.DisplayTextLine("      Execute employee payrun and test the results");
        console.DisplayTextLine("      Arguments:");
        console.DisplayTextLine("          1. JSON/ZIP file name or file mask [FileMask]");
        console.DisplayTextLine("          2. owner name (optional) [Owner]");
        console.DisplayTextLine("      Toggles:");
        console.DisplayTextLine("          test mode: /insertemployee or /updateemployee (default: insertemployee)");
        console.DisplayTextLine("          running mode: /runtests or /skiptests (default: runtests)");
        console.DisplayTextLine("          test display mode: /showfailed or /showall (default: showfailed)");
        console.DisplayTextLine("          test precision: /TestPrecisionOff or /TestPrecision1 to /TestPrecision6 (default: /TestPrecision2)");
        console.DisplayTextLine("      Examples:");
        console.DisplayTextLine("          PayrunEmployeeTest Test.json");
        console.DisplayTextLine("          PayrunEmployeeTest *.et.json");
        console.DisplayTextLine("          PayrunEmployeeTest Test.json /showall /TestPrecision3");
    }
}