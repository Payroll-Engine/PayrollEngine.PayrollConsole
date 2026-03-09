using System;
using PayrollEngine.Client.Command;

namespace PayrollEngine.PayrollConsole.Commands.LoadTestCommands;

/// <summary>Payrun load test parameters</summary>
public class PayrunLoadTestParameters : ICommandParameters
{
    /// <summary>Path to Payrun-Invocation exchange file</summary>
    public string InvocationFile { get; init; }

    /// <summary>Expected employee count (for report)</summary>
    public int EmployeeCount { get; init; }

    /// <summary>Number of repetitions (default: 3, median used)</summary>
    public int Repetitions { get; init; } = 3;

    /// <summary>Output CSV path for results</summary>
    public string ResultFile { get; init; }

    /// <inheritdoc />
    public Type[] Toggles => null;

    /// <inheritdoc />
    public string Test()
    {
        if (string.IsNullOrWhiteSpace(InvocationFile))
        {
            return "Missing invocation file";
        }
        if (EmployeeCount <= 0)
        {
            return "Invalid employee count";
        }
        return null;
    }

    /// <summary>Parse command parameters</summary>
    public static PayrunLoadTestParameters ParserFrom(CommandLineParser parser) =>
        new()
        {
            InvocationFile = parser.Get(2, nameof(InvocationFile)),
            EmployeeCount = int.TryParse(parser.Get(3, nameof(EmployeeCount)), out var c) ? c : 0,
            Repetitions = int.TryParse(parser.Get(4, nameof(Repetitions)), out var r) && r > 0 ? r : 3,
            ResultFile = parser.Get(5, nameof(ResultFile)) ?? "LoadTestResults.csv"
        };
}
