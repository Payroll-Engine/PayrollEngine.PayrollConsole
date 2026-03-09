using System;

namespace PayrollEngine.PayrollConsole.Commands.LoadTestCommands;

/// <summary>Single load test run result</summary>
internal sealed class PayrunLoadTestResult
{
    /// <summary>Timestamp of the measurement</summary>
    internal DateTimeOffset Timestamp { get; init; }

    /// <summary>Run number within repetitions</summary>
    internal int RunNumber { get; init; }

    /// <summary>Payrun period start</summary>
    internal DateTime Period { get; init; }

    /// <summary>Number of employees in this run</summary>
    internal int EmployeeCount { get; init; }

    /// <summary>Client-side duration including HTTP overhead (ms)</summary>
    internal long ClientDurationMs { get; init; }

    /// <summary>Server-side job duration from PayrunJob (ms)</summary>
    internal long ServerJobDurationMs { get; init; }

    /// <summary>Server-side average per employee (ms)</summary>
    internal double ServerAvgMsPerEmployee { get; init; }
}
