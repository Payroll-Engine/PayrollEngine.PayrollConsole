﻿using System;
using PayrollEngine.Client;
using PayrollEngine.Client.Exchange;

namespace PayrollEngine.PayrollConsole.Arguments;

public static class PayrollImportArguments
{
    public static string SourceFileName =>
        ConsoleArguments.Get(2);

    public static string OptionsFileName =>
        ConsoleArguments.Get(3);

    public static string Namespace =>
        ConsoleArguments.Get(4);

    public static DataImportMode DataImportMode(DataImportMode defaultValue = Client.Exchange.DataImportMode.Single) =>
        ConsoleArguments.GetEnumToggle(defaultValue);

    public static Type[] Toggles => new[]
    {
        typeof(DataImportMode)
    };

    public static string TestArguments() =>
        string.IsNullOrWhiteSpace(SourceFileName) ? "Missing source file name or file mask" : null;
}