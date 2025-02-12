using System;
using PayrollEngine.Client.Command;
using PayrollEngine.Client.Exchange;
using PayrollEngine.Client.Test;
using PayrollEngine.Client.Test.Payrun;

namespace PayrollEngine.PayrollConsole.Commands;

public class PayrunTestParameters : ICommandParameters
{
    public string FileMask { get; init; }
    public string Owner { get; init; }
    public DataImportMode ImportMode { get; private init; } = DataImportMode.Bulk;
    public TestRunMode RunMode { get; private init; } = TestRunMode.RunTests;
    public TestDisplayMode DisplayMode { get; private init; } = TestDisplayMode.ShowFailed;
    public TestResultMode ResultMode { get; private init; } = TestResultMode.CleanTest;
    public TestPrecision Precision { get; private init; } = TestPrecision.TestPrecision2;

    public Type[] Toggles =>
    [
        typeof(DataImportMode),
        typeof(TestRunMode),
        typeof(TestDisplayMode),
        typeof(TestResultMode),
        typeof(TestPrecision)
    ];

    public string Test() =>
        string.IsNullOrWhiteSpace(FileMask) ? "Missing file name or file mask" : null;

    public static PayrunTestParameters ParserFrom(CommandLineParser parser) =>
        new()
        {
            FileMask = parser.Get(2, nameof(FileMask)),
            Owner = parser.Get(3, nameof(Owner)),
            ImportMode = parser.GetEnumToggle(DataImportMode.Bulk),
            RunMode = parser.GetEnumToggle(TestRunMode.RunTests),
            DisplayMode = parser.GetEnumToggle(TestDisplayMode.ShowFailed),
            ResultMode = parser.GetEnumToggle(TestResultMode.CleanTest),
            Precision = parser.GetEnumToggle(TestPrecision.TestPrecision2)
        };
}