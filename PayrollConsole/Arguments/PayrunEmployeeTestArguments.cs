using System;
using PayrollEngine.Client;
using PayrollEngine.Client.Test;
using PayrollEngine.Client.Test.Payrun;
using PayrollEngine.PayrollConsole.Shared;

namespace PayrollEngine.PayrollConsole.Arguments;

public static class PayrunEmployeeTestArguments
{
    public static string FileMask =>
        ConsoleArguments.GetMember(typeof(PayrunEmployeeTestArguments), 2);

    public static string Owner =>
        ConsoleArguments.GetMember(typeof(PayrunEmployeeTestArguments), 3);

    public static EmployeeTestMode EmployeeTestMode(EmployeeTestMode defaultTestMode = Client.Test.Payrun.EmployeeTestMode.InsertEmployee) =>
        ConsoleArguments.GetEnumToggle(defaultTestMode);

    public static TestRunMode TestRunMode(TestRunMode defaultRunMode = Client.Test.Payrun.TestRunMode.RunTests) =>
        ConsoleArguments.GetEnumToggle(defaultRunMode);

    public static TestDisplayMode TestDisplayMode(TestDisplayMode defaultImportMode = Shared.TestDisplayMode.ShowFailed) =>
        ConsoleArguments.GetEnumToggle(defaultImportMode);

    public static TestPrecision TestPrecision(TestPrecision defaultTestPrecision = Client.Test.TestPrecision.TestPrecision2) =>
        ConsoleArguments.GetEnumToggle(defaultTestPrecision);

    public static Type[] Toggles =>
    [
        typeof(EmployeeTestMode),
        typeof(TestRunMode),
        typeof(TestDisplayMode),
        typeof(TestPrecision)
    ];

    public static string TestArguments() =>
        string.IsNullOrWhiteSpace(FileMask) ? "Missing file name or file mask" : null;
}