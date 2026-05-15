using System;
using PayrollEngine.Client.Command;
using PayrollEngine.Client.Exchange;
using PayrollEngine.Client.Test;
using PayrollEngine.Client.Test.Payrun;

namespace PayrollEngine.PayrollConsole.Commands.PayrunCommands;

/// <summary>
/// Payrun test command parameters
/// </summary>
public class PayrunTestParameters : ICommandParameters
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
    /// Test import mode (default: bulk)
    /// </summary>
    public DataImportMode ImportMode { get; private init; } = DataImportMode.Bulk;

    /// <summary>
    /// Test run mode (default: run tests)
    /// </summary>
    public TestRunMode RunMode { get; private init; } = TestRunMode.RunTests;

    /// <summary>
    /// Test display mode (default: show failed)
    /// </summary>
    public TestDisplayMode DisplayMode { get; private init; } = TestDisplayMode.ShowFailed;

    /// <summary>
    /// Test result mode (default: clean test)
    /// </summary>
    public TestResultMode ResultMode { get; private init; } = TestResultMode.CleanTest;

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
        typeof(DataImportMode),
        typeof(TestRunMode),
        typeof(TestDisplayMode),
        typeof(TestResultMode),
        typeof(TestPrecision),
        typeof(TestExitMode)
    ];

    /// <inheritdoc />
    public string Test() =>
        string.IsNullOrWhiteSpace(FileMask) ? "Missing file name or file mask" : null;

    /// <summary>
    /// Parse command parameters
    /// </summary>
    /// <param name="parser">Parameter parser</param>
    public static PayrunTestParameters ParserFrom(CommandLineParser parser) =>
        new()
        {
            FileMask = parser.Get(2, nameof(FileMask)),
            Owner = parser.Get(3, nameof(Owner)),
            ImportMode = parser.GetEnumToggle(DataImportMode.Bulk),
            RunMode = parser.GetEnumToggle(TestRunMode.RunTests),
            DisplayMode = parser.GetEnumToggle(TestDisplayMode.ShowFailed),
            ResultMode = parser.GetEnumToggle(TestResultMode.CleanTest),
            Precision = parser.GetEnumToggle(TestPrecision.TestPrecision2),
            ExitMode = parser.GetEnumToggle(TestExitMode.ErrorOnFail)
        };
}
