using System;
using System.Linq;
using System.Threading.Tasks;
using PayrollEngine.Client;
using PayrollEngine.Client.Exchange;
using PayrollEngine.Client.Script;
using PayrollEngine.Client.Scripting.Script;
using PayrollEngine.Client.Test;
using PayrollEngine.Client.Test.Payrun;
using PayrollEngine.PayrollConsole.Shared;

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
    /// <param name="fileMask">The test file mask</param>
    /// <param name="importMode">The import mode</param>
    /// <param name="displayMode">Show results</param>
    /// <param name="resultMode">Keep the test data</param>
    /// <param name="namespace">The namespace</param>
    /// <param name="owner">The test owner</param>
    /// <returns>True if no test failed</returns>
    internal async Task<ProgramExitCode> TestAsync(string fileMask, DataImportMode importMode,
        TestDisplayMode displayMode, TestResultMode resultMode, string @namespace = null, string owner = null)
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
                DisplayTitle("Test payrun");
                if (!string.IsNullOrWhiteSpace(@namespace))
                {
                    ConsoleTool.DisplayTextLine($"Namespace          {@namespace}");
                }
                if (!string.IsNullOrWhiteSpace(owner))
                {
                    ConsoleTool.DisplayTextLine($"Owner              {owner}");
                }
                ConsoleTool.DisplayTextLine($"File               {testFileName}");
                ConsoleTool.DisplayTextLine($"Import mode        {importMode}");
                ConsoleTool.DisplayTextLine($"Display mode       {displayMode}");
                ConsoleTool.DisplayTextLine($"Result mode        {resultMode}");
                ConsoleTool.DisplayTextLine($"Url                {HttpClient}");
                ConsoleTool.DisplayTextLine($"Test precision     {TestPrecision.GetDecimals()}");
                ConsoleTool.DisplayNewLine();

                ConsoleTool.DisplayTextLine("Running test...");
                // run test
                var testRunner = new PayrunTestRunner(HttpClient, testFileName, ScriptParser,
                    TestPrecision, owner, importMode, resultMode);
                var results = await testRunner.TestAllAsync(@namespace);
                // test results
                DisplayTestResults(testFileName, displayMode, results);

                // failed test
                foreach (var resultValues in results.Values)
                {
                    if (resultValues.Any(x => x.IsFailed()))
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
        ConsoleTool.DisplayTextLine("      Test payrun results, existing tenant will be deleted");
        ConsoleTool.DisplayTextLine("      Arguments:");
        ConsoleTool.DisplayTextLine("          1. JSON/ZIP file name or file mask");
        ConsoleTool.DisplayTextLine("          2. namespace (optional)");
        ConsoleTool.DisplayTextLine("          3. owner name (optional)");
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