using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using PayrollEngine.Client.Command;
using PayrollEngine.Client.Exchange;
using PayrollEngine.Client.Model;
using PayrollEngine.Client.Service;
using PayrollEngine.Client.Service.Api;
using Task = System.Threading.Tasks.Task;

namespace PayrollEngine.PayrollConsole.Commands.LoadTestCommands;

/// <summary>Execute payrun load test with timing metrics</summary>
[Command("PayrunLoadTest")]
// ReSharper disable once UnusedType.Global
internal sealed class PayrunLoadTestCommand : CommandBase<PayrunLoadTestParameters>
{
    public override bool BackendCommand => true;

    /// <summary>Execute payrun load test</summary>
    protected override async Task<int> Execute(CommandContext context, PayrunLoadTestParameters parameters)
    {
        var console = context.Console;

        DisplayTitle(console, "Payrun load test");
        if (context.DisplayLevel == DisplayLevel.Full)
        {
            console.DisplayTextLine($"Invocation file  {parameters.InvocationFile}");
            console.DisplayTextLine($"Employee count   {parameters.EmployeeCount}");
            console.DisplayTextLine($"Repetitions      {parameters.Repetitions}");
            console.DisplayTextLine($"Result file      {parameters.ResultFile}");
        }
        console.DisplayNewLine();

        try
        {
            // load invocation exchange
            var exchange = await FileReader.ReadAsync<Exchange>(parameters.InvocationFile);
            if (exchange?.Tenants == null || exchange.Tenants.Count == 0)
            {
                console.DisplayErrorLine("Invalid invocation file: no tenants found.");
                return (int)ProgramExitCode.InvalidInput;
            }

            var exchangeTenant = exchange.Tenants[0];
            var invocations = exchangeTenant.PayrunJobInvocations;
            if (invocations == null || invocations.Count == 0)
            {
                console.DisplayErrorLine("Invalid invocation file: no PayrunJobInvocation found.");
                return (int)ProgramExitCode.InvalidInput;
            }

            console.DisplayTextLine($"Invocations      {invocations.Count} periods");
            console.DisplayNewLine();

            // resolve tenant
            var tenantService = new TenantService(context.HttpClient);
            var tenant = await tenantService.GetAsync<Tenant>(new(), exchangeTenant.Identifier);
            if (tenant == null)
            {
                console.DisplayErrorLine($"Tenant {exchangeTenant.Identifier} not found.");
                return (int)ProgramExitCode.InvalidInput;
            }

            var tenantContext = new TenantServiceContext(tenant.Id);
            var payrunJobService = new PayrunJobService(context.HttpClient);
            var allResults = new List<PayrunLoadTestResult>();

            // warmup run (first invocation only, not measured)
            console.DisplayTextLine("Warmup run...");
            var warmupInvocation = CreateInvocation(invocations[0], "Warmup");
            var warmupJob = await payrunJobService.StartJobAsync<PayrunJob>(tenantContext, warmupInvocation);
            var completedWarmup = await WaitForJobCompletion(payrunJobService, tenantContext, warmupJob.Id, console);
            console.DisplayTextLine($"  Warmup complete ({completedWarmup.TotalEmployeeCount} employees)");
            console.DisplayNewLine();

            // measured runs: execute all invocations sequentially per run
            for (var run = 1; run <= parameters.Repetitions; run++)
            {
                console.DisplayTextLine($"Run {run}/{parameters.Repetitions} ({invocations.Count} periods)...");

                var runStopwatch = Stopwatch.StartNew();
                long runServerTotal = 0;
                var runEmployeeTotal = 0;

                foreach (var invocation in invocations)
                {
                    var runInvocation = CreateInvocation(invocation, $"Run-{run}");

                    var stopwatch = Stopwatch.StartNew();
                    var startedJob = await payrunJobService.StartJobAsync<PayrunJob>(
                        tenantContext, runInvocation);
                    var payrunJob = await WaitForJobCompletion(
                        payrunJobService, tenantContext, startedJob.Id, console);
                    stopwatch.Stop();

                    var serverDurationMs = payrunJob.JobEnd.HasValue
                        ? (long)(payrunJob.JobEnd.Value - payrunJob.JobStart).TotalMilliseconds
                        : stopwatch.ElapsedMilliseconds;
                    var employeeCount = payrunJob.TotalEmployeeCount > 0
                        ? payrunJob.TotalEmployeeCount
                        : parameters.EmployeeCount;

                    runServerTotal += serverDurationMs;
                    runEmployeeTotal += employeeCount;

                    // per-period result
                    allResults.Add(new PayrunLoadTestResult
                    {
                        Timestamp = DateTimeOffset.Now,
                        RunNumber = run,
                        Period = invocation.PeriodStart,
                        EmployeeCount = employeeCount,
                        ClientDurationMs = stopwatch.ElapsedMilliseconds,
                        ServerJobDurationMs = serverDurationMs,
                        ServerAvgMsPerEmployee = employeeCount > 0
                            ? serverDurationMs / (double)employeeCount
                            : 0
                    });
                }
                runStopwatch.Stop();

                console.DisplayTextLine(
                    $"  Total: Client {runStopwatch.ElapsedMilliseconds}ms | " +
                    $"Server {runServerTotal}ms | " +
                    $"Employees {runEmployeeTotal} | " +
                    $"Avg {(runEmployeeTotal > 0 ? runServerTotal / (double)runEmployeeTotal : 0):F1}ms/employee");
            }

            // write CSV report
            WriteResults(parameters.ResultFile, allResults);

            // write Excel report (optional)
            if (parameters.ExcelReport)
            {
                var excelPath = Path.ChangeExtension(parameters.ResultFile, ".xlsx");
                new PayrunLoadTestExcelWriter(excelPath, parameters, allResults).Write();
                console.DisplayTextLine($"Excel written to   {Path.GetFullPath(excelPath!)}");
            }

            // display summary (median of total server duration per run)
            var runTotals = allResults
                .GroupBy(r => r.RunNumber)
                .Select(g => new
                {
                    ServerTotal = g.Sum(r => r.ServerJobDurationMs),
                    EmployeeTotal = g.Sum(r => r.EmployeeCount)
                })
                .OrderBy(r => r.ServerTotal)
                .ToList();
            var median = runTotals[runTotals.Count / 2];
            console.DisplayNewLine();
            console.DisplaySuccessLine(
                $"Median: {median.ServerTotal}ms total | " +
                $"{median.EmployeeTotal} employees | " +
                $"{(median.EmployeeTotal > 0 ? median.ServerTotal / (double)median.EmployeeTotal : 0):F1}ms/employee");
            console.DisplayTextLine($"Results written to {Path.GetFullPath(parameters.ResultFile!)}");

            return (int)ProgramExitCode.Ok;
        }
        catch (Exception exception)
        {
            ProcessError(context.Console, exception);
            return (int)ProgramExitCode.GenericError;
        }
    }

    /// <summary>
    /// Poll job until processing is complete (JobEnd is set).
    /// Uses adaptive polling: starts fast (100ms) for small jobs,
    /// then backs off to 1s for long-running jobs.
    /// </summary>
    private static async Task<PayrunJob> WaitForJobCompletion(
        PayrunJobService payrunJobService, TenantServiceContext tenantContext,
        int jobId, ICommandConsole console)
    {
        const int initialPollMs = 100;  // fast polling for first 5 seconds
        const int normalPollMs = 1000;  // slower after that
        const int fastPollDurationMs = 5000;
        const int maxWaitMinutes = 60;

        var pollStart = Stopwatch.StartNew();
        var maxWaitMs = maxWaitMinutes * 60 * 1000;

        while (pollStart.ElapsedMilliseconds < maxWaitMs)
        {
            // adaptive delay: fast initially, slower for long jobs
            var delayMs = pollStart.ElapsedMilliseconds < fastPollDurationMs
                ? initialPollMs
                : normalPollMs;
            await Task.Delay(delayMs);

            var job = await payrunJobService.GetAsync<PayrunJob>(tenantContext, jobId);

            // job aborted or cancelled
            if (job.JobStatus is PayrunJobStatus.Abort or PayrunJobStatus.Cancel)
            {
                throw new PayrollException(
                    $"Payrun job {jobId} ended with status {job.JobStatus}");
            }

            // job processing complete: JobEnd is set by the backend
            // only after all employees have been processed
            if (job.JobEnd.HasValue)
            {
                return job;
            }

            // progress indicator every 10 seconds
            if (pollStart.ElapsedMilliseconds > 0 && pollStart.ElapsedMilliseconds % 10000 < delayMs)
            {
                console.DisplayTextLine($"  ... waiting ({pollStart.ElapsedMilliseconds / 1000}s)");
            }
        }

        throw new PayrollException(
            $"Payrun job {jobId} did not complete within {maxWaitMinutes} minutes");
    }

    /// <summary>Create a fresh invocation with unique name for each run</summary>
    private static PayrunJobInvocation CreateInvocation(PayrunJobInvocation template, string suffix) =>
        new()
        {
            Name = $"{template.Name}-{suffix}-{DateTime.Now:HHmmss}",
            PayrunName = template.PayrunName,
            UserIdentifier = template.UserIdentifier,
            JobStatus = template.JobStatus,
            PeriodStart = template.PeriodStart,
            EvaluationDate = template.EvaluationDate,
            Reason = $"{template.Reason} ({suffix})"
            // no EmployeeIdentifiers → all employees
        };

    /// <summary>Write results as CSV (semicolon-separated)</summary>
    private static void WriteResults(string path, List<PayrunLoadTestResult> results)
    {
        var sb = new StringBuilder();
        sb.AppendLine("Timestamp;Run;Period;EmployeeCount;ClientDuration_ms;" +
                      "ServerJobDuration_ms;ServerAvgMs_PerEmployee");
        foreach (var r in results)
        {
            sb.AppendLine(string.Format(CultureInfo.InvariantCulture,
                "{0:yyyy-MM-dd HH:mm:ss};{1};{2:yyyy-MM};{3};{4};{5};{6:F1}",
                r.Timestamp, r.RunNumber, r.Period, r.EmployeeCount,
                r.ClientDurationMs, r.ServerJobDurationMs,
                r.ServerAvgMsPerEmployee));
        }

        var directory = Path.GetDirectoryName(Path.GetFullPath(path));
        if (!string.IsNullOrEmpty(directory))
        {
            Directory.CreateDirectory(directory);
        }
        File.WriteAllText(path, sb.ToString());
    }

    /// <inheritdoc />
    public override ICommandParameters GetParameters(CommandLineParser parser) =>
        PayrunLoadTestParameters.ParserFrom(parser);

    /// <inheritdoc />
    public override void ShowHelp(ICommandConsole console)
    {
        console.DisplayTitleLine("- PayrunLoadTest");
        console.DisplayTextLine("      Execute payrun and measure performance");
        console.DisplayTextLine("      Setup must be done beforehand (LoadTestSetup + LoadTestSetupCases)");
        console.DisplayTextLine("      Arguments:");
        console.DisplayTextLine("          1. Path to Payrun-Invocation exchange file [InvocationFile]");
        console.DisplayTextLine("          2. Expected employee count [EmployeeCount]");
        console.DisplayTextLine("          3. Number of repetitions (optional, default: 3) [Repetitions]");
        console.DisplayTextLine("          4. Output CSV path (optional, default: LoadTestResults.csv) [ResultFile]");
        console.DisplayTextLine("      Toggles:");
        console.DisplayTextLine("          /ExcelReport             Also write Excel alongside CSV (derived filename)");
        console.DisplayTextLine("      Options:");
        console.DisplayTextLine("          /ExcelFile=<path>        Explicit Excel output path (also enables Excel report)");
        console.DisplayTextLine("          /ParallelSetting=<v>     Backend MaxParallelEmployees value (for Excel setup sheet)");
        console.DisplayTextLine("      Examples:");
        console.DisplayTextLine("          PayrunLoadTest LoadTest100\\Payrun-Invocation.json 100");
        console.DisplayTextLine("          PayrunLoadTest LoadTest1000\\Payrun-Invocation.json 1000 5 Results\\LT1000.csv");
        console.DisplayTextLine("          PayrunLoadTest LoadTest1000\\Payrun-Invocation.json 1000 5 Results\\LT1000.csv /ExcelReport");
        console.DisplayTextLine("          PayrunLoadTest LoadTest1000\\Payrun-Invocation.json 1000 5 Results\\LT1000.csv /ExcelFile=Reports\\LT1000.xlsx /ParallelSetting=half");
    }
}
