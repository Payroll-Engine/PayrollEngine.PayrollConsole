using System;
using PayrollEngine.Client;

namespace PayrollEngine.PayrollConsole.Arguments;

public static class PayrunStatisticsArguments
{
    public static string Tenant =>
        ConsoleArguments.Get(2);

    public static int CreatedSinceMinutes(int defaultMinutes = 30) =>
        ConsoleArguments.GetInt(3, defaultMinutes);

    public static Type[] Toggles => null;
}