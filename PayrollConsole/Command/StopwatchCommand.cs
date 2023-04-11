using System;
using System.Globalization;
using PayrollEngine.PayrollConsole.Shared;

namespace PayrollEngine.PayrollConsole.Command;

internal sealed class StopwatchCommand : CommandBase
{
    /// <summary>Process the variable</summary>
    /// <param name="variableName">The variable name</param>
    /// <param name="mode">The stopwatch mode</param>
    internal ProgramExitCode Stopwatch(string variableName, StopwatchMode mode = StopwatchMode.WatchView)
    {
        if (string.IsNullOrWhiteSpace(variableName))
        {
            throw new ArgumentException(nameof(variableName));
        }

        try
        {
            var now = DateTime.Now;
            if (mode == StopwatchMode.WatchStart)
            {
                ConsoleTool.DisplayNewLine();
                ConsoleTool.DisplayInfo($"Starting stopwatch {variableName}...");
                // set current date as stopwatch start
                SetUserVariable(variableName, now.ToUtcString(CultureInfo.InvariantCulture));
                ConsoleTool.DisplayInfoLine("done.");
                ConsoleTool.DisplayNewLine();
                ConsoleTool.DisplaySuccessLine($"Stopwatch {variableName} (started): {now}");
            }
            else
            {
                var value = GetUserVariable(variableName);
                if (string.IsNullOrWhiteSpace(value))
                {
                    ConsoleTool.DisplayInfoLine($"Empty stopwatch {variableName}");
                }
                else
                {
                    // parse environment variable
                    if (!DateTime.TryParse(value, CultureInfo.InvariantCulture,
                            DateTimeStyles.None, out var stopwatchStart))
                    {
                        ConsoleTool.DisplayErrorLine($"Invalid stopwatch {variableName}: {value}");
                    }

                    // elapsed time in readable form
                    var elapsedTime = (now - stopwatchStart).ToReadableString();
                    switch (mode)
                    {
                        case StopwatchMode.WatchView:
                            ConsoleTool.DisplayNewLine();
                            ConsoleTool.DisplaySuccessLine($"Stopwatch {variableName} (running): {elapsedTime}");
                            break;
                        case StopwatchMode.WatchStop:
                            ConsoleTool.DisplayNewLine();
                            ConsoleTool.DisplaySuccessLine($"Stopwatch {variableName} (stopped): {elapsedTime}");
                            // reset the stopwatch start variable
                            SetUserVariable(variableName, null);
                            break;
                    }

                }
            }
            return ProgramExitCode.Ok;
        }
        catch (Exception exception)
        {
            Log.Error(exception, exception.GetBaseMessage());
            ConsoleTool.DisplayErrorLine($"Stopwatch failed: {exception.GetBaseMessage()}");
            return ProgramExitCode.GenericError;
        }
    }

    private static string GetUserVariable(string variableName) =>
        Environment.GetEnvironmentVariable(variableName, EnvironmentVariableTarget.User);

    private static void SetUserVariable(string variableName, string variableValue) =>
        Environment.SetEnvironmentVariable(variableName, variableValue, EnvironmentVariableTarget.User);

    /// <summary>Show the application help</summary>
    internal static void ShowHelp()
    {
        ConsoleTool.DisplayTitleLine("- Stopwatch");
        ConsoleTool.DisplayTextLine("      Time measure based on environment user variable");
        ConsoleTool.DisplayTextLine("      Arguments:");
        ConsoleTool.DisplayTextLine("          1. Stopwatch name");
        ConsoleTool.DisplayTextLine("      Toggles:");
        ConsoleTool.DisplayTextLine("          stopwatch mode: /watchstart, /watchstop or /watchview (default watchview)");
        ConsoleTool.DisplayTextLine("      Examples:");
        ConsoleTool.DisplayTextLine("          Stopwatch MyStopwatch /watchstart");
        ConsoleTool.DisplayTextLine("          Stopwatch MyStopwatch /watchstop");
    }
}