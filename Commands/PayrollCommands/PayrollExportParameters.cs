using System;
using PayrollEngine.Client.Command;

namespace PayrollEngine.PayrollConsole.Commands.PayrollCommands;

/// <summary>
/// Payroll export command parameters
/// </summary>
public class PayrollExportParameters : ICommandParameters
{
    /// <summary>
    /// Tenant
    /// </summary>
    public string Tenant { get; init; }

    /// <summary>
    /// Target file name
    /// </summary>
    public string TargetFileName { get; init; }

    /// <summary>
    /// Options file name
    /// </summary>
    public string OptionsFileName { get; init; }

    /// <summary>
    /// Export namespace
    /// </summary>
    public string Namespace { get; init; }
  
    /// <inheritdoc />
    public Type[] Toggles => null;

    /// <inheritdoc />
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

    /// <summary>
    /// Parse command parameters
    /// </summary>
    /// <param name="parser">Parameter parser</param>
    public static PayrollExportParameters ParserFrom(CommandLineParser parser) =>
        new()
        {
            Tenant = parser.Get(2, nameof(Tenant)),
            TargetFileName = parser.Get(3, nameof(TargetFileName)),
            OptionsFileName = parser.Get(4, nameof(OptionsFileName)),
            Namespace = parser.Get(5, nameof(Namespace))
        };
}