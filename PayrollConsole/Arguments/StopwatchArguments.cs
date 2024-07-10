using System;
using PayrollEngine.Client;
using PayrollEngine.PayrollConsole.Shared;

namespace PayrollEngine.PayrollConsole.Arguments;

public static class StopwatchArguments
{
    public static string VariableName =>
        ConsoleArguments.GetMember(typeof(StopwatchArguments), 2);

    public static StopwatchMode StopwatchMode(StopwatchMode defaultValue = Shared.StopwatchMode.WatchView) =>
        ConsoleArguments.GetEnumToggle(defaultValue);

    public static Type[] Toggles =>
    [
        typeof(StopwatchMode)
    ];

    public static string TestArguments() =>
        string.IsNullOrWhiteSpace(VariableName) ? "Missing stopwatch variable name" : null;
}