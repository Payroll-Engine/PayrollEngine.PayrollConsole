using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using PayrollEngine.Client;
using PayrollEngine.Client.Model;
using PayrollEngine.Client.Test;
using PayrollEngine.Client.Test.Payrun;
using PayrollEngine.PayrollConsole.Shared;

namespace PayrollEngine.PayrollConsole.Command;

internal abstract class PayrunTestCommandBase : TestCommandBase
{
    protected PayrunTestCommandBase(PayrollHttpClient httpClient, TestPrecision testPrecision) :
        base(httpClient, testPrecision)
    {
    }

    protected ProgramExitCode DisplayTestResults(string fileName, TestDisplayMode displayMode,
        Dictionary<Tenant, List<PayrollTestResult>> tenantResults)
    {
        var exitCode = ProgramExitCode.Ok;
        var culture = CultureInfo.GetCultureInfo("en-US");
        foreach (var tenantResult in tenantResults)
        {
            var tenant = tenantResult.Key;
            var results = tenantResult.Value;

            var separator = new string('-', 10);
            var durations = new List<TimeSpan>();
            var errorCount = 0;

            // show results
            ConsoleTool.DisplayTextLine($"Testing result values for tenant {tenant.Identifier}");
            foreach (var result in results)
            {
                var jobPeriod = new DatePeriod(result.PayrunJob.JobStart, result.PayrunJob.JobEnd);
                durations.Add(jobPeriod.Duration);

                var jobName = result.PayrunJob.ParentJobId.HasValue ?
                    $"{result.PayrunJob.Name} -> Retro {result.PayrunJob.PeriodStart.ToString("MMM yyyy", culture)}" :
                    result.PayrunJob.Name;
                Log.Information($"{separator} {result.Tenant.Identifier} - {result.Employee.Identifier}: " +
                                $"{jobName} [{result.TotalResultCount} results in {jobPeriod.Duration.TotalMilliseconds:#0} ms] {separator}");

                // wage type result
                var failedWageTypeCount = DisplayWageTypeResults(result, displayMode);
                // collector result
                var failedCollectorCount = DisplayCollectorResults(result, displayMode);
                // payrun result
                var failedPayrunResultCount = DisplayPayrunResults(result, displayMode);

                // summary
                if (failedWageTypeCount == 0 && failedCollectorCount == 0 && failedPayrunResultCount == 0)
                {
                    ConsoleTool.DisplaySuccessLine("-> no errors");
                }
                else
                {
                    errorCount += failedWageTypeCount + failedCollectorCount + failedPayrunResultCount;
                    ConsoleTool.DisplayErrorLine($"Test {GetLocalFileName(fileName)} failed");
                    exitCode = ProgramExitCode.FailedTest;
                }
            }

            // statistics
            DisplayStatistics(fileName, results, durations, errorCount);
        }

        return exitCode;
    }

    private static int DisplayCollectorResults(PayrollTestResult result, TestDisplayMode displayMode)
    {
        var failedCollectorCount = 0;
        foreach (var collectorResult in result.CollectorResults)
        {
            var invalidAttribute = collectorResult.FirstInvalidAttribute();
            var message = invalidAttribute == null ?
                $"-> Collector {collectorResult.ExpectedResult.CollectorName}: expected={collectorResult.ExpectedResult.Value}, actual={collectorResult.ActualResult?.Value}" :
                $"-> Collector {collectorResult.ExpectedResult.CollectorName} -> Attribute {invalidAttribute.Item1}: expected={invalidAttribute.Item2}, actual={invalidAttribute.Item3}";
            if (collectorResult.IsInvalidResult())
            {
                // failed collector result test
                failedCollectorCount++;
                Log.Error(message);
            }
            else if (displayMode == TestDisplayMode.ShowAll)
            {
                // succeeded collector result test
                Log.Debug(message);
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
                    if (collectorCustomResult.IsInvalidResult())
                    {
                        // failed collector custom result test
                        failedCollectorCount++;
                        Log.Error(message);
                    }
                    else if (displayMode == TestDisplayMode.ShowAll)
                    {
                        // succeeded collector custom result test
                        Log.Debug(message);
                    }
                }
            }
        }

        return failedCollectorCount;
    }

    private static int DisplayWageTypeResults(PayrollTestResult result, TestDisplayMode displayMode)
    {
        var failedWageTypeCount = 0;
        foreach (var wageTypeResult in result.WageTypeResults)
        {
            var invalidAttribute = wageTypeResult.FirstInvalidAttribute();
            var message = invalidAttribute == null ?
                $"-> Wage type {wageTypeResult.ExpectedResult.WageTypeNumber:0.####}: expected={wageTypeResult.ExpectedResult.Value}, actual={wageTypeResult.ActualResult?.Value}" :
                $"-> Wage type {wageTypeResult.ExpectedResult.WageTypeNumber:0.####} -> Attribute {invalidAttribute.Item1}: expected={invalidAttribute.Item2}, actual={invalidAttribute.Item3}";
            if (wageTypeResult.IsInvalidResult())
            {
                // failed wage type result test
                failedWageTypeCount++;
                Log.Error(message);
            }
            else if (displayMode == TestDisplayMode.ShowAll)
            {
                // succeeded wage type result test
                Log.Debug(message);
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
                    if (wageTypeCustomResult.IsInvalidResult())
                    {
                        // failed wage type custom result test
                        failedWageTypeCount++;
                        Log.Error(message);
                    }
                    else if (displayMode == TestDisplayMode.ShowAll)
                    {
                        // succeeded wage type custom result test
                        ConsoleTool.DisplayInfoLine(message);
                    }
                }
            }
        }
        return failedWageTypeCount;
    }

    private static int DisplayPayrunResults(PayrollTestResult result, TestDisplayMode displayMode)
    {
        var failedPayrunResultCount = 0;
        foreach (var payrunTestResult in result.PayrunResults)
        {
            var invalidAttribute = payrunTestResult.FirstInvalidAttribute();
            var message = invalidAttribute == null ?
                $"-> Payrun result {payrunTestResult.ExpectedResult.Name}: expected={payrunTestResult.ExpectedResult.Value}, actual={payrunTestResult.ActualResult?.Value}" :
                $"-> Payrun result {payrunTestResult.ExpectedResult.Name} -> Attribute {invalidAttribute.Item1}: expected={invalidAttribute.Item2}, actual={invalidAttribute.Item3}";
            if (!payrunTestResult.IsValidResult())
            {
                failedPayrunResultCount++;
                Log.Error(message);
            }
            else if (displayMode == TestDisplayMode.ShowAll)
            {
                ConsoleTool.DisplayInfoLine(message);
            }
        }

        return failedPayrunResultCount;
    }

    private static void DisplayStatistics(string fileName, List<PayrollTestResult> results, List<TimeSpan> durations, int errorCount)
    {
        if (results.Count <= 1)
        {
            return;
        }

        // info
        ConsoleTool.DisplayNewLine();
        var title = "---------- Test statistics ----------";
        ConsoleTool.DisplayTextLine(title);
        ConsoleTool.DisplayTextLine($"File          {new FileInfo(fileName).Name}");
        ConsoleTool.DisplayTextLine($"Jobs          {results.Count}");
        var duration = durations.Aggregate(TimeSpan.Zero, (subtotal, t) => subtotal.Add(t));
        ConsoleTool.DisplayTextLine($"Duration      {duration.TotalSeconds:0.#} sec");

        // performance
        var fastestIndex = durations.IndexOf(durations.Min());
        var slowestIndex = durations.IndexOf(durations.Max());
        if (results.Count > 2)
        {
            if (fastestIndex >= 0)
            {
                ConsoleTool.DisplayTextLine($"Fastest       {durations[fastestIndex].TotalMilliseconds:#0} ms - {results[fastestIndex].PayrunJob.Name}");
            }
            if (slowestIndex >= 0)
            {
                ConsoleTool.DisplayTextLine($"Slowest       {durations[slowestIndex].TotalMilliseconds:#0} ms - {results[slowestIndex].PayrunJob.Name}");
            }
        }

        var average = duration.TotalMilliseconds / results.Count;
        ConsoleTool.DisplayTextLine($"Average       {average:#0} ms");
        if (results.Count > 2 && slowestIndex >= 0)
        {
            var slowestResult = results[slowestIndex];
            var slowestDuration = durations[slowestIndex];
            // remove the slowest job
            results.Remove(slowestResult);
            durations.Remove(slowestDuration);
            duration = durations.Aggregate(TimeSpan.Zero, (subtotal, t) => subtotal.Add(t));
            var average2nd = duration.TotalMilliseconds / results.Count;

            // show 2+ average on a difference of 20 percent
            var range = average / 5;
            if (Math.Abs(average - average2nd) > range)
            {
                ConsoleTool.DisplayTextLine($"Average 2+    {duration.TotalMilliseconds / results.Count:#0} ms");
            }
        }

        if (errorCount > 0)
        {
            ConsoleTool.DisplayErrorLine($"Errors        {errorCount}");
        }
        ConsoleTool.DisplayTextLine(new string('-', title.Length));
    }
}