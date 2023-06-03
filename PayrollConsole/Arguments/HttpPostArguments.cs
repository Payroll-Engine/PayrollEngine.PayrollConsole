using System;
using PayrollEngine.Client;

namespace PayrollEngine.PayrollConsole.Arguments;

public static class HttpPostArguments
{
    public static string Url =>
        ConsoleArguments.GetMember(typeof(HttpPostArguments), 2);

    public static string FileName =>
        ConsoleArguments.GetMember(typeof(HttpPostArguments), 3);

    public static Type[] Toggles => null;

    public static string TestArguments() =>
        string.IsNullOrWhiteSpace(Url) ? "Missing http url" : null;
}