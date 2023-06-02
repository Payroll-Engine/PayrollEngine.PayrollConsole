using System;
using PayrollEngine.Client;

namespace PayrollEngine.PayrollConsole.Arguments;

public static class HttpPutArguments
{
    public static string Url =>
        ConsoleArguments.GetMember(2);

    public static string FileName =>
        ConsoleArguments.GetMember(3);

    public static Type[] Toggles => null;

    public static string TestArguments() =>
        string.IsNullOrWhiteSpace(Url) ? "Missing http url" : null;
}