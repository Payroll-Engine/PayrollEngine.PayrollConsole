using System;
using PayrollEngine.Client.Command;

namespace PayrollEngine.PayrollConsole.Commands;

/// <summary>
/// Change password command parameters
/// </summary>
public class ChangePasswordParameters : ICommandParameters
{
    /// <summary>
    /// Tenant
    /// </summary>
    public string Tenant { get; init; }

    /// <summary>
    /// User
    /// </summary>
    public string User { get; init; }

    /// <summary>
    /// New password
    /// </summary>
    public string NewPassword { get; init; }

    /// <summary>
    /// Existing password
    /// </summary>
    public string ExistingPassword { get; init; }

    /// <inheritdoc />
    public Type[] Toggles => null;

    /// <inheritdoc />
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

    /// <summary>
    /// Parse command parameters
    /// </summary>
    /// <param name="parser">Parameter parser</param>
    public static ChangePasswordParameters ParserFrom(CommandLineParser parser) =>
        new()
        {
            Tenant = parser.Get(2, nameof(Tenant)),
            User = parser.Get(3, nameof(User)),
            NewPassword = parser.Get(4, nameof(NewPassword)),
            ExistingPassword = parser.Get(5, nameof(ExistingPassword))
        };
}