using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using PayrollEngine.Client.Command;
using PayrollEngine.Client.Script;
using PayrollEngine.Client.Scripting.Script;
using PayrollEngine.Client.Test;
using PayrollEngine.Client.Test.Payrun;
using PayrollEngine.Serialization;

namespace PayrollEngine.PayrollConsole.Commands.PayrunCommands;

/// <summary>
/// Payrun test command
/// </summary>
[Command("PayrunTest")]
// ReSharper disable once UnusedType.Global
internal sealed class PayrunTestCommand : PayrunTestCommandBase<PayrunTestParameters>
{
    private IScriptParser ScriptParser { get; } = new ScriptParser();

    /// <summary>
    /// Test a payrun specified in a payroll exchange JSON file
    /// The test sequence is:
    ///   1. Read test payroll
    ///   2. Create test tenant (existing tenant will be deleted)
    ///   3. Execute payrun job (payrun job must be present)
    ///   4. Compare the file results with the calculated payrun results (results must be present)
    ///   5. Remove test payroll
    /// </summary>
    /// <param name="context">Command execution context</param>
    /// <param name="parameters">Command parameters</param>
    /// <returns>Program exit ok, 0 on success</returns>
    protected override async Task<int> Execute(CommandContext context, PayrunTestParameters parameters)
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
                DisplayTitle(context.Console, $"Payrun test - {testFileName}");
                if (context.DisplayLevel == DisplayLevel.Full)
                {
                    if (!string.IsNullOrWhiteSpace(parameters.Owner))
                    {
                        context.Console.DisplayTextLine($"Owner              {parameters.Owner}");
                    }

                    context.Console.DisplayTextLine($"Path               {new FileInfo(testFileName).Directory?.FullName}");
                    context.Console.DisplayTextLine($"File               {testFileName}");
                    context.Console.DisplayTextLine($"Import mode        {parameters.ImportMode}");
                    context.Console.DisplayTextLine($"Running mode       {parameters.RunMode}");
                    context.Console.DisplayTextLine($"Display mode       {parameters.DisplayMode}");
                    context.Console.DisplayTextLine($"Result mode        {parameters.ResultMode}");
                    context.Console.DisplayTextLine($"Url                {context.HttpClient}");
                    context.Console.DisplayTextLine($"Test precision     {parameters.Precision.GetDecimals()}");
                }

                context.Console.DisplayNewLine();

                context.Console.DisplayTextLine("Running test...");

                // load test data
                var exchange = await JsonSerializer.DeserializeFromFileAsync<Client.Model.Exchange>(testFileName);
                if (exchange == null)
                {
                    throw new PayrollException($"Invalid case test file {testFileName}.");
                }

                // test settings
                var settings = new PayrunTestSettings
                {
                    TestPrecision = parameters.Precision,
                    ResultMode = parameters.ResultMode,
                    Owner = parameters.Owner
                };

                // run test
                var testRunner = new PayrunTestRunner(
                    httpClient: context.HttpClient,
                    scriptParser: ScriptParser,
                    settings: settings,
                    importMode: parameters.ImportMode,
                    runMode: parameters.RunMode);
                var results = await testRunner.TestAllAsync(exchange);

                // display test results
                context.Console.DisplayNewLine();
                DisplayTestResults(
                    logger: context.Logger,
                    console: context.Console,
                    fileName: testFileName,
                    displayMode: parameters.DisplayMode,
                    tenantResults: results);

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

    /// <inheritdoc />
    public override ICommandParameters GetParameters(CommandLineParser parser) =>
        PayrunTestParameters.ParserFrom(parser);

    /// <inheritdoc />
    public override void ShowHelp(ICommandConsole console)
    {
        console.DisplayTitleLine("- PayrunTest");
        console.DisplayTextLine("      Execute payrun and test the results, existing tenant will be deleted");
        console.DisplayTextLine("      Arguments:");
        console.DisplayTextLine("          1. JSON/ZIP file name or file mask [FileMask]");
        console.DisplayTextLine("          2. owner name (optional) [Owner]");
        console.DisplayTextLine("      Toggles:");
        console.DisplayTextLine("          import mode: /single or /bulk (default: bulk)");
        console.DisplayTextLine("          running mode: /runtests or /skiptests (default: runtests)");
        console.DisplayTextLine("          test display mode: /showfailed or /showall (default: showfailed)");
        console.DisplayTextLine("          test result mode: /cleantest, /keepfailedtest or /keeptest (default: cleantest)");
        console.DisplayTextLine("          test precision: /TestPrecisionOff or /TestPrecision1 to /TestPrecision6 (default: /TestPrecision2)");
        console.DisplayTextLine("      Examples:");
        console.DisplayTextLine("          PayrunTest Test.json");
        console.DisplayTextLine("          PayrunTest *.pt.json");
        console.DisplayTextLine("          PayrunTest *.pt.json MyOwner");
        console.DisplayTextLine("          PayrunTest Test.pt.json /single /showall /keeptest /TestPrecision3");
    }
}