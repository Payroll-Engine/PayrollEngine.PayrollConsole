using System;
using System.Globalization;
using System.Threading.Tasks;
using PayrollEngine.Client.Command;

namespace PayrollEngine.PayrollConsole.Commands.DiagnosticsCommands;

/// <summary>
/// Stopwatch command
/// </summary>
[Command("Stopwatch")]
// ReSharper disable once UnusedType.Global
internal sealed class StopwatchCommand : CommandBase<StopwatchParameters>
{
    /// <summary>Process the variable</summary>
    /// <param name="context">Command execution context</param>
    /// <param name="parameters">Command parameters</param>
    /// <returns>Program exit ok, 0 on success</returns>
    protected override Task<int> Execute(CommandContext context, StopwatchParameters parameters)
    {
        try
        {
            var now = DateTime.Now;
            if (parameters.StopwatchMode == StopwatchMode.WatchStart)
            {
                context.Console.DisplayNewLine();
                context.Console.DisplayInfo($"Starting stopwatch {parameters.VariableName}...");
                // set current date as stopwatch start
                SetUserVariable(parameters.VariableName, now.ToUtcString(CultureInfo.InvariantCulture));
                context.Console.DisplayInfoLine("done.");
                context.Console.DisplayNewLine();
                context.Console.DisplaySuccessLine($"Stopwatch {parameters.VariableName} (started): {now}");
            }
            else
            {
                var value = GetUserVariable(parameters.VariableName);
                if (string.IsNullOrWhiteSpace(value))
                {
                    context.Console.DisplayInfoLine($"Empty stopwatch {parameters.VariableName}");
                }
                else
                {
                    // parse environment variable
                    if (!DateTime.TryParse(value, CultureInfo.InvariantCulture,
                            DateTimeStyles.None, out var stopwatchStart))
                    {
                        context.Console.DisplayErrorLine($"Invalid stopwatch {parameters.VariableName}: {value}");
                    }

                    // elapsed time in readable form
                    var elapsedTime = (now - stopwatchStart).ToReadableString();
                    switch (parameters.StopwatchMode)
                    {
                        case StopwatchMode.WatchView:
                            context.Console.DisplayNewLine();
                            context.Console.DisplaySuccessLine($"Stopwatch {parameters.VariableName} (running): {elapsedTime}");
                            break;
                        case StopwatchMode.WatchStop:
                            context.Console.DisplayNewLine();
                            context.Console.DisplaySuccessLine($"Stopwatch {parameters.VariableName} (stopped): {elapsedTime}");
                            // reset the stopwatch start variable
                            SetUserVariable(parameters.VariableName, null);
                            break;
                    }

                }
            }
            return Task.FromResult((int)ProgramExitCode.Ok);
        }
        catch (Exception exception)
        {
            ProcessError(context.Console, exception);
            return Task.FromResult((int)ProgramExitCode.GenericError);
        }
    }

    private static string GetUserVariable(string variableName) =>
        Environment.GetEnvironmentVariable(variableName, EnvironmentVariableTarget.User);

    private static void SetUserVariable(string variableName, string variableValue) =>
        Environment.SetEnvironmentVariable(variableName, variableValue, EnvironmentVariableTarget.User);
    
    /// <inheritdoc />
    public override bool BackendCommand => false;

    /// <inheritdoc />
    public override ICommandParameters GetParameters(CommandLineParser parser) =>
        StopwatchParameters.ParserFrom(parser);

    /// <inheritdoc />
    public override void ShowHelp(ICommandConsole console)
    {
        console.DisplayTitleLine("- Stopwatch");
        console.DisplayTextLine("      Stopwatch based on environment user variable");
        console.DisplayTextLine("      Arguments:");
        console.DisplayTextLine("          1. Stopwatch variable name [VariableName]");
        console.DisplayTextLine("      Toggles:");
        console.DisplayTextLine("          stopwatch mode: /watchstart, /watchstop or /watchview (default watchview)");
        console.DisplayTextLine("      Examples:");
        console.DisplayTextLine("          Stopwatch MyStopwatch /watchstart");
        console.DisplayTextLine("          Stopwatch MyStopwatch /watchstop");
    }
}