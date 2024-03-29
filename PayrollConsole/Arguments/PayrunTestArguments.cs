﻿using System;
using PayrollEngine.Client;
using PayrollEngine.Client.Exchange;
using PayrollEngine.Client.Test;
using PayrollEngine.Client.Test.Payrun;
using PayrollEngine.PayrollConsole.Shared;

namespace PayrollEngine.PayrollConsole.Arguments;

public static class PayrunTestArguments
{
    public static string FileMask =>
        ConsoleArguments.GetMember(typeof(PayrunTestArguments), 2);

    public static string Owner =>
        ConsoleArguments.GetMember(typeof(PayrunTestArguments), 3);

    public static DataImportMode DataImportMode(DataImportMode defaultDataImportMode = Client.Exchange.DataImportMode.Bulk) =>
        ConsoleArguments.GetEnumToggle(defaultDataImportMode);
    
    public static TestRunMode TestRunMode(TestRunMode defaultRunMode = Client.Test.Payrun.TestRunMode.RunTests) =>
        ConsoleArguments.GetEnumToggle(defaultRunMode);

    public static TestDisplayMode TestDisplayMode(TestDisplayMode defaultImportMode = Shared.TestDisplayMode.ShowFailed) =>
        ConsoleArguments.GetEnumToggle(defaultImportMode);

    public static TestResultMode TestResultMode(TestResultMode defaultValue = Client.Test.Payrun.TestResultMode.CleanTest) =>
        ConsoleArguments.GetEnumToggle(defaultValue);

    public static TestPrecision TestPrecision(TestPrecision defaultTestPrecision = Client.Test.TestPrecision.TestPrecision2) =>
        ConsoleArguments.GetEnumToggle(defaultTestPrecision);

    public static Type[] Toggles => new[]
    {
        typeof(DataImportMode),
        typeof(TestRunMode),
        typeof(TestDisplayMode),
        typeof(TestResultMode),
        typeof(TestPrecision)
    };

    public static string TestArguments() =>
        string.IsNullOrWhiteSpace(FileMask) ? "Missing file name or file mask" : null;
}