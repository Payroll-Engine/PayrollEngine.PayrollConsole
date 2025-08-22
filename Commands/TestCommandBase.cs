using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using PayrollEngine.Client.Test;
using PayrollEngine.Client.Command;

namespace PayrollEngine.PayrollConsole.Commands;

/// <summary>
/// Test command base
/// </summary>
internal abstract class TestCommandBase : CommandBase
{
    /// <summary>
    /// Get test file names
    /// </summary>
    /// <param name="fileMask">File mask</param>
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

    /// <summary>
    /// Display test results
    /// </summary>
    /// <param name="logger">Logger</param>
    /// <param name="console">Console</param>
    /// <param name="displayMode">Display mode</param>
    /// <param name="results">Test results</param>
    protected static void DisplayTestResults<TResult>(ILogger logger, ICommandConsole console,
        TestDisplayMode displayMode, ICollection<TResult> results)
        where TResult : ScriptTestResultBase
    {
        console.DisplayTextLine("Test results...");
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
                logger.Error(message);
            }
            else
            {
                // visible succeeded test
                var message = $"-> Test {testResult.TestName} succeeded";
                if (!string.IsNullOrWhiteSpace(testResult.Message))
                {
                    message += $" ({testResult.Message})";
                }
                logger.Debug(message);
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
    }
}