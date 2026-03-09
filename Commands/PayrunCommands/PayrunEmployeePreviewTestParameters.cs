using System;
using PayrollEngine.Client.Command;
using PayrollEngine.Client.Test;

namespace PayrollEngine.PayrollConsole.Commands.PayrunCommands;

/// <summary>
/// Payrun employee preview test command parameters
/// </summary>
public class PayrunEmployeePreviewTestParameters : ICommandParameters
{
    /// <summary>
    /// File mask
    /// </summary>
    public string FileMask { get; init; }

    /// <summary>
    /// Test owner
    /// </summary>
    public string Owner { get; init; }

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
    public string Test() =>
        string.IsNullOrWhiteSpace(FileMask) ? "Missing file name or file mask" : null;

    /// <summary>
    /// Parse command parameters
    /// </summary>
    /// <param name="parser">Parameter parser</param>
    public static PayrunEmployeePreviewTestParameters ParserFrom(CommandLineParser parser) =>
        new()
        {
            FileMask = parser.Get(2, nameof(FileMask)),
            Owner = parser.Get(3, nameof(Owner)),
            DisplayMode = parser.GetEnumToggle(TestDisplayMode.ShowFailed),
            Precision = parser.GetEnumToggle(TestPrecision.TestPrecision2)
        };
}
