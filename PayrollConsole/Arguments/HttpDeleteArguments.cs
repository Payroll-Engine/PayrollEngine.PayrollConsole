using System;
using PayrollEngine.Client;

namespace PayrollEngine.PayrollConsole.Arguments;

public static class HttpDeleteArguments
{
    public static string Url =>
        ConsoleArguments.GetMember(2);

    public static Type[] Toggles => null;

    public static string TestArguments() =>
        string.IsNullOrWhiteSpace(Url) ? "Missing http url" : null;
}