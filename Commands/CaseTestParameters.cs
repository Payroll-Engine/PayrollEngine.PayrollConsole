using System;
using PayrollEngine.Client.Command;
using PayrollEngine.Client.Test;

namespace PayrollEngine.PayrollConsole.Commands;

public class CaseTestParameters : ICommandParameters
{
    public string FileMask { get; init; }
    public TestDisplayMode DisplayMode { get; private init; } = TestDisplayMode.ShowFailed;
    public TestPrecision Precision { get; private init; } = TestPrecision.TestPrecision2;

    public Type[] Toggles =>
    [
        typeof(TestDisplayMode),
        typeof(TestPrecision)
    ];

    public string Test() => null;

    public static CaseTestParameters ParserFrom(CommandLineParser parser) =>
        new()
        {
            FileMask = parser.Get(2, nameof(FileMask)),
            DisplayMode = parser.GetEnumToggle(TestDisplayMode.ShowFailed),
            Precision = parser.GetEnumToggle(TestPrecision.TestPrecision2)
        };
}