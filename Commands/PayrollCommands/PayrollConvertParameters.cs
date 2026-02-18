using System;
using PayrollEngine.Client.Command;

namespace PayrollEngine.PayrollConsole.Commands.PayrollCommands;

/// <summary>
/// Payroll convert command parameters
/// </summary>
public class PayrollConvertParameters : ICommandParameters
{
    /// <summary>
    /// File name
    /// </summary>
    public string FileName { get; init; }

    /// <summary>
    /// DirectoryMode mode (default: top)
    /// </summary>
    public DirectoryMode DirectoryMode { get; private init; } = DirectoryMode.Top;

    /// <summary>
    /// Schema type (default: Auto)
    /// </summary>
    public SchemaType SchemaType { get; private init; } = SchemaType.Auto;

    /// <inheritdoc />
    public Type[] Toggles =>
    [
        typeof(DirectoryMode),
        typeof(SchemaType)
    ];

    /// <inheritdoc />
    public string Test() =>
        string.IsNullOrWhiteSpace(FileName) ? "Missing file name" : null;

    /// <summary>
    /// Parse command parameters
    /// </summary>
    /// <param name="parser">Parameter parser</param>
    public static PayrollConvertParameters ParserFrom(CommandLineParser parser) =>
        new()
        {
            FileName = parser.Get(2, nameof(FileName)),
            DirectoryMode = parser.GetEnumToggle(DirectoryMode.Top),
            SchemaType = parser.GetEnumToggle(SchemaType.Auto)
        };
}