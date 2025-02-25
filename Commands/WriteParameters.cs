using System;
using PayrollEngine.Client.Command;

namespace PayrollEngine.PayrollConsole.Commands;

/// <summary>
/// Write command parameters
/// </summary>
public class WriteParameters : ICommandParameters
{
    /// <summary>
    /// Write text
    /// </summary>
    public string Text { get; init; }

    /// <summary>
    /// Write mode (default: console)
    /// </summary>
    public WriteMode WriteMode { get; private init; } = WriteMode.Console;

    /// <summary>
    /// Console write mode (default: normal)
    /// </summary>
    public ConsoleWriteMode ConsoleWriteMode { get; private init; } = ConsoleWriteMode.ConsoleNormal;

    /// <summary>
    /// Log write mode (default: info)
    /// </summary>
    public LogWriteMode LogWriteMode { get; private init; } = LogWriteMode.LogInfo;

    /// <inheritdoc />
    public Type[] Toggles =>
    [
        typeof(WriteMode),
        typeof(ConsoleWriteMode),
        typeof(LogWriteMode)
    ];

    /// <inheritdoc />
    public string Test() =>
        string.IsNullOrWhiteSpace(Text) ? "Missing write text" : null;

    /// <summary>
    /// Parse command parameters
    /// </summary>
    /// <param name="parser">Parameter parser</param>
    public static WriteParameters ParserFrom(CommandLineParser parser) =>
        new()
        {
            Text = parser.Get(2, nameof(Text)),
            WriteMode = parser.GetEnumToggle(WriteMode.Console),
            ConsoleWriteMode = parser.GetEnumToggle(ConsoleWriteMode.ConsoleNormal),
            LogWriteMode = parser.GetEnumToggle(LogWriteMode.LogInfo)
        };
}