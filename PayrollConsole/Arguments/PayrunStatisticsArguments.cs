using System;
using PayrollEngine.Client;

namespace PayrollEngine.PayrollConsole.Arguments;

public static class PayrunStatisticsArguments
{
    public static string Tenant =>
        ConsoleArguments.GetMember(typeof(PayrunStatisticsArguments), 2);

    public static int CreatedSinceMinutes(int defaultMinutes = 30) =>
        ConsoleArguments.GetInt(3, defaultMinutes, nameof(CreatedSinceMinutes));

    public static Type[] Toggles => null;
}