using System;
using PayrollEngine.Client.Command;

namespace PayrollEngine.PayrollConsole.Commands;

public class PayrunJobDeleteParameters : ICommandParameters
{
    public string Tenant { get; init; }
    public Type[] Toggles => null;

    public string Test() =>
        string.IsNullOrWhiteSpace(Tenant) ? "Missing tenant" : null;

    public static PayrunJobDeleteParameters ParserFrom(CommandLineParser parser) =>
        new()
        {
            Tenant = parser.Get(2, nameof(Tenant))
        };
}