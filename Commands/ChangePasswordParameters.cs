using System;
using PayrollEngine.Client.Command;

namespace PayrollEngine.PayrollConsole.Commands;

public class ChangePasswordParameters : ICommandParameters
{
    public string Tenant { get; init; }
    public string User { get; init; }
    public string NewPassword { get; init; }
    public string ExistingPassword { get; init; }
    public Type[] Toggles => null;

    public string Test()
    {
        if (string.IsNullOrWhiteSpace(Tenant))
        {
            return "Missing tenant";
        }
        if (string.IsNullOrWhiteSpace(User))
        {
            return "Missing user";
        }
        return string.IsNullOrWhiteSpace(NewPassword) ? "Missing new password" : null;
    }

    public static ChangePasswordParameters ParserFrom(CommandLineParser parser) =>
        new()
        {
            Tenant = parser.Get(2, nameof(Tenant)),
            User = parser.Get(3, nameof(User)),
            NewPassword = parser.Get(4, nameof(NewPassword)),
            ExistingPassword = parser.Get(5, nameof(ExistingPassword))
        };
}