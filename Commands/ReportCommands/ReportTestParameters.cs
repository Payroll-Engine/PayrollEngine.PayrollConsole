using System;
using PayrollEngine.Client.Command;
using PayrollEngine.Client.Test;

namespace PayrollEngine.PayrollConsole.Commands.ReportCommands;

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

    /// <summary>
    /// Test exit mode (default: error on fail).
    /// When set to OkOnFail, all test files are processed and the program exits with Ok
    /// even if tests fail — failures are still logged for collection in a log file.
    /// </summary>
    public TestExitMode ExitMode { get; private init; } = TestExitMode.ErrorOnFail;

    /// <inheritdoc />
    public Type[] Toggles =>
    [
        typeof(TestDisplayMode),
        typeof(TestPrecision),
        typeof(TestExitMode)
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
            Precision = parser.GetEnumToggle(TestPrecision.TestPrecision2),
            ExitMode = parser.GetEnumToggle(TestExitMode.ErrorOnFail)
        };
}
