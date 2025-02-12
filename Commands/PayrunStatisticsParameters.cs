using System;
using PayrollEngine.Client.Command;

namespace PayrollEngine.PayrollConsole.Commands;

public class PayrunStatisticsParameters : ICommandParameters
{
    public string Tenant { get; init; }
    public int CreatedSinceMinutes { get; set; } = 30;
    public Type[] Toggles => null;

    public string Test() => null;

    public static PayrunStatisticsParameters ParserFrom(CommandLineParser parser) =>
        new()
        {
            Tenant = parser.Get(2, nameof(Tenant)),
            CreatedSinceMinutes = parser.GetInt(3, 30, nameof(CreatedSinceMinutes))
        };
}