using System;
using PayrollEngine.Client.Command;

namespace PayrollEngine.PayrollConsole.Commands;

public class StopwatchParameters : ICommandParameters
{
    public string VariableName { get; init; }
    public StopwatchMode StopwatchMode { get; private init; } = StopwatchMode.WatchView;
    public Type[] Toggles =>
    [
        typeof(StopwatchMode)
    ];

    public string Test() =>
        string.IsNullOrWhiteSpace(VariableName) ? "Missing stopwatch variable name" : null;

    public static StopwatchParameters ParserFrom(CommandLineParser parser) =>
        new()
        {
            VariableName = parser.Get(2, nameof(VariableName)),
            StopwatchMode = parser.GetEnumToggle(StopwatchMode.WatchView)
        };
}