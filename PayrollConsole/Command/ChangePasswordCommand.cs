using System;
using System.Threading.Tasks;
using PayrollEngine.Client;
using PayrollEngine.Client.Model;
using PayrollEngine.Client.Service.Api;
using PayrollEngine.PayrollConsole.Shared;

namespace PayrollEngine.PayrollConsole.Command;

internal sealed class ChangePasswordCommand : HttpCommandBase
{
    internal ChangePasswordCommand(PayrollHttpClient httpClient) :
        base(httpClient)
    {
    }

    /// <summary>
    /// Change the user password
    /// </summary>
    /// <param name="tenantIdentifier">The identifier of the tenant</param>
    /// <param name="userIdentifier">The identifier of the user</param>
    /// <param name="newPassword">The new password</param>
    /// <param name="existingPassword">The existing password, required while changing an existing password</param>
    internal async Task<ProgramExitCode> ChangeAsync(string tenantIdentifier, string userIdentifier,
        string newPassword, string existingPassword = null)
    {
        if (string.IsNullOrWhiteSpace(tenantIdentifier))
        {
            throw new PayrollException("Missing tenant argument");
        }
        if (string.IsNullOrWhiteSpace(userIdentifier))
        {
            throw new PayrollException("Missing user argument");
        }
        if (string.IsNullOrWhiteSpace(newPassword))
        {
            throw new PayrollException("Missing new password");
        }
        // display
        DisplayTitle("Change user password");
        ConsoleTool.DisplayTextLine($"Tenant            {tenantIdentifier}");
        ConsoleTool.DisplayTextLine($"User              {userIdentifier}");
        ConsoleTool.DisplayTextLine("New password      (yes)");
        ConsoleTool.DisplayTextLine($"Existing password {(string.IsNullOrWhiteSpace(existingPassword) ? "(no)" : "(yes)")}");
        ConsoleTool.DisplayNewLine();

        ConsoleTool.DisplayText("Change user password...");
        try
        {
            if (string.Equals(newPassword, existingPassword))
            {
                ConsoleTool.WriteErrorLine("Ignoring unchanged password");
            }
            else
            {
                // tenant
                var tenant = await new TenantService(HttpClient).GetAsync<Tenant>(new(), tenantIdentifier);
                if (tenant == null)
                {
                    throw new PayrollException($"Unknown tenant {tenantIdentifier}");
                }

                // user
                var userService = new UserService(HttpClient);
                var user = await userService.GetAsync<User>(new(tenant.Id), userIdentifier);
                if (user == null)
                {
                    throw new PayrollException($"Unknown user {userIdentifier}");
                }

                // existing password
                var existingValid = true;
                if (!string.IsNullOrWhiteSpace(user.Password))
                {
                    if (string.IsNullOrWhiteSpace(existingPassword))
                    {
                        ConsoleTool.DisplayNewLine();
                        ConsoleTool.WriteErrorLine("Missing existing password");
                    }
                    existingValid = await userService.TestPasswordAsync(new(tenant.Id), user.Id, existingPassword);
                }

                if (!existingValid)
                {
                    ConsoleTool.DisplayNewLine();
                    ConsoleTool.WriteErrorLine("Invalid existing password");
                    return ProgramExitCode.FailedTest;
                }

                // new password
                await userService.UpdatePasswordAsync(new(tenant.Id), user.Id, newPassword);

                // notification
                ConsoleTool.DisplayNewLine();
                ConsoleTool.DisplaySuccessLine($"Password successfully changed for user {userIdentifier}");
            }

            return ProgramExitCode.Ok;
        }
        catch (Exception exception)
        {
            if (ConsoleTool.DisplayMode == ConsoleDisplayMode.Silent)
            {
                ConsoleTool.WriteErrorLine($"Payrun script build error: {exception.GetBaseMessage()}");
            }
            return ProgramExitCode.GenericError;
        }
    }

    internal static void ShowHelp()
    {
        ConsoleTool.DisplayTitleLine("- ChangePassword");
        ConsoleTool.DisplayTextLine("      Change the user password");
        ConsoleTool.DisplayTextLine("      Arguments:");
        ConsoleTool.DisplayTextLine("          1. tenant identifier [Tenant]");
        ConsoleTool.DisplayTextLine("          2. user identifier [User]");
        ConsoleTool.DisplayTextLine("          3. new password [NewPassword]");
        ConsoleTool.DisplayTextLine("          4. existing password, required on change [ExistingPassword]");
        ConsoleTool.DisplayTextLine("      Examples:");
        ConsoleTool.DisplayTextLine("          ChangePassword MyTenant MyUser My3irst@assword");
        ConsoleTool.DisplayTextLine("          ChangePassword MyTenant MyUser My2econd@assword My3irst@assword");
    }
}