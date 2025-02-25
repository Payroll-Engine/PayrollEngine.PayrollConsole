using System;
using PayrollEngine.Client.Command;
using PayrollEngine.Client.Test;

namespace PayrollEngine.PayrollConsole.Commands;

/// <summary>
/// Report test command parameters
/// </summary>
public class ReportTestParameters : ICommandParameters
{
    /// <summary>
    /// File mask
    /// </summary>
    public string FileMask { get; init; }

    /// <summary>
    /// Test display mode (default: show failed)
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
    public static ReportTestParameters ParserFrom(CommandLineParser parser) =>
        new()
        {
            FileMask = parser.Get(2, nameof(FileMask)),
            DisplayMode = parser.GetEnumToggle(TestDisplayMode.ShowFailed),
            Precision = parser.GetEnumToggle(TestPrecision.TestPrecision2)
        };
}