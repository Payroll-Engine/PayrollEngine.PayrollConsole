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
    /// <param name="settings">The command settings</param>
    internal async Task<ProgramExitCode> TestAsync(PayrunEmployeeTestCommandSettings settings)
    {
        // test arguments
        if (settings == null)
        {
            throw new ArgumentNullException(nameof(settings));
        }
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
                DisplayTitle("Test employee payrun");
                if (!string.IsNullOrWhiteSpace(settings.Owner))
                {
                    ConsoleTool.DisplayTextLine($"Owner              {settings.Owner}");
                }
                ConsoleTool.DisplayTextLine($"Path               {new FileInfo(testFileName).Directory?.FullName}");
                ConsoleTool.DisplayTextLine($"File               {testFileName}");
                ConsoleTool.DisplayTextLine($"Test mode          {settings.TestMode}");
                ConsoleTool.DisplayTextLine($"Display mode       {settings.DisplayMode}");
                ConsoleTool.DisplayTextLine($"Url                {HttpClient}");
                ConsoleTool.DisplayTextLine($"Test precision     {TestPrecision.GetDecimals()}");
                ConsoleTool.DisplayNewLine();

                ConsoleTool.DisplayTextLine("Running test...");

                // load test data
                var exchange = await JsonSerializer.DeserializeFromFileAsync<Client.Model.Exchange>(testFileName);
                if (exchange == null)
                {
                    throw new PayrollException($"Invalid employee payrun test file {testFileName}");
                }

                // run test
                var testRunner = new PayrunEmployeeTestRunner(HttpClient, ScriptParser,
                    TestPrecision, settings.Owner, settings.TestMode);
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
        ConsoleTool.DisplayTitleLine("- PayrunEmployeeTest");
        ConsoleTool.DisplayTextLine("      Execute employee payrun and test the results");
        ConsoleTool.DisplayTextLine("      Arguments:");
        ConsoleTool.DisplayTextLine("          1. JSON/ZIP file name or file mask [FileMask]");
        ConsoleTool.DisplayTextLine("          2. owner name (optional) [Owner]");
        ConsoleTool.DisplayTextLine("      Toggles:");
        ConsoleTool.DisplayTextLine("          test mode: /insertemployee or /updateemployee (default: insertemployee)");
        ConsoleTool.DisplayTextLine("          test display mode: /showfailed or /showall (default: showfailed)");
        ConsoleTool.DisplayTextLine("          test precision: /TestPrecisionOff or /TestPrecision1 to /TestPrecision6 (default: /TestPrecision2)");
        ConsoleTool.DisplayTextLine("      Examples:");
        ConsoleTool.DisplayTextLine("          PayrunEmployeeTest Test.json");
        ConsoleTool.DisplayTextLine("          PayrunEmployeeTest *.et.json");
        ConsoleTool.DisplayTextLine("          PayrunEmployeeTest Test.json /showall /TestPrecision3");
    }
}