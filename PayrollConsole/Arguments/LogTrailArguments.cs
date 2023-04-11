using System;
using PayrollEngine.Client;

namespace PayrollEngine.PayrollConsole.Arguments;

public static class LogTrailArguments
{
    public static string Tenant =>
        ConsoleArguments.Get(2);

    public static int Interval(int defaultSeconds = 5) =>
        ConsoleArguments.GetInt(3, defaultSeconds);

    public static Type[] Toggles => null;

    public static string TestArguments() =>
        string.IsNullOrWhiteSpace(Tenant) ? "Missing tenant" : null;
}