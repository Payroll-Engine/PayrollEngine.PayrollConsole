using System;
using PayrollEngine.Client.Command;
using PayrollEngine.Client.Test;
using PayrollEngine.Client.Test.Payrun;

namespace PayrollEngine.PayrollConsole.Commands;

public class PayrunEmployeeTestParameters : ICommandParameters
{
    public string FileMask { get; init; }
    public string Owner { get; init; }
    public EmployeeTestMode TestMode { get; private init; } = EmployeeTestMode.InsertEmployee;
    public TestRunMode RunMode { get; private init; } = TestRunMode.RunTests;
    public TestDisplayMode DisplayMode { get; private init; } = TestDisplayMode.ShowFailed;
    public TestPrecision Precision { get; private init; } = TestPrecision.TestPrecision2;

    public Type[] Toggles =>
    [
        typeof(EmployeeTestMode),
        typeof(TestRunMode),
        typeof(TestDisplayMode),
        typeof(TestPrecision)
    ];

    public string Test() =>
        string.IsNullOrWhiteSpace(FileMask) ? "Missing file name or file mask" : null;

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