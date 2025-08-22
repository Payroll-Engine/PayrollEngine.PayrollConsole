using System;
using System.Threading.Tasks;
using PayrollEngine.Client.Command;
using PayrollEngine.Client.Model;
using PayrollEngine.Client.Service.Api;

namespace PayrollEngine.PayrollConsole.Commands.UserCommands;

/// <summary>
/// Change password command
/// </summary>
[Command("ChangePassword")]
// ReSharper disable once UnusedType.Global
internal sealed class ChangePasswordCommand : CommandBase<ChangePasswordParameters>
{
    /// <summary>Change the user password</summary>
    /// <param name="context">Command execution context</param>
    /// <param name="parameters">Command parameters</param>
    /// <returns>Program exit ok, 0 on success</returns>
    protected override async Task<int> Execute(CommandContext context, ChangePasswordParameters parameters)
    {
        if (string.IsNullOrWhiteSpace(parameters.Tenant))
        {
            throw new PayrollException("Missing tenant argument.");
        }
        if (string.IsNullOrWhiteSpace(parameters.User))
        {
            throw new PayrollException("Missing user argument.");
        }
        if (string.IsNullOrWhiteSpace(parameters.NewPassword))
        {
            throw new PayrollException("Missing new password.");
        }
        // display
        DisplayTitle(context.Console, "Change password");
        if (context.DisplayLevel == DisplayLevel.Full)
        {
            context.Console.DisplayTextLine($"Tenant            {parameters.Tenant}");
            context.Console.DisplayTextLine($"User              {parameters.User}");
            context.Console.DisplayTextLine("New password      (yes)");
            context.Console.DisplayTextLine($"Existing password {(string.IsNullOrWhiteSpace(parameters.ExistingPassword) ? "(no)" : "(yes)")}");
        }

        context.Console.DisplayNewLine();

        context.Console.DisplayText("Change user password...");
        try
        {
            if (string.Equals(parameters.NewPassword, parameters.ExistingPassword))
            {
                context.Console.WriteErrorLine("Ignoring unchanged password");
            }
            else
            {
                // tenant
                var tenant = await new TenantService(context.HttpClient).GetAsync<Tenant>(new(), parameters.Tenant);
                if (tenant == null)
                {
                    throw new PayrollException($"Unknown tenant {parameters.Tenant}.");
                }

                // user
                var userService = new UserService(context.HttpClient);
                var user = await userService.GetAsync<User>(new(tenant.Id), parameters.User);
                if (user == null)
                {
                    throw new PayrollException($"Unknown user {parameters.User}.");
                }

                // existing password
                var existingValid = true;
                if (!string.IsNullOrWhiteSpace(user.Password))
                {
                    if (string.IsNullOrWhiteSpace(parameters.ExistingPassword))
                    {
                        context.Console.DisplayNewLine();
                        context.Console.WriteErrorLine("Missing existing password");
                    }
                    existingValid = await userService.TestPasswordAsync(new(tenant.Id), user.Id, parameters.ExistingPassword);
                }

                if (!existingValid)
                {
                    context.Console.DisplayNewLine();
                    context.Console.WriteErrorLine("Invalid existing password");
                    return (int)ProgramExitCode.FailedTest;
                }

                // new password
                await userService.UpdatePasswordAsync(new(tenant.Id), user.Id, new()
                {
                    NewPassword = parameters.NewPassword,
                    ExistingPassword = parameters.ExistingPassword
                });

                // notification
                context.Console.DisplayNewLine();
                context.Console.DisplaySuccessLine($"Password successfully changed for user {parameters.User}");
            }

            return (int)ProgramExitCode.Ok;
        }
        catch (Exception exception)
        {
            if (context.Console.DisplayLevel == DisplayLevel.Silent)
            {
                context.Console.WriteErrorLine($"Payrun script build error: {exception.GetBaseMessage()}");
            }
            return (int)ProgramExitCode.GenericError;
        }
    }

    /// <inheritdoc />
    public override ICommandParameters GetParameters(CommandLineParser parser) =>
        ChangePasswordParameters.ParserFrom(parser);

    /// <inheritdoc />
    public override void ShowHelp(ICommandConsole console)
    {
        console.DisplayTitleLine("- ChangePassword");
        console.DisplayTextLine("      Change the user password");
        console.DisplayTextLine("      Arguments:");
        console.DisplayTextLine("          1. tenant identifier [Tenant]");
        console.DisplayTextLine("          2. user identifier [User]");
        console.DisplayTextLine("          3. new password [NewPassword]");
        console.DisplayTextLine("          4. existing password, required on change [ExistingPassword]");
        console.DisplayTextLine("      Examples:");
        console.DisplayTextLine("          ChangePassword MyTenant MyUser My3irst@assword");
        console.DisplayTextLine("          ChangePassword MyTenant MyUser My2econd@assword My3irst@assword");
    }
}