﻿using System;
using PayrollEngine.Client;

namespace PayrollEngine.PayrollConsole.Arguments;

public static class ScriptPublishArguments
{
    public static string SourceFile =>
        ConsoleArguments.GetMember(2);

    public static string SourceScript =>
        ConsoleArguments.GetMember(3);

    public static Type[] Toggles => null;

    public static string TestArguments() =>
        string.IsNullOrWhiteSpace(SourceFile) ? "Missing source file" : null;
}