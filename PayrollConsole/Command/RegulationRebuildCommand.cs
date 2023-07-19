using System;
using System.Threading.Tasks;
using PayrollEngine.Client;
using PayrollEngine.Client.Model;
using PayrollEngine.Client.Script;
using PayrollEngine.Client.Service.Api;
using PayrollEngine.PayrollConsole.Shared;

namespace PayrollEngine.PayrollConsole.Command;

internal sealed class RegulationRebuildCommand : HttpCommandBase
{
    internal RegulationRebuildCommand(PayrollHttpClient httpClient) :
        base(httpClient)
    {
    }

    /// <summary>
    /// Rebuild the regulation
    /// </summary>
    /// <param name="tenantIdentifier">The identifier of the tenant</param>
    /// <param name="regulationName">The regulation name</param>
    /// <param name="scriptObject">The scripting object</param>
    /// <param name="objectKey">The object key, all )</param>
    internal async Task<ProgramExitCode> RebuildAsync(string tenantIdentifier, string regulationName,
        RegulationScriptObject? scriptObject, string objectKey = null)
    {
        if (string.IsNullOrWhiteSpace(tenantIdentifier))
        {
            throw new PayrollException("Missing tenant argument");
        }
        if (string.IsNullOrWhiteSpace(regulationName))
        {
            throw new PayrollException("Missing regulation name");
        }

        DisplayTitle("Rebuild regulation scripts");
        ConsoleTool.DisplayTextLine($"Tenant           {tenantIdentifier}");
        ConsoleTool.DisplayTextLine($"Regulation       {regulationName}");
        if (!string.IsNullOrWhiteSpace(objectKey))
        {
            ConsoleTool.DisplayTextLine($"Object           {objectKey}");
        }
        ConsoleTool.DisplayTextLine($"Url              {HttpClient}");
        ConsoleTool.DisplayNewLine();

        // build
        ConsoleTool.DisplayText("Building regulation scripts...");
        try
        {
            // tenant
            var tenant = await new TenantService(HttpClient).GetAsync<Tenant>(new(), tenantIdentifier);
            if (tenant == null)
            {
                throw new PayrollException($"Unknown tenant {tenantIdentifier}");
            }

            // rebuild
            var scriptRebuild = new ScriptRebuild(HttpClient, tenant.Id);
            if (scriptObject.HasValue)
            {
                await scriptRebuild.RebuildRegulationObjectAsync(regulationName, scriptObject.Value, objectKey);
            }
            else
            {
                await scriptRebuild.RebuildRegulationAsync(regulationName);
            }

            ConsoleTool.DisplayNewLine();
            ConsoleTool.DisplaySuccessLine($"Rebuilt regulation {regulationName} for tenant {tenantIdentifier}");
            return ProgramExitCode.Ok;
        }
        catch (Exception exception)
        {
            if (ConsoleTool.DisplayMode == ConsoleDisplayMode.Silent)
            {
                ConsoleTool.WriteErrorLine($"Regulation script build error: {exception.GetBaseMessage()}");
            }
            return ProgramExitCode.GenericError;
        }
    }

    internal static void ShowHelp()
    {
        ConsoleTool.DisplayTitleLine("- RegulationRebuild");
        ConsoleTool.DisplayTextLine("      Rebuild the regulation objects (update scripting binaries)");
        ConsoleTool.DisplayTextLine("      Arguments:");
        ConsoleTool.DisplayTextLine("          1. tenant identifier [Tenant]");
        ConsoleTool.DisplayTextLine("          2. regulation name [RegulationName]");
        ConsoleTool.DisplayTextLine("          3. object type: Case, CaseRelation, Collector, WageType or Report (default: all) [ObjectType]");
        ConsoleTool.DisplayTextLine("          4. object key, requires the object type [ObjectKey]");
        ConsoleTool.DisplayTextLine("      Examples:");
        ConsoleTool.DisplayTextLine("          RegulationRebuild MyTenantName MyRegulationName");
        ConsoleTool.DisplayTextLine("          RegulationRebuild MyTenantName MyRegulationName Case");
        ConsoleTool.DisplayTextLine("          RegulationRebuild MyTenantName MyRegulationName Case MyCaseName");
        ConsoleTool.DisplayTextLine("          RegulationRebuild MyTenantName MyRegulationName CaseRelation");
        ConsoleTool.DisplayTextLine("          RegulationRebuild MyTenantName MyRegulationName CaseRelation SourceCaseName;TargetCaseName");
        ConsoleTool.DisplayTextLine("          RegulationRebuild MyTenantName MyRegulationName WageType");
        ConsoleTool.DisplayTextLine("          RegulationRebuild MyTenantName MyRegulationName WageType 115");
    }
}