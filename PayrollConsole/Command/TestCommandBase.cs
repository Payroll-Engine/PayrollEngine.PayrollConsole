using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using PayrollEngine.Client;
using PayrollEngine.Client.Test;
using PayrollEngine.PayrollConsole.Shared;

namespace PayrollEngine.PayrollConsole.Command;

internal abstract class TestCommandBase(PayrollHttpClient httpClient, TestPrecision testPrecision) : HttpCommandBase(httpClient)
{
    protected TestPrecision TestPrecision { get; } = testPrecision;

    protected static List<string> GetTestFileNames(string fileMask)
    {
        if (string.IsNullOrWhiteSpace(fileMask))
        {
            throw new ArgumentException(nameof(fileMask));
        }

        var files = new List<string>();
        // single file
        if (File.Exists(fileMask))
        {
            files.Add(fileMask);
        }
        else
        {
            // multiple files
            files.AddRange(new DirectoryInfo(Directory.GetCurrentDirectory()).GetFiles(fileMask).Select(x => x.Name));
        }
        return files;
    }
    
    protected static void DisplayTestResults<TResult>(TestDisplayMode displayMode, ICollection<TResult> results)
        where TResult : ScriptTestResultBase
    {
        ConsoleTool.DisplayTextLine("Test results...");
        foreach (var testResult in results)
        {
            // hidden succeeded test
            if (!testResult.Failed && displayMode != TestDisplayMode.ShowAll)
            {
                continue;
            }

            // failed test
            if (testResult.Failed)
            {
                var message = $"-> Test {testResult.TestName} failed";
                if (!string.IsNullOrWhiteSpace(testResult.Message))
                {
                    message += $" ({testResult.Message})";
                }
                if (testResult.ErrorCode != 0)
                {
                    message += $" [{testResult.ErrorCode}]";
                }
                message += ")";
                Log.Error(message);
            }
            else
            {
                // visible succeeded test
                var message = $"-> Test {testResult.TestName} succeeded";
                if (!string.IsNullOrWhiteSpace(testResult.Message))
                {
                    message += $" ({testResult.Message})";
                }
                Log.Debug(message);
            }
        }

        // summary
        var totalCount = results.Count;
        var failedCount = results.Count(x => x.Failed);
        var successCount = totalCount - failedCount;
        // success
        if (successCount > 0)
        {
            var successMessage = $"Passed tests: {successCount}";
            if (failedCount > 0)
            {
                ConsoleTool.DisplayInfoLine(successMessage);
            }
            else
            {
                ConsoleTool.DisplaySuccessLine(successMessage);
            }
        }
        // failed
        if (failedCount > 0)
        { 
            ConsoleTool.DisplayErrorLine($"Failed tests: {failedCount}");
        }
    }
}