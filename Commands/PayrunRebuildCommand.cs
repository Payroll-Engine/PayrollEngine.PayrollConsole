using System;
using System.Threading.Tasks;
using PayrollEngine.Client.Model;
using PayrollEngine.Client.Script;
using PayrollEngine.Client.Command;
using PayrollEngine.Client.Service.Api;

namespace PayrollEngine.PayrollConsole.Commands;

/// <summary>
/// Payrun rebuild command
/// </summary>
[Command("PayrunRebuild")]
// ReSharper disable once UnusedType.Global
internal sealed class PayrunRebuildCommand : CommandBase<PayrunRebuildParameters>
{
    /// <summary>Rebuild the payrun</summary>
    /// <param name="context">Command execution context</param>
    /// <param name="parameters">Command parameters</param>
    /// <returns>Program exit ok, 0 on success</returns>
    protected override async Task<int> Execute(CommandContext context, PayrunRebuildParameters parameters)
    {
        if (string.IsNullOrWhiteSpace(parameters.Tenant))
        {
            throw new PayrollException("Missing tenant argument.");
        }
        if (string.IsNullOrWhiteSpace(parameters.Payrun))
        {
            throw new PayrollException("Missing payrun name.");
        }

        // display
        DisplayTitle(context.Console, "Payrun rebuild");
        if (context.DisplayLevel == DisplayLevel.Full)
        {
            context.Console.DisplayTextLine($"Tenant           {parameters.Tenant}");
            context.Console.DisplayTextLine($"Payrun           {parameters.Payrun}");
            context.Console.DisplayTextLine($"Url              {context.HttpClient}");
        }

        context.Console.DisplayNewLine();

        context.Console.DisplayText("Building payrun script...");
        try
        {
            // tenant
            var tenant = await new TenantService(context.HttpClient).GetAsync<Tenant>(new(), parameters.Tenant);
            if (tenant == null)
            {
                throw new PayrollException($"Unknown tenant {parameters.Tenant}.");
            }

            // rebuild
            await new ScriptRebuild(context.HttpClient, tenant.Id).RebuildPayrunAsync(parameters.Payrun);

            // notification
            context.Console.DisplayNewLine();
            context.Console.DisplaySuccessLine($"Rebuilt payrun {parameters.Payrun} for tenant {parameters.Tenant}");
            return (int)ProgramExitCode.Ok;
        }
        catch (Exception exception)
        {
            if (context.Console.DisplayLevel == DisplayLevel.Silent)
            {
                context.Console.WriteErrorLine($"Payrun script build error: {exception.GetBaseMessage()}");
            }
            ProcessError(context.Console, exception);
            return (int)ProgramExitCode.GenericError;
        }
    }

    /// <inheritdoc />
    public override ICommandParameters GetParameters(CommandLineParser parser) =>
        PayrunRebuildParameters.ParserFrom(parser);

    /// <inheritdoc />
    public override void ShowHelp(ICommandConsole console)
    {
        console.DisplayTitleLine("- PayrunRebuild");
        console.DisplayTextLine("      Rebuild payrun (update scripting binaries)");
        console.DisplayTextLine("      Arguments:");
        console.DisplayTextLine("          1. tenant identifier [Tenant]");
        console.DisplayTextLine("          2. payrun name [Payrun]");
        console.DisplayTextLine("      Examples:");
        console.DisplayTextLine("          PayrunRebuild MyTenantName MyPayrunName");
    }
}