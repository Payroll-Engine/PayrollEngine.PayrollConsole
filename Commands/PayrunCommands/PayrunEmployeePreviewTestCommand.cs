using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using PayrollEngine.Client.Command;
using PayrollEngine.Client.Exchange;
using PayrollEngine.Client.Model;
using PayrollEngine.Client.Test;
using PayrollEngine.Client.Test.Payrun;
using Task = System.Threading.Tasks.Task;

namespace PayrollEngine.PayrollConsole.Commands.PayrunCommands;

/// <summary>
/// Payrun employee preview test command
/// </summary>
[Command("PayrunEmployeePreviewTest")]
// ReSharper disable once UnusedType.Global
internal sealed class PayrunEmployeePreviewTestCommand : PayrunTestCommandBase<PayrunEmployeePreviewTestParameters>
{
    /// <summary>
    /// Test employee payrun via preview specified in a payroll exchange JSON file
    /// The test sequence is:
    ///   1. Load test data from exchange file (payrun job invocations and expected results)
    ///   2. Execute payrun job preview for each invocation
    ///   3. Compare the preview results with the expected results
    /// </summary>
    /// <param name="context">Command execution context</param>
    /// <param name="parameters">Command parameters</param>
    /// <returns>Program exit ok, 0 on success</returns>
    protected override async Task<int> Execute(CommandContext context, PayrunEmployeePreviewTestParameters parameters)
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
                DisplayTitle(context.Console, "Payrun employee preview test");
                if (context.DisplayLevel == DisplayLevel.Full)
                {
                    if (!string.IsNullOrWhiteSpace(parameters.Owner))
                    {
                        context.Console.DisplayTextLine($"Owner              {parameters.Owner}");
                    }

                    context.Console.DisplayTextLine($"Path               {new FileInfo(testFileName).Directory?.FullName}");
                    context.Console.DisplayTextLine($"File               {testFileName}");
                    context.Console.DisplayTextLine($"Display mode       {parameters.DisplayMode}");
                    context.Console.DisplayTextLine($"Url                {context.HttpClient}");
                    context.Console.DisplayTextLine($"Test precision     {parameters.Precision.GetDecimals()}");
                }

                context.Console.DisplayNewLine();

                context.Console.DisplayTextLine("Running preview test...");

                // load test data
                var exchange = await FileReader.ReadAsync<Client.Model.Exchange>(testFileName);
                if (exchange == null)
                {
                    throw new PayrollException($"Invalid employee payrun preview test file {testFileName}.");
                }

                // test settings
                var settings = new PayrunTestSettings
                {
                    TestPrecision = parameters.Precision,
                    Owner = parameters.Owner
                };

                // run preview test
                var testRunner = new PayrunEmployeePreviewTestRunner(
                    httpClient: context.HttpClient,
                    settings: settings);
                var results = await testRunner.TestAllAsync(exchange);

                // display test results
                context.Console.DisplayNewLine();
                DisplayTestResults(context.Logger, context.Console, testFileName, parameters.DisplayMode, results);

                // write actual results to file if requested
                if (!string.IsNullOrWhiteSpace(parameters.ActualOutputFile))
                {
                    await WriteActualResultsAsync(results, parameters.ActualOutputFile);
                    context.Console.DisplayTextLine($"Actual results written to: {parameters.ActualOutputFile}");
                }

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

    // ── Actual results writer ──────────────────────────────────────────────

    private static async Task WriteActualResultsAsync(
        Dictionary<Tenant, List<PayrollTestResult>> tenantResults,
        string outputFile)
    {
        var payrollResults = new List<object>();

        foreach (var tenantResult in tenantResults)
        {
            foreach (var result in tenantResult.Value)
            {
                var employeeIdentifier = Regex.Replace(
                    result.Employee.Identifier,
                    @"\s+Test\s+\d+$",
                    string.Empty,
                    RegexOptions.IgnoreCase);

                var wageTypeResults = result.WageTypeResults
                    .Where(w => w.ActualResult != null)
                    .OrderBy(w => w.ActualResult.WageTypeNumber)
                    .Select(w => new { wageTypeNumber = w.ActualResult.WageTypeNumber, value = w.ActualResult.Value })
                    .ToList();

                var collectorResults = result.CollectorResults
                    .Where(c => c.ActualResult != null)
                    .OrderBy(c => c.ActualResult.CollectorName)
                    .Select(c => new { collectorName = c.ActualResult.CollectorName, value = c.ActualResult.Value })
                    .ToList();

                payrollResults.Add(new
                {
                    payrunJobName = result.PayrunJob.Name,
                    employeeIdentifier,
                    wageTypeResults,
                    collectorResults
                });
            }
        }

        var json = JsonSerializer.Serialize(
            payrollResults,
            new JsonSerializerOptions { WriteIndented = true });

        await File.WriteAllTextAsync(outputFile, json);
    }

    /// <inheritdoc />
    public override ICommandParameters GetParameters(CommandLineParser parser) =>
        PayrunEmployeePreviewTestParameters.ParserFrom(parser);

    /// <inheritdoc />
    public override void ShowHelp(ICommandConsole console)
    {
        console.DisplayTitleLine("- PayrunEmployeePreviewTest");
        console.DisplayTextLine("      Execute employee payrun preview and test the results");
        console.DisplayTextLine("      Arguments:");
        console.DisplayTextLine("          1. JSON/YAML/ZIP file name or file mask [FileMask]");
        console.DisplayTextLine("          2. owner name (optional) [Owner]");
        console.DisplayTextLine("      Toggles:");
        console.DisplayTextLine("          test display mode: /showfailed or /showall (default: showfailed)");
        console.DisplayTextLine("          test precision: /TestPrecisionOff or /TestPrecision1 to /TestPrecision6 (default: /TestPrecision2)");
        console.DisplayTextLine("          actual output: ActualOutputFile:<path> — write actual results as JSON (payrollResults format)");
        console.DisplayTextLine("      Examples:");
        console.DisplayTextLine("          PayrunEmployeePreviewTest Test.et.json");
        console.DisplayTextLine("          PayrunEmployeePreviewTest *.et.json");
        console.DisplayTextLine("          PayrunEmployeePreviewTest Test.et.json /showall /TestPrecision3");
        console.DisplayTextLine("          PayrunEmployeePreviewTest Test.et.json /ActualOutputFile:Test.actual.json");
    }
}
