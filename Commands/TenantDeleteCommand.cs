using System;
using System.Threading.Tasks;
using PayrollEngine.Client;
using PayrollEngine.Client.Model;
using PayrollEngine.Client.Command;
using PayrollEngine.Client.Service.Api;

namespace PayrollEngine.PayrollConsole.Commands;

/// <summary>
/// Tenant delete command
/// </summary>
[Command("TenantDelete")]
// ReSharper disable once UnusedType.Global
internal sealed class TenantDeleteCommand : CommandBase<TenantDeleteParameters>
{
    /// <summary>Delete tenant</summary>
    /// <param name="context">Command execution context</param>
    /// <param name="parameters">Command parameters</param>
    /// <returns>Program exit ok, 0 on success</returns>
    protected override async Task<int> Execute(CommandContext context, TenantDeleteParameters parameters)
    {
        // user info
        DisplayTitle(context.Console, $"Tenant delete - {parameters.Tenant}");
        if (context.DisplayLevel == DisplayLevel.Full)
        {
            context.Console.DisplayTextLine($"Tenant           {parameters.Tenant}");
            context.Console.DisplayTextLine($"Url              {context.HttpClient}");
            context.Console.DisplayTextLine($"Delete mode      {parameters.DeleteMode}");
        }

        context.Console.DisplayNewLine();

        try
        {
            // tenant
            var service = new TenantService(context.HttpClient);
            var tenant = await service.GetAsync<Tenant>(new(), parameters.Tenant);
            if (tenant == null)
            {
                switch (parameters.DeleteMode)
                {
                    // enforced delete
                    case ObjectDeleteMode.Delete:
                        context.Console.DisplayErrorLine($"Unknown tenant {parameters.Tenant}");
                        break;
                    // optional delete
                    case ObjectDeleteMode.TryDelete:
                        context.Console.DisplayInfoLine($"Tenant {parameters.Tenant} not available for deletion");
                        break;
                }
                return (int)ProgramExitCode.Ok;
            }

            // delete
            context.Console.DisplayInfo($"Deleting tenant {tenant.Identifier}...");
            await context.HttpClient.DeleteAsync(TenantApiEndpoints.TenantsUrl(), tenant.Id);
            context.Console.DisplayInfoLine("done.");

            // test
            tenant = await service.GetAsync<Tenant>(new(), parameters.Tenant);
            context.Console.DisplayNewLine();
            if (tenant == null)
            {
                context.Console.DisplaySuccessLine($"Tenant {parameters.Tenant} successfully deleted");
            }
            else
            {
                context.Console.DisplayErrorLine($"Tenant {parameters.Tenant} not deleted");
            }

            return (int)ProgramExitCode.Ok;
        }
        catch (Exception exception)
        {
            ProcessError(context.Console, exception);
            return (int)ProgramExitCode.GenericError;
        }
    }

    /// <inheritdoc />
    public override ICommandParameters GetParameters(CommandLineParser parser) =>
        TenantDeleteParameters.ParserFrom(parser);

    /// <inheritdoc />
    public override void ShowHelp(ICommandConsole console)
    {
        console.DisplayTitleLine("- TenantDelete");
        console.DisplayTextLine("      Delete tenant");
        console.DisplayTextLine("      Arguments:");
        console.DisplayTextLine("          1. tenant identifier [Tenant]");
        console.DisplayTextLine("      Toggles:");
        console.DisplayTextLine("          object delete mode: /delete or /trydelete (default: delete)");
        console.DisplayTextLine("      Examples:");
        console.DisplayTextLine("          TenantDelete MyTenantName");
        console.DisplayTextLine("          TenantDelete MyTenantName /trydelete");
    }
}