using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using PayrollEngine.Client;
using PayrollEngine.Client.Script;
using PayrollEngine.Client.Scripting.Script;
using PayrollEngine.Client.Test;
using PayrollEngine.Client.Test.Payrun;
using PayrollEngine.PayrollConsole.Shared;
using PayrollEngine.Serialization;

namespace PayrollEngine.PayrollConsole.Command;

internal sealed class PayrunTestCommand : PayrunTestCommandBase
{
    private IScriptParser ScriptParser { get; } = new ScriptParser();

    internal PayrunTestCommand(PayrollHttpClient httpClient, TestPrecision testPrecision) :
        base(httpClient, testPrecision)
    {
    }

    /// <summary>
    /// Test a payrun specified in a payroll exchange JSON file
    /// The test sequence is:
    ///   1. Read test payroll
    ///   2. Create test tenant (existing tenant will be deleted)
    ///   3. Execute payrun job (payrun job must be present)
    ///   4. Compare the file results with the calculated payrun results (results must be present)
    ///   5. Remove test payroll
    /// </summary>
    /// <param name="settings">The command settings</param>
    /// <returns>True if no test failed</returns>
    internal async Task<ProgramExitCode> TestAsync(ReportTestCommandSettings settings)
    {
        if (string.IsNullOrWhiteSpace(settings.FileMask))
        {
            throw new PayrollException($"Missing test file {settings.FileMask}");
        }

        // test files by mask
        var testFileNames = GetTestFileNames(settings.FileMask);
        if (testFileNames.Count == 0)
        {
            throw new PayrollException($"Missing test file {settings.FileMask}");
        }

        try
        {
            foreach (var testFileName in testFileNames)
            {
                DisplayTitle("Test payrun");
                if (!string.IsNullOrWhiteSpace(settings.Owner))
                {
                    ConsoleTool.DisplayTextLine($"Owner              {settings.Owner}");
                }
                ConsoleTool.DisplayTextLine($"Path               {new FileInfo(testFileName).Directory?.FullName}");
                ConsoleTool.DisplayTextLine($"File               {testFileName}");
                ConsoleTool.DisplayTextLine($"Import mode        {settings.ImportMode}");
                ConsoleTool.DisplayTextLine($"Display mode       {settings.DisplayMode}");
                ConsoleTool.DisplayTextLine($"Result mode        {settings.ResultMode}");
                ConsoleTool.DisplayTextLine($"Url                {HttpClient}");
                ConsoleTool.DisplayTextLine($"Test precision     {TestPrecision.GetDecimals()}");
                ConsoleTool.DisplayNewLine();

                ConsoleTool.DisplayTextLine("Running test...");

                // load test data
                var exchange = await JsonSerializer.DeserializeFromFileAsync<Client.Model.Exchange>(testFileName);
                if (exchange == null)
                {
                    throw new PayrollException($"Invalid case test file {testFileName}");
                }

                // run test
                var testRunner = new PayrunTestRunner(HttpClient, ScriptParser,
                    TestPrecision, settings.Owner, settings.ImportMode, settings.ResultMode);
                var results = await testRunner.TestAllAsync(exchange);

                // display test results
                ConsoleTool.DisplayNewLine();
                DisplayTestResults(testFileName, settings.DisplayMode, results);

                // failed test
                foreach (var resultValues in results.Values)
                {
                    if (resultValues.Any(x => x.Failed))
                    {
                        return ProgramExitCode.FailedTest;
                    }
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
        ConsoleTool.DisplayTitleLine("- PayrunTest");
        ConsoleTool.DisplayTextLine("      Execute payrun and test the results, existing tenant will be deleted");
        ConsoleTool.DisplayTextLine("      Arguments:");
        ConsoleTool.DisplayTextLine("          1. JSON/ZIP file name or file mask [FileMask]");
        ConsoleTool.DisplayTextLine("          2. namespace (optional) [Namespace]");
        ConsoleTool.DisplayTextLine("          3. owner name (optional) [Owner]");
        ConsoleTool.DisplayTextLine("      Toggles:");
        ConsoleTool.DisplayTextLine("          import mode: /single or /bulk (default: bulk)");
        ConsoleTool.DisplayTextLine("          test display mode: /showfailed or /showall (default: showfailed)");
        ConsoleTool.DisplayTextLine("          test result mode: /cleantest or /keeptest (default: cleantest)");
        ConsoleTool.DisplayTextLine("          test precision: /TestPrecisionOff or /TestPrecision1 to /TestPrecision6 (default: /TestPrecision2)");
        ConsoleTool.DisplayTextLine("      Examples:");
        ConsoleTool.DisplayTextLine("          PayrunTest Test.json");
        ConsoleTool.DisplayTextLine("          PayrunTest *.pt.json");
        ConsoleTool.DisplayTextLine("          PayrunTest *.pt.json MyNamespace");
        ConsoleTool.DisplayTextLine("          PayrunTest *.pt.json MyNamespace MyOwner");
        ConsoleTool.DisplayTextLine("          PayrunTest Test.pt.json /single /showall /keeptest /TestPrecision3");
    }
}