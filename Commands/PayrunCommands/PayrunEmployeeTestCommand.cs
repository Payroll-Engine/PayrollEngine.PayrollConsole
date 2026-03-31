using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using PayrollEngine.Client.Test;
using PayrollEngine.Client.Script;
using PayrollEngine.Client.Command;
using PayrollEngine.Client.Exchange;
using PayrollEngine.Client.Model;
using PayrollEngine.Client.Test.Payrun;
using PayrollEngine.Client.Scripting.Script;
using Task = System.Threading.Tasks.Task;

namespace PayrollEngine.PayrollConsole.Commands.PayrunCommands;

/// <summary>
/// Payrun employee test command
/// </summary>
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
    /// <param name="context">Command execution context</param>
    /// <param name="parameters">Command parameters</param>
    /// <returns>Program exit ok, 0 on success</returns>
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
                    context.Console.DisplayTextLine($"Import mode        {parameters.ImportMode}");
                    context.Console.DisplayTextLine($"Test mode          {parameters.TestMode}");
                    context.Console.DisplayTextLine($"Running mode       {parameters.RunMode}");
                    context.Console.DisplayTextLine($"Display mode       {parameters.DisplayMode}");
                    context.Console.DisplayTextLine($"Result mode        {parameters.ResultMode}");
                    context.Console.DisplayTextLine($"Url                {context.HttpClient}");
                    context.Console.DisplayTextLine($"Test precision     {parameters.Precision.GetDecimals()}");
                }

                context.Console.DisplayNewLine();

                context.Console.DisplayTextLine("Running test...");

                // load test data
                var exchange = await FileReader.ReadAsync<Exchange>(testFileName);
                if (exchange == null)
                {
                    throw new PayrollException($"Invalid employee payrun test file {testFileName}.");
                }

                // when capturing actual results: inject minimal payrollResults placeholders so
                // TestPayrunJobAsync runs fully and populates ActualResult on each test result object
                var captureActual = !string.IsNullOrWhiteSpace(parameters.ActualOutputFile);
                if (captureActual)
                {
                    InjectPayrollResultPlaceholders(exchange);
                }

                // test settings
                var settings = new PayrunTestSettings
                {
                    TestPrecision = parameters.Precision,
                    ResultMode = parameters.ResultMode,
                    Owner = parameters.Owner
                };

                // run test
                var testRunner = new PayrunEmployeeTestRunner(
                    httpClient: context.HttpClient,
                    scriptParser: ScriptParser,
                    settings: settings,
                    importMode: parameters.ImportMode,
                    employeeMode: parameters.TestMode,
                    runMode: parameters.RunMode);

                // import without progress indicator
                await testRunner.ImportAsync(exchange);

                // run payrun test with progress indicator
                using var cts = new CancellationTokenSource();
                var progressTask = RunProgressAsync(context.Console, cts.Token);
                try
                {
                    var results = await testRunner.TestAsync(exchange);

                    // display test results
                    context.Console.DisplayNewLine();
                    DisplayTestResults(context.Logger, context.Console, testFileName, parameters.DisplayMode, results);

                    // write actual results to file if requested
                    if (captureActual)
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
                finally
                {
                    await cts.CancelAsync();
                    await progressTask;
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

    // ── Actual results capture ─────────────────────────────────────────────

    /// <summary>
    /// Injects minimal payrollResults placeholders into the exchange so that
    /// TestPayrunJobAsync runs fully and populates ActualResult on each test result.
    /// Each invocation gets one placeholder with empty wageTypeResults and collectorResults.
    /// The employee identifier is taken from the first case change in the payroll.
    /// </summary>
    private static void InjectPayrollResultPlaceholders(Exchange exchange)
    {
        foreach (var tenant in exchange.Tenants ?? [])
        {
            if (tenant.PayrunJobInvocations == null || !tenant.PayrunJobInvocations.Any())
            {
                continue;
            }

            // only inject if no payrollResults present
            if (tenant.PayrollResults != null && tenant.PayrollResults.Any())
            {
                continue;
            }

            tenant.PayrollResults ??= [];

            // resolve employee identifier from case changes
            var employeeIdentifier = tenant.Payrolls?
                .SelectMany(p => p.Cases ?? [])
                .Select(c => c.EmployeeIdentifier)
                .FirstOrDefault(id => !string.IsNullOrWhiteSpace(id));

            foreach (var invocation in tenant.PayrunJobInvocations)
            {
                tenant.PayrollResults.Add(new PayrollResultSet
                {
                    PayrunJobName = invocation.Name,
                    EmployeeIdentifier = employeeIdentifier,
                    WageTypeResults = [],
                    CollectorResults = []
                });
            }
        }
    }

    /// <summary>
    /// Serializes actual WageType and Collector results from all test results to a JSON file.
    /// Output format matches the payrollResults block in .et.json — ready for direct use
    /// as expected values without manual editing.
    /// Employee identifier is reverse-mapped from the test clone ("X Test N" → "X").
    /// </summary>
    private static async Task WriteActualResultsAsync(
        Dictionary<Tenant, List<PayrollTestResult>> tenantResults,
        string outputFile)
    {
        var payrollResults = new List<object>();

        foreach (var tenantResult in tenantResults)
        {
            foreach (var result in tenantResult.Value)
            {
                // reverse-map test clone identifier back to the original employee identifier
                var employeeIdentifier = Regex.Replace(
                    result.Employee.Identifier,
                    @"\s+Test\s+\d+$",
                    string.Empty,
                    RegexOptions.IgnoreCase);

                var wageTypeResults = result.WageTypeResults
                    .Where(w => w.ActualResult != null)
                    .OrderBy(w => w.ActualResult.WageTypeNumber)
                    .Select(w => new
                    {
                        wageTypeNumber = w.ActualResult.WageTypeNumber,
                        value = w.ActualResult.Value
                    })
                    .ToList();

                var collectorResults = result.CollectorResults
                    .Where(c => c.ActualResult != null)
                    .OrderBy(c => c.ActualResult.CollectorName)
                    .Select(c => new
                    {
                        collectorName = c.ActualResult.CollectorName,
                        value = c.ActualResult.Value
                    })
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
        PayrunEmployeeTestParameters.ParserFrom(parser);

    /// <inheritdoc />
    public override void ShowHelp(ICommandConsole console)
    {
        console.DisplayTitleLine("- PayrunEmployeeTest");
        console.DisplayTextLine("      Execute employee payrun and test the results");
        console.DisplayTextLine("      Arguments:");
        console.DisplayTextLine("          1. JSON/YAML/ZIP file name or file mask [FileMask]");
        console.DisplayTextLine("          2. owner name (optional) [Owner]");
        console.DisplayTextLine("      Toggles:");
        console.DisplayTextLine("          import mode: /single or /bulk (default: bulk)");
        console.DisplayTextLine("          test mode: /insertemployee or /updateemployee (default: insertemployee)");
        console.DisplayTextLine("          running mode: /runtests or /skiptests (default: runtests)");
        console.DisplayTextLine("          test display mode: /showfailed or /showall (default: showfailed)");
        console.DisplayTextLine("          test result mode: /cleantest, /keepfailedtest or /keeptest (default: cleantest)");
        console.DisplayTextLine("          test precision: /TestPrecisionOff or /TestPrecision1 to /TestPrecision6 (default: /TestPrecision2)");
        console.DisplayTextLine("          actual output: ActualOutputFile:<path> — write actual results as JSON (payrollResults format)");
        console.DisplayTextLine("      Examples:");
        console.DisplayTextLine("          PayrunEmployeeTest Test.json");
        console.DisplayTextLine("          PayrunEmployeeTest *.et.json");
        console.DisplayTextLine("          PayrunEmployeeTest Test.json /showall /TestPrecision3");
        console.DisplayTextLine("          PayrunEmployeeTest Test.json /bulk /showall");
        console.DisplayTextLine("          PayrunEmployeeTest Test.json ActualOutputFile:Test.actual.json");
    }
}
