using System;
using PayrollEngine.Client.Command;

namespace PayrollEngine.PayrollConsole.Commands;

public class ActionReportParameters : ICommandParameters
{
    public string FileName { get; init; }
    public Type[] Toggles => null;

    public string Test() =>
        string.IsNullOrWhiteSpace(FileName) ? "Missing file name" : null;

    public static ActionReportParameters ParserFrom(CommandLineParser parser) =>
        new()
        {
            FileName = parser.Get(2, nameof(FileName))
        };
}