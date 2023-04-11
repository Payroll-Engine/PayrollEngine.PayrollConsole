using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using PayrollEngine.Client;
using PayrollEngine.Client.Test;
using PayrollEngine.PayrollConsole.Shared;

namespace PayrollEngine.PayrollConsole.Command;

internal abstract class TestCommandBase : HttpCommandBase
{
    protected TestPrecision TestPrecision { get; }

    protected TestCommandBase(PayrollHttpClient httpClient, TestPrecision testPrecision) :
        base(httpClient)
    {
        TestPrecision = testPrecision;
    }

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

    protected static string GetLocalFileName(string fileName)
    {
        var directoryInfo = new FileInfo(Environment.CurrentDirectory);
        var fileInfo = new FileInfo(fileName);
        var localFileName = fileInfo.FullName;
        // reduce full paths to the parent folder
        if (localFileName.StartsWith(directoryInfo.FullName))
        {
            localFileName = $"{directoryInfo.Name}{localFileName.Substring(directoryInfo.FullName.Length)}";
        }
        return localFileName;
    }
    
    protected static void DisplayTestResults<TResult>(string fileName, TestDisplayMode displayMode, 
        ICollection<TResult> results)
        where TResult : ScriptTestResultBase
    {
        ConsoleTool.DisplayTextLine("Testing result values...");
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

        // test without failures
        if (results.Count(x => x.Failed) == 0)
        {
            ConsoleTool.DisplaySuccessLine("-> no errors");
            return;
        }

        // test with failures
        ConsoleTool.DisplayErrorLine($"Test {GetLocalFileName(fileName)} failed");
    }
}