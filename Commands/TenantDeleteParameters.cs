using System;
using PayrollEngine.Client.Command;

namespace PayrollEngine.PayrollConsole.Commands;

public class TenantDeleteParameters : ICommandParameters
{
    public string Tenant { get; init; }
    public ObjectDeleteMode DeleteMode { get; private init; } = ObjectDeleteMode.Delete;
    public Type[] Toggles =>
    [
        typeof(ObjectDeleteMode)
    ];

    public string Test() =>
        string.IsNullOrWhiteSpace(Tenant) ? "Missing tenant" : null;

    public static TenantDeleteParameters ParserFrom(CommandLineParser parser) =>
        new()
        {
            Tenant = parser.Get(2, nameof(Tenant)),
            DeleteMode = parser.GetEnumToggle(ObjectDeleteMode.Delete)
        };
}