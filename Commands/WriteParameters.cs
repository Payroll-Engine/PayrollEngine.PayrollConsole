using System;
using PayrollEngine.Client.Command;

namespace PayrollEngine.PayrollConsole.Commands;

public class WriteParameters : ICommandParameters
{
    public string Text { get; init; }
    public WriteMode WriteMode { get; private init; } = WriteMode.Console;
    public ConsoleWriteMode ConsoleWriteMode { get; private init; } = ConsoleWriteMode.ConsoleNormal;
    public LogWriteMode LogWriteMode { get; private init; } = LogWriteMode.LogInfo;
    public Type[] Toggles =>
    [
        typeof(WriteMode),
        typeof(ConsoleWriteMode),
        typeof(LogWriteMode)
    ];

    public string Test() =>
        string.IsNullOrWhiteSpace(Text) ? "Missing write text" : null;

    public static WriteParameters ParserFrom(CommandLineParser parser) =>
        new()
        {
            Text = parser.Get(2, nameof(Text)),
            WriteMode = parser.GetEnumToggle(WriteMode.Console),
            ConsoleWriteMode = parser.GetEnumToggle(ConsoleWriteMode.ConsoleNormal),
            LogWriteMode = parser.GetEnumToggle(LogWriteMode.LogInfo)
        };
}