using System;
using PayrollEngine.Client.Command;

namespace PayrollEngine.PayrollConsole.Commands;

public class PayrunRebuildParameters : ICommandParameters
{
    public string Tenant { get; init; }
    public string Payrun { get; init; }
    public Type[] Toggles => null;

    public string Test()
    {
        if (string.IsNullOrWhiteSpace(Tenant))
        {
            return "Missing tenant";
        }
        return string.IsNullOrWhiteSpace(Payrun) ? "Missing payrun name" : null;
    }
    
    public static PayrunRebuildParameters ParserFrom(CommandLineParser parser) =>
        new()
        {
            Tenant = parser.Get(2, nameof(Tenant)),
            Payrun = parser.Get(3, nameof(Payrun))
        };
}