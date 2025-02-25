using System;
using System.Threading.Tasks;
using PayrollEngine.Client.Model;
using PayrollEngine.Client.Script;
using PayrollEngine.Client.Command;
using PayrollEngine.Client.Service.Api;

namespace PayrollEngine.PayrollConsole.Commands;

/// <summary>
/// Regulation rebuild command
/// </summary>
[Command("RegulationRebuild")]
// ReSharper disable once UnusedType.Global
internal sealed class RegulationRebuildCommand : CommandBase<RegulationRebuildParameters>
{
    /// <summary>Rebuild the regulation</summary>
    /// <param name="context">Command execution context</param>
    /// <param name="parameters">Command parameters</param>
    /// <returns>Program exit ok, 0 on success</returns>
    protected override async Task<int> Execute(CommandContext context, RegulationRebuildParameters parameters)
    {
        if (string.IsNullOrWhiteSpace(parameters.Tenant))
        {
            throw new PayrollException("Missing tenant argument.");
        }
        if (string.IsNullOrWhiteSpace(parameters.Regulation))
        {
            throw new PayrollException("Missing regulation name.");
        }

        DisplayTitle(context.Console, "Regulation rebuild");
        if (context.DisplayLevel == DisplayLevel.Full)
        {
            context.Console.DisplayTextLine($"Tenant           {parameters.Tenant}");
            context.Console.DisplayTextLine($"Regulation       {parameters.Regulation}");
            if (!string.IsNullOrWhiteSpace(parameters.ObjectKey))
            {
                context.Console.DisplayTextLine($"Object           {parameters.ObjectKey}");
            }

            context.Console.DisplayTextLine($"Url              {context.HttpClient}");
        }

        context.Console.DisplayNewLine();

        // build
        context.Console.DisplayText("Building regulation scripts...");
        try
        {
            // tenant
            var tenant = await new TenantService(context.HttpClient).GetAsync<Tenant>(new(), parameters.Tenant);
            if (tenant == null)
            {
                throw new PayrollException($"Unknown tenant {parameters.Tenant}.");
            }

            // rebuild
            var scriptRebuild = new ScriptRebuild(context.HttpClient, tenant.Id);
            if (parameters.ScriptObject.HasValue)
            {
                await scriptRebuild.RebuildRegulationObjectAsync(parameters.Regulation, parameters.ScriptObject.Value, parameters.ObjectKey);
            }
            else
            {
                await scriptRebuild.RebuildRegulationAsync(parameters.Regulation);
            }

            context.Console.DisplayNewLine();
            context.Console.DisplaySuccessLine($"Rebuilt regulation {parameters.Regulation} for tenant {parameters.Tenant}");
            return (int)ProgramExitCode.Ok;
        }
        catch (Exception exception)
        {
            if (context.Console.DisplayLevel == DisplayLevel.Silent)
            {
                context.Console.WriteErrorLine($"Regulation script build error: {exception.GetBaseMessage()}");
            }
            ProcessError(context.Console, exception);
            return (int)ProgramExitCode.GenericError;
        }
    }

    /// <inheritdoc />
    public override ICommandParameters GetParameters(CommandLineParser parser) =>
        RegulationRebuildParameters.ParserFrom(parser);

    /// <inheritdoc />
    public override void ShowHelp(ICommandConsole console)
    {
        console.DisplayTitleLine("- RegulationRebuild");
        console.DisplayTextLine("      Rebuild the regulation objects (update scripting binaries)");
        console.DisplayTextLine("      Arguments:");
        console.DisplayTextLine("          1. tenant identifier [Tenant]");
        console.DisplayTextLine("          2. regulation name [Regulation]");
        console.DisplayTextLine("          3. object type: Case, CaseRelation, Collector, WageType or Report (default: all) [ObjectType]");
        console.DisplayTextLine("          4. object key, requires the object type [ObjectKey]");
        console.DisplayTextLine("      Examples:");
        console.DisplayTextLine("          RegulationRebuild MyTenantName MyRegulationName");
        console.DisplayTextLine("          RegulationRebuild MyTenantName MyRegulationName Case");
        console.DisplayTextLine("          RegulationRebuild MyTenantName MyRegulationName Case MyCaseName");
        console.DisplayTextLine("          RegulationRebuild MyTenantName MyRegulationName CaseRelation");
        console.DisplayTextLine("          RegulationRebuild MyTenantName MyRegulationName CaseRelation SourceCaseName;TargetCaseName");
        console.DisplayTextLine("          RegulationRebuild MyTenantName MyRegulationName WageType");
        console.DisplayTextLine("          RegulationRebuild MyTenantName MyRegulationName WageType 115");
    }
}