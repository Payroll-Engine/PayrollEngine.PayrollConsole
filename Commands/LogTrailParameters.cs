using System;
using PayrollEngine.Client.Command;

namespace PayrollEngine.PayrollConsole.Commands;

public class LogTrailParameters : ICommandParameters
{
    public string Tenant { get; init; }
    public int Interval { get; init; } = 5;
    public Type[] Toggles => null;

    public string Test() =>
        string.IsNullOrWhiteSpace(Tenant) ? "Missing tenant" : null;

    public static LogTrailParameters ParserFrom(CommandLineParser parser) =>
        new()
        {
            Tenant = parser.Get(2, nameof(Tenant)),
            Interval = parser.GetInt(3, 5, nameof(Interval))
        };
}