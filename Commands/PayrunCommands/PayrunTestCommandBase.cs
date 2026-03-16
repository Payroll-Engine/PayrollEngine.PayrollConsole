using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading;
using PayrollEngine.Client.Command;
using PayrollEngine.Client.Model;
using PayrollEngine.Client.Test.Payrun;
using Task = System.Threading.Tasks.Task;

namespace PayrollEngine.PayrollConsole.Commands.PayrunCommands;

/// <summary>
/// Payrun test command base
/// </summary>
internal abstract class PayrunTestCommandBase : TestCommandBase
{
    /// <summary>Show a waiting indicator every 10 seconds until cancelled</summary>
    protected static async Task RunProgressAsync(ICommandConsole console, CancellationToken cancellationToken)
    {
        const int intervalMs = 10000;
        const int tickMs = 500;
        var elapsed = 0;
        try
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                await Task.Delay(tickMs, cancellationToken);
                elapsed += tickMs;
                if (elapsed % intervalMs == 0)
                {
                    console.DisplayTextLine($"  ... waiting ({elapsed / 1000}s)");
                }
            }
        }
        catch (OperationCanceledException)
        {
            // expected on cancellation
        }
    }

    protected void DisplayTestResults(ILogger logger, ICommandConsole console, string fileName,
        TestDisplayMode displayMode, Dictionary<Tenant, List<PayrollTestResult>> tenantResults)
    {
        var culture = CultureInfo.GetCultureInfo("en-US");
        var duration = TimeSpan.Zero;
        foreach (var tenantResult in tenantResults)
        {
            var tenant = tenantResult.Key;
            var results = tenantResult.Value;

            var separator = new string('-', 10);
            var durations = new List<TimeSpan>();
            var errorCount = 0;

            // show results
            console.DisplayTextLine("Test results" +
                                        (tenantResults.Count > 1 ? $" for tenant {tenant.Identifier}" : string.Empty) + "...");
            foreach (var result in results)
            {
                var jobPeriod = new DatePeriod(result.PayrunJob.JobStart, result.PayrunJob.JobEnd);
                durations.Add(jobPeriod.Duration);

                var jobName = result.PayrunJob.ParentJobId.HasValue ?
                    $"{result.PayrunJob.Name} -> Retro {result.PayrunJob.PeriodStart.ToString("MMM yyyy", culture)}" :
                    result.PayrunJob.Name;
                logger.Information($"{separator} {result.Tenant.Identifier} - {result.Employee.Identifier}: " +
                                $"{jobName} [{result.TotalResultCount} result{(results.Count == 1 ? "" : "s")} in {jobPeriod.Duration.TotalMilliseconds:#0} ms] {separator}");

                // wage type result
                var failedWageTypeCount = DisplayWageTypeResults(logger, console, result, displayMode);
                // collector result
                var failedCollectorCount = DisplayCollectorResults(logger, result, displayMode);
                // payrun result
                var failedPayrunResultCount = DisplayPayrunResults(logger, console, result, displayMode);

                // summary
                var failedCount = failedWageTypeCount + failedCollectorCount + failedPayrunResultCount;
                // success
                var successCount = result.TotalResultCount - failedCount;
                if (successCount > 0)
                {
                    var successMessage = $"Passed tests: {successCount}";
                    if (failedCount > 0)
                    {
                        console.DisplayInfoLine(successMessage);
                    }
                    else
                    {
                        console.DisplaySuccessLine(successMessage);
                    }
                }
                // failed
                if (failedCount > 0)
                {
                    console.DisplayErrorLine($"Failed tests: {failedCount}");
                }
                errorCount += failedCount;
            }

            // statistics
            duration += DisplayStatistics(console, fileName, results, durations, errorCount);
        }

        if (tenantResults.Count > 1)
        {
            console.DisplayTextLine($"Overall duration: {duration.ToReadableString()}");
        }
    }

    private static int DisplayCollectorResults(ILogger logger, PayrollTestResult result, TestDisplayMode displayMode)
    {
        var failedCollectorCount = 0;
        foreach (var collectorResult in result.CollectorResults)
        {
            var invalidAttribute = collectorResult.FirstInvalidAttribute();
            var message = invalidAttribute == null ?
                $"-> Collector {collectorResult.ExpectedResult.CollectorName}: expected={collectorResult.ExpectedResult.Value}, actual={collectorResult.ActualResult?.Value}" :
                $"-> Collector {collectorResult.ExpectedResult.CollectorName} -> Attribute {invalidAttribute.Item1}: expected={invalidAttribute.Item2}, actual={invalidAttribute.Item3}";
            if (!collectorResult.ValidValue())
            {
                // failed collector result test
                failedCollectorCount++;
                logger.Error(message);
            }
            else if (!collectorResult.ValidCulture())
            {
                message = $"-> Collector {collectorResult.ExpectedResult.CollectorName}: expected={collectorResult.ExpectedResult.Culture}, actual={collectorResult.ActualResult?.Culture}";
                // failed collector result test
                failedCollectorCount++;
                logger.Error(message);
            }
            else if (displayMode == TestDisplayMode.ShowAll)
            {
                // succeeded collector result test
                logger.Debug(message);
            }

            // collector custom result
            if (collectorResult.CustomResults != null)
            {
                foreach (var collectorCustomResult in collectorResult.CustomResults)
                {
                    invalidAttribute = collectorCustomResult.FirstInvalidAttribute();
                    message = invalidAttribute == null ?
                        $"-> Collector custom result {collectorCustomResult.ExpectedResult.Source}: expected={collectorCustomResult.ExpectedResult.Value}, actual={collectorCustomResult.ActualResult?.Value}" :
                        $"-> Collector custom result {collectorCustomResult.ExpectedResult.Source} -> Attribute {invalidAttribute.Item1}: expected={invalidAttribute.Item2}, actual={invalidAttribute.Item3}";
                    if (!collectorCustomResult.ValidValue())
                    {
                        // failed collector custom result test
                        failedCollectorCount++;
                        logger.Error(message);
                    }
                    else if (!collectorCustomResult.ValidCulture())
                    {
                        message = $"-> Collector custom result {collectorCustomResult.ExpectedResult.CollectorName}: expected={collectorCustomResult.ExpectedResult.Culture}, actual={collectorCustomResult.ActualResult?.Culture}";
                        // failed collector custom result test
                        failedCollectorCount++;
                        logger.Error(message);
                    }
                    else if (displayMode == TestDisplayMode.ShowAll)
                    {
                        // succeeded collector custom result test
                        logger.Debug(message);
                    }
                }
            }
        }

        return failedCollectorCount;
    }

    private static int DisplayWageTypeResults(ILogger logger, ICommandConsole console,
        PayrollTestResult result, TestDisplayMode displayMode)
    {
        var failedWageTypeCount = 0;
        foreach (var wageTypeResult in result.WageTypeResults)
        {
            var invalidAttribute = wageTypeResult.FirstInvalidAttribute();
            var message = invalidAttribute == null ?
                $"-> Wage type {wageTypeResult.ExpectedResult.WageTypeNumber:0.####}: expected={wageTypeResult.ExpectedResult.Value}, actual={wageTypeResult.ActualResult?.Value}" :
                $"-> Wage type {wageTypeResult.ExpectedResult.WageTypeNumber:0.####} -> Attribute {invalidAttribute.Item1}: expected={invalidAttribute.Item2}, actual={invalidAttribute.Item3}";
            if (!wageTypeResult.ValidValue())
            {
                // failed wage type result test
                failedWageTypeCount++;
                logger.Error(message);
            }
            else if (!wageTypeResult.ValidCulture())
            {
                message = $"-> Wage type {wageTypeResult.ExpectedResult.WageTypeNumber:0.####}: expected={wageTypeResult.ExpectedResult.Culture}, actual={wageTypeResult.ActualResult?.Culture}";
                // failed wage type result test
                failedWageTypeCount++;
                logger.Error(message);
            }
            else if (displayMode == TestDisplayMode.ShowAll)
            {
                // succeeded wage type result test
                logger.Debug(message);
            }

            // wage type custom result
            if (wageTypeResult.CustomResults != null)
            {
                foreach (var wageTypeCustomResult in wageTypeResult.CustomResults)
                {
                    invalidAttribute = wageTypeCustomResult.FirstInvalidAttribute();
                    message = invalidAttribute == null ?
                        $"-> Wage type custom result {wageTypeCustomResult.ExpectedResult.Source}: expected={wageTypeCustomResult.ExpectedResult.Value}, actual={wageTypeCustomResult.ActualResult?.Value}" :
                        $"-> Wage type custom result {wageTypeCustomResult.ExpectedResult.Source} -> Attribute {invalidAttribute.Item1}: expected={invalidAttribute.Item2}, actual={invalidAttribute.Item3}";
                    if (!wageTypeCustomResult.ValidValue())
                    {
                        // failed wage type custom result test
                        failedWageTypeCount++;
                        logger.Error(message);
                    }
                    else if (!wageTypeCustomResult.ValidCulture())
                    {
                        message = $"-> Wage type custom result {wageTypeCustomResult.ExpectedResult.Source}: expected={wageTypeCustomResult.ExpectedResult.Culture}, actual={wageTypeCustomResult.ActualResult?.Culture}";
                        // failed wage type result test
                        failedWageTypeCount++;
                        logger.Error(message);
                    }
                    else if (displayMode == TestDisplayMode.ShowAll)
                    {
                        // succeeded wage type custom result test
                        console.DisplayInfoLine(message);
                    }
                }
            }
        }
        return failedWageTypeCount;
    }

    private static int DisplayPayrunResults(ILogger logger, ICommandConsole console,
        PayrollTestResult result, TestDisplayMode displayMode)
    {
        var failedPayrunResultCount = 0;
        foreach (var payrunTestResult in result.PayrunResults)
        {
            var invalidAttribute = payrunTestResult.FirstInvalidAttribute();
            var message = invalidAttribute == null ?
                $"-> Payrun result {payrunTestResult.ExpectedResult.Name}: expected={payrunTestResult.ExpectedResult.Value}, actual={payrunTestResult.ActualResult?.Value}" :
                $"-> Payrun result {payrunTestResult.ExpectedResult.Name} -> Attribute {invalidAttribute.Item1}: expected={invalidAttribute.Item2}, actual={invalidAttribute.Item3}";
            if (!payrunTestResult.ValidValue())
            {
                failedPayrunResultCount++;
                logger.Error(message);
            }
            else if (!payrunTestResult.ValidCulture())
            {
                message = $"-> Payrun result {payrunTestResult.ExpectedResult.Source}: expected={payrunTestResult.ExpectedResult.Culture}, actual={payrunTestResult.ActualResult?.Culture}";
                failedPayrunResultCount++;
                logger.Error(message);
            }
            else if (displayMode == TestDisplayMode.ShowAll)
            {
                console.DisplayInfoLine(message);
            }
        }

        return failedPayrunResultCount;
    }

    private static TimeSpan DisplayStatistics(ICommandConsole console, string fileName,
        List<PayrollTestResult> results, List<TimeSpan> durations, int errorCount)
    {
        if (results.Count <= 1)
        {
            return TimeSpan.Zero;
        }

        // info
        console.DisplayNewLine();
        var title = "---------- Test statistics ----------";
        console.DisplayTextLine(title);
        console.DisplayTextLine($"File          {new FileInfo(fileName).Name}");
        console.DisplayTextLine($"Jobs          {results.Count}");
        console.DisplayTextLine($"Results       {results.Sum(x => x.TotalResultCount)}");
        var duration = durations.Aggregate(TimeSpan.Zero, (subtotal, t) => subtotal.Add(t));
        console.DisplayTextLine($"Duration      {duration.TotalSeconds:0.#} sec");

        // performance
        var fastestIndex = durations.IndexOf(durations.Min());
        var slowestIndex = durations.IndexOf(durations.Max());
        if (results.Count > 2)
        {
            if (fastestIndex >= 0)
            {
                console.DisplayTextLine($"Fastest       {durations[fastestIndex].TotalMilliseconds:#0} ms - {results[fastestIndex].PayrunJob.Name}");
            }
            if (slowestIndex >= 0)
            {
                console.DisplayTextLine($"Slowest       {durations[slowestIndex].TotalMilliseconds:#0} ms - {results[slowestIndex].PayrunJob.Name}");
            }
        }

        // ms/Employee: avg payroll calculation time per employee across all periods.
        // A payrun job processes N employees concurrently — its JobStart/JobEnd duration
        // is shared by all N PayrollTestResults of that job. Summing durations naively
        // counts the same job duration N times. Correct denominator:
        //   unique job wall-clock time / total employee-period results.
        var uniqueJobMs = results
            .GroupBy(r => r.PayrunJob.Id)
            .Sum(g => new DatePeriod(g.First().PayrunJob.JobStart, g.First().PayrunJob.JobEnd).Duration.TotalMilliseconds);
        var msPerEmployee = uniqueJobMs / results.Count;
        console.DisplayTextLine($"ms/Employee   {msPerEmployee:#0} ms");
        var employeesPerHour = (int)Math.Round(3_600_000.0 / msPerEmployee);
        console.DisplayTextLine($"Employees/h   {employeesPerHour:#,0}");

        if (errorCount > 0)
        {
            console.DisplayErrorLine($"Errors        {errorCount}");
        }
        console.DisplayTextLine(new string('-', title.Length));

        return duration;
    }
}