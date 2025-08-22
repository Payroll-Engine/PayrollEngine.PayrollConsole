using System;
using PayrollEngine.Client.Test;
using PayrollEngine.Client.Command;

namespace PayrollEngine.PayrollConsole.Commands.CaseCommands;

/// <summary>
/// Case test command parameter
/// </summary>
public class CaseTestParameters : ICommandParameters
{
    /// <summary>
    /// File mask
    /// </summary>
    public string FileMask { get; init; }

    /// <summary>
    /// Display mode
    /// </summary>
    public TestDisplayMode DisplayMode { get; private init; } = TestDisplayMode.ShowFailed;

    /// <summary>
    /// Test precision (default: 2)
    /// </summary>
    public TestPrecision Precision { get; private init; } = TestPrecision.TestPrecision2;

    /// <inheritdoc />
    public Type[] Toggles =>
    [
        typeof(TestDisplayMode),
        typeof(TestPrecision)
    ];

    /// <inheritdoc />
    public string Test() => null;

    /// <summary>
    /// Parse command parameters
    /// </summary>
    /// <param name="parser">Parameter parser</param>
    public static CaseTestParameters ParserFrom(CommandLineParser parser) =>
        new()
        {
            FileMask = parser.Get(2, nameof(FileMask)),
            DisplayMode = parser.GetEnumToggle(TestDisplayMode.ShowFailed),
            Precision = parser.GetEnumToggle(TestPrecision.TestPrecision2)
        };
}