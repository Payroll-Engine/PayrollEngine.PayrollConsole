using System;
using PayrollEngine.Client.Command;

namespace PayrollEngine.PayrollConsole.Commands;

public class HelpParameters : ICommandParameters
{
    public string Command { get; init; }
    public Type[] Toggles => null;

    public string Test() => null;

    public static HelpParameters ParserFrom(CommandLineParser parser) =>
        new()
        {
            Command = parser.Get(2, nameof(Command))
        };
}