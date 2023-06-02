using System;
using PayrollEngine.Client;
using PayrollEngine.Client.Test;
using PayrollEngine.PayrollConsole.Shared;

namespace PayrollEngine.PayrollConsole.Arguments;

public static class CaseTestArguments
{
    public static string FileMask =>
        ConsoleArguments.GetMember(2);

    public static TestDisplayMode TestDisplayMode(TestDisplayMode defaultImportMode = Shared.TestDisplayMode.ShowFailed) =>
        ConsoleArguments.GetEnumToggle(defaultImportMode);

    public static TestPrecision TestPrecision(TestPrecision defaultTestPrecision = Client.Test.TestPrecision.TestPrecision2) =>
        ConsoleArguments.GetEnumToggle(defaultTestPrecision);

    public static Type[] Toggles => new[]
    {
        typeof(TestDisplayMode),
        typeof(TestPrecision)
    };

    public static string TestArguments() => null;
}