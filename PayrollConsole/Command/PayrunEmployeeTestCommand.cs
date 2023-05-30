using System;
using System.Linq;
using System.Threading.Tasks;
using PayrollEngine.Client;
using PayrollEngine.Client.Script;
using PayrollEngine.Client.Scripting.Script;
using PayrollEngine.Client.Test;
using PayrollEngine.Client.Test.Payrun;
using PayrollEngine.PayrollConsole.Shared;

namespace PayrollEngine.PayrollConsole.Command;

internal sealed class PayrunEmployeeTestCommand : PayrunTestCommandBase
{
    private IScriptParser ScriptParser { get; } = new ScriptParser();

    internal PayrunEmployeeTestCommand(PayrollHttpClient httpClient, TestPrecision testPrecision) :
        base(httpClient, testPrecision)
    {
    }

    /// <summary>
    /// Test a employee payrun specified in a payroll exchange JSON file
    /// The test sequence is:
    ///   1. Import employee case changes, payrun jobs and expected results (payroll, payrun job and employee must be present)
    ///   2. Create test employee, copy of existing employee
    ///   3. Setup test employee on jobs and results
    ///   4. Execute payrun jobs
    ///   5. Compare the file results with the calculated employee results (results must be present)
    /// </summary>
    /// <param name="mask">The test file mask</param>
    /// <param name="displayMode">Show results</param>
    /// <param name="employeeTestMode">The employee test mode</param>
    /// <param name="namespace">The namespace</param>
    /// <param name="owner">The test owner</param>
    /// <returns>True if no test failed</returns>
    internal async Task<ProgramExitCode> TestAsync(string mask, TestDisplayMode displayMode, EmployeeTestMode employeeTestMode,
        string @namespace = null, string owner = null)
    {
        if (string.IsNullOrWhiteSpace(mask))
        {
            throw new PayrollException($"Missing test file {mask}");
        }

        // test files by mask
        var testFileNames = GetTestFileNames(mask);
        if (testFileNames.Count == 0)
        {
            throw new PayrollException($"Missing test file {mask}");
        }

        try
        {
            foreach (var testFileName in testFileNames)
            {
                DisplayTitle("Test employee payrun");
                if (!string.IsNullOrWhiteSpace(@namespace))
                {
                    ConsoleTool.DisplayTextLine($"Namespace          {@namespace}");
                }
                if (!string.IsNullOrWhiteSpace(owner))
                {
                    ConsoleTool.DisplayTextLine($"Owner              {owner}");
                }
                ConsoleTool.DisplayTextLine($"File               {testFileName}");
                ConsoleTool.DisplayTextLine($"Test mode          {employeeTestMode}");
                ConsoleTool.DisplayTextLine($"Display mode       {displayMode}");
                ConsoleTool.DisplayTextLine($"Url                {HttpClient}");
                ConsoleTool.DisplayTextLine($"Test precision     {TestPrecision.GetDecimals()}");
                ConsoleTool.DisplayNewLine();

                ConsoleTool.DisplayTextLine("Running test...");
                // run test
                var testRunner = new PayrunEmployeeTestRunner(HttpClient, testFileName, ScriptParser,
                    TestPrecision, owner, employeeTestMode);
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
        ConsoleTool.DisplayTitleLine("- PayrunEmployeeTest");
        ConsoleTool.DisplayTextLine("      Execute employee payrun and test the results");
        ConsoleTool.DisplayTextLine("      Arguments:");
        ConsoleTool.DisplayTextLine("          1. JSON/ZIP file name or file mask");
        ConsoleTool.DisplayTextLine("          2. namespace (optional)");
        ConsoleTool.DisplayTextLine("          3. owner name (optional)");
        ConsoleTool.DisplayTextLine("      Toggles:");
        ConsoleTool.DisplayTextLine("          test mode: /insertemployee or /updateemployee (default: insertemployee)");
        ConsoleTool.DisplayTextLine("          test display mode: /showfailed or /showall (default: showfailed)");
        ConsoleTool.DisplayTextLine("          test precision: /TestPrecisionOff or /TestPrecision1 to /TestPrecision6 (default: /TestPrecision2)");
        ConsoleTool.DisplayTextLine("      Examples:");
        ConsoleTool.DisplayTextLine("          PayrunEmployeeTest Test.json");
        ConsoleTool.DisplayTextLine("          PayrunEmployeeTest *.et.json");
        ConsoleTool.DisplayTextLine("          PayrunEmployeeTest *.et.json MyNamespace");
        ConsoleTool.DisplayTextLine("          PayrunEmployeeTest *.et.json MyNamespace MyOwner");
        ConsoleTool.DisplayTextLine("          PayrunEmployeeTest Test.json /showall /TestPrecision3");
    }
}