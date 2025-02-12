using System;
using PayrollEngine.Client.Command;

namespace PayrollEngine.PayrollConsole.Commands;

public class ScriptPublishParameters : ICommandParameters
{
    public string SourceFile { get; init; }
    public string SourceScript { get; init; }
    public Type[] Toggles => null;

    public string Test() =>
        string.IsNullOrWhiteSpace(SourceFile) ? "Missing source file" : null;

    public static ScriptPublishParameters ParserFrom(CommandLineParser parser) =>
        new()
        {
            SourceFile = parser.Get(2, nameof(SourceFile)),
            SourceScript = parser.Get(3, nameof(SourceScript))
        };
}