using System;
using PayrollEngine.Client.Command;

namespace PayrollEngine.PayrollConsole.Commands;

public class PayrollExportParameters : ICommandParameters
{
    public string Tenant { get; init; }
    public string TargetFileName { get; init; }
    public string OptionsFileName { get; init; }
    public string Namespace { get; init; }
    public Type[] Toggles => null;

    public string Test()
    {
        if (string.IsNullOrWhiteSpace(Tenant))
        {
            return "Missing tenant";
        }
        if (string.IsNullOrWhiteSpace(TargetFileName))
        {
            return "Missing target file name";
        }
        return null;
    }

    public static PayrollExportParameters ParserFrom(CommandLineParser parser) =>
        new()
        {
            Tenant = parser.Get(2, nameof(Tenant)),
            TargetFileName = parser.Get(3, nameof(TargetFileName)),
            OptionsFileName = parser.Get(4, nameof(OptionsFileName)),
            Namespace = parser.Get(5, nameof(Namespace))
        };
}