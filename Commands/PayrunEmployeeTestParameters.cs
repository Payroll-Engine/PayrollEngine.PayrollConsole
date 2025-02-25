using System;
using PayrollEngine.Client.Test;
using PayrollEngine.Client.Command;
using PayrollEngine.Client.Test.Payrun;

namespace PayrollEngine.PayrollConsole.Commands;

/// <summary>
/// Payrun employee test command parameters
/// </summary>
public class PayrunEmployeeTestParameters : ICommandParameters
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
    /// Test mode (default: insert employee)
    /// </summary>
    public EmployeeTestMode TestMode { get; private init; } = EmployeeTestMode.InsertEmployee;

    /// <summary>
    /// Test run mode (default: run tests)
    /// </summary>
    public TestRunMode RunMode { get; private init; } = TestRunMode.RunTests;

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
        typeof(EmployeeTestMode),
        typeof(TestRunMode),
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
    public static PayrunEmployeeTestParameters ParserFrom(CommandLineParser parser) =>
        new()
        {
            FileMask = parser.Get(2, nameof(FileMask)),
            Owner = parser.Get(3, nameof(Owner)),
            TestMode = parser.GetEnumToggle(EmployeeTestMode.InsertEmployee),
            RunMode = parser.GetEnumToggle(TestRunMode.RunTests),
            DisplayMode = parser.GetEnumToggle(TestDisplayMode.ShowFailed),
            Precision = parser.GetEnumToggle(TestPrecision.TestPrecision2)
        };
}