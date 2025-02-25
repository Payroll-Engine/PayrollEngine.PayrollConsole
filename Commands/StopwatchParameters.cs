using System;
using PayrollEngine.Client.Command;

namespace PayrollEngine.PayrollConsole.Commands;

/// <summary>
/// Stopwatch command parameters
/// </summary>
public class StopwatchParameters : ICommandParameters
{
    /// <summary>
    /// Variable name
    /// </summary>
    public string VariableName { get; init; }

    /// <summary>
    /// Stopwatch mode (default: watch view)
    /// </summary>
    public StopwatchMode StopwatchMode { get; private init; } = StopwatchMode.WatchView;

    /// <inheritdoc />
    public Type[] Toggles =>
    [
        typeof(StopwatchMode)
    ];

    /// <inheritdoc />
    public string Test() =>
        string.IsNullOrWhiteSpace(VariableName) ? "Missing stopwatch variable name" : null;

    /// <summary>
    /// Parse command parameters
    /// </summary>
    /// <param name="parser">Parameter parser</param>
    public static StopwatchParameters ParserFrom(CommandLineParser parser) =>
        new()
        {
            VariableName = parser.Get(2, nameof(VariableName)),
            StopwatchMode = parser.GetEnumToggle(StopwatchMode.WatchView)
        };
}