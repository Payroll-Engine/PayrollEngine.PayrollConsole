using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using PayrollEngine.Client;
using PayrollEngine.Client.Model;
using PayrollEngine.Client.QueryExpression;
using PayrollEngine.Client.Service.Api;
using PayrollEngine.PayrollConsole.Shared;
using Task = System.Threading.Tasks.Task;

namespace PayrollEngine.PayrollConsole.Command;

internal sealed class RegulationShareCommand : HttpCommandBase
{
    internal RegulationShareCommand(PayrollHttpClient httpClient) :
        base(httpClient)
    {
    }

    internal async Task<ProgramExitCode> ChangeAsync(RegulationShareCommandSettings settings)
    {
        if (settings == null)
        {
            throw new ArgumentNullException(nameof(settings));
        }
        var changeMode = settings.ShareMode != ShareMode.View;
        if (changeMode && string.IsNullOrWhiteSpace(settings.ConsumerTenant))
        {
            throw new ArgumentException("Missing provider tenant");
        }
        if (changeMode && string.IsNullOrWhiteSpace(settings.ProviderRegulation))
        {
            throw new ArgumentException("Missing provider regulation");
        }

        DisplayTitle("Regulation share");
        if (!string.IsNullOrWhiteSpace(settings.ProviderTenant))
        {
            ConsoleTool.DisplayTextLine($"Tenant               {settings.ProviderTenant}");
        }
        if (!string.IsNullOrWhiteSpace(settings.ProviderRegulation))
        {
            ConsoleTool.DisplayTextLine($"Regulation           {settings.ProviderRegulation}");
        }
        if (!string.IsNullOrWhiteSpace(settings.ConsumerTenant))
        {
            ConsoleTool.DisplayTextLine($"Share tenant    {settings.ConsumerTenant}");
        }
        if (!string.IsNullOrWhiteSpace(settings.ConsumerDivision))
        {
            ConsoleTool.DisplayTextLine($"Share division  {settings.ConsumerDivision}");
        }
        ConsoleTool.DisplayTextLine($"Url                  {HttpClient}");
        ConsoleTool.DisplayTextLine($"Share mode      {settings.ShareMode}");
        ConsoleTool.DisplayNewLine();

        try
        {
            // shares
            var sharesService = new RegulationShareService(HttpClient);

            // tenant
            Tenant tenantObject = null;
            if (!string.IsNullOrWhiteSpace(settings.ProviderTenant))
            {
                tenantObject = await new TenantService(HttpClient).GetAsync<Tenant>(new(), settings.ProviderTenant);
                if (tenantObject == null)
                {
                    ConsoleTool.DisplayErrorLine($"Unknown tenant {settings.ProviderTenant}");
                    return ProgramExitCode.GenericError;
                }
            }
            if (changeMode && tenantObject == null)
            {
                ConsoleTool.DisplayErrorLine("Missing tenant identifier");
                return ProgramExitCode.ConnectionError;
            }

            // regulation
            Regulation regulationObject = null;
            if (tenantObject != null && !string.IsNullOrWhiteSpace(settings.ProviderRegulation))
            {
                regulationObject = await new RegulationService(HttpClient).GetAsync<Regulation>(
                    new(tenantObject.Id), settings.ProviderRegulation);
                if (regulationObject == null)
                {
                    ConsoleTool.DisplayErrorLine($"Unknown regulation {settings.ProviderRegulation}");
                    return ProgramExitCode.ConnectionError;
                }
            }
            if (changeMode && regulationObject == null)
            {
                ConsoleTool.DisplayErrorLine("Missing regulation name");
                return ProgramExitCode.ConnectionError;
            }

            // share tenant
            Tenant shareTenantObject = null;
            if (!string.IsNullOrWhiteSpace(settings.ConsumerTenant))
            {
                shareTenantObject = await new TenantService(HttpClient).GetAsync<Tenant>(new(), settings.ConsumerTenant);
                if (shareTenantObject == null)
                {
                    ConsoleTool.DisplayErrorLine($"Unknown share tenant {settings.ConsumerTenant}");
                    return ProgramExitCode.ConnectionError;
                }
            }
            if (changeMode && shareTenantObject == null)
            {
                ConsoleTool.DisplayErrorLine("Missing share tenant identifier");
                return ProgramExitCode.ConnectionError;
            }

            // share division (optional)
            Division shareDivisionObject = null;
            if (shareTenantObject != null && !string.IsNullOrWhiteSpace(settings.ConsumerDivision))
            {
                shareDivisionObject = await new DivisionService(HttpClient).GetAsync<Division>(
                    new(shareTenantObject.Id), settings.ConsumerDivision);
                if (shareDivisionObject == null)
                {
                    ConsoleTool.DisplayErrorLine($"Unknown share division {settings.ConsumerDivision}");
                    return ProgramExitCode.ConnectionError;
                }
            }

            // query
            var query = GetRegulationShareQuery(tenantObject, regulationObject, shareTenantObject, shareDivisionObject);

            ConsoleTool.DisplayNewLine();
            var shares = await QuerySharesAsync(HttpClient, sharesService, query);
            // view shares
            if (!changeMode)
            {
                if (shares.Count == 0)
                {
                    ConsoleTool.DisplayInfoLine("No regulation shares available");
                }
                else
                {
                    ConsoleTool.DisplayTextLine($"Total shares: {shares.Count}");
                    ConsoleTool.DisplayNewLine();
                    foreach (var share in shares)
                    {
                        ReportShare(share, share == shares.First(), share == shares.Last());
                    }
                }
            }

            // set share
            else if (settings.ShareMode == ShareMode.Set)
            {
                var share = BuildRegulationShare(tenantObject, regulationObject, shareTenantObject, shareDivisionObject);
                if (shares == null || !shares.Any())
                {
                    await CreateShareAsync(sharesService, share);
                }
                else if (shares.Count == 1)
                {
                    ConsoleTool.DisplayInfoLine("Share already set");
                }
                else
                {
                    ConsoleTool.DisplayInfoLine("Removing duplicates...");
                    // replace multiple shares by one
                    foreach (var regulationShare in shares)
                    {
                        await sharesService.DeleteAsync(new(), regulationShare.Id);
                        ReportShare(regulationShare, regulationShare == shares.First(), regulationShare == shares.Last());
                    }
                    await CreateShareAsync(sharesService, share);
                }
            }
            // remove share
            else if (settings.ShareMode == ShareMode.Remove)
            {
                await RemoveSharesAsync(shares, sharesService);
            }
            ConsoleTool.DisplayNewLine();
            return ProgramExitCode.Ok;
        }
        catch (Exception exception)
        {
            ProcessError(exception);
            return ProgramExitCode.GenericError;
        }
    }

    private static async Task RemoveSharesAsync(List<RegulationShare> shares, RegulationShareService sharesService)
    {
        if (shares == null || !shares.Any())
        {
            ConsoleTool.DisplayInfoLine("Share not set");
        }
        else
        {
            // remove multiple shares
            foreach (var regulationShare in shares)
            {
                await sharesService.DeleteAsync(new(), regulationShare.Id);
                ReportShare(regulationShare, regulationShare == shares.First(),
                    regulationShare == shares.Last());
            }

            ConsoleTool.DisplayNewLine();
            ConsoleTool.DisplaySuccessLine("Share successfully removed");
            ConsoleTool.DisplayNewLine();
        }
    }

    private static async Task CreateShareAsync(RegulationShareService sharesService,
        RegulationShare share)
    {
        await sharesService.CreateAsync(new(), share);
        ConsoleTool.DisplayNewLine();
        ConsoleTool.DisplaySuccessLine("Share successfully set");
        ConsoleTool.DisplayNewLine();
        ReportShare(share, true, true);
    }

    private static Query GetRegulationShareQuery(Tenant providerTenant, Regulation providerRegulation,
        Tenant consumerTenant, Division consumerDivision)
    {
        var query = new Query();
        if (providerTenant == null)
        {
            return query;
        }

        // provider tenant
        Filter filter = new Equals(nameof(RegulationShare.ProviderTenantId), providerTenant.Id);
        if (providerRegulation != null)
        {
            // provider regulation
            filter = filter.And(new Equals(nameof(RegulationShare.ProviderRegulationId), providerRegulation.Id));
            if (consumerTenant != null)
            {
                // consumer tenant
                filter = filter.And(new Equals(nameof(RegulationShare.ConsumerTenantId), consumerTenant.Id));
                if (consumerDivision != null)
                {
                    // consumer division
                    filter = filter.And(new Equals(nameof(RegulationShare.ConsumerDivisionId), consumerDivision.Id));
                }
            }
        }
        query.Filter = filter;
        return query;
    }

    private static void ReportShare(RegulationShare share, bool start, bool end)
    {
        const int columnWidth = 30;
        var line = new string('-', 4 * columnWidth);

        // start
        if (start)
        {
            Console.WriteLine(line);
            Console.Write("Tenant".PadRight(columnWidth));
            Console.Write("Regulation".PadRight(columnWidth));
            Console.Write("Share tenant".PadRight(columnWidth));
            Console.Write("Share division".PadRight(columnWidth));
            Console.WriteLine();
            Console.WriteLine(line);
        }

        // share
        Console.Write(share.ProviderTenantIdentifier.PadRight(columnWidth));
        Console.Write(share.ProviderRegulationName.PadRight(columnWidth));
        Console.Write(share.ConsumerTenantIdentifier.PadRight(columnWidth));
        if (!string.IsNullOrWhiteSpace(share.ConsumerDivisionName))
        {
            Console.Write(share.ConsumerDivisionName.PadRight(columnWidth));
        }
        Console.WriteLine();

        // end
        if (end)
        {
            Console.WriteLine(line);
        }
    }

    private static async Task<List<RegulationShare>> QuerySharesAsync(PayrollHttpClient httpClient,
        RegulationShareService sharesService, Query query)
    {
        var shares = await sharesService.QueryAsync<RegulationShare>(new(), query);

        var tenantService = new TenantService(httpClient);
        var regulationService = new RegulationService(httpClient);
        var divisionService = new DivisionService(httpClient);
        foreach (var share in shares)
        {
            share.ProviderTenantIdentifier = (await tenantService.GetAsync<Tenant>(
                new(), share.ProviderTenantId)).Identifier;
            share.ProviderRegulationName = (await regulationService.GetAsync<Regulation>(
                new(share.ProviderTenantId), share.ProviderRegulationId)).Name;
            share.ConsumerTenantIdentifier = (await tenantService.GetAsync<Tenant>(
                new(), share.ConsumerTenantId)).Identifier;
            if (share.ConsumerDivisionId.HasValue)
            {
                share.ConsumerDivisionName = (await divisionService.GetAsync<Division>(
                    new(share.ConsumerTenantId), share.ConsumerDivisionId.Value)).Name;
            }
        }
        return shares;
    }

    private static RegulationShare BuildRegulationShare(Tenant providerTenant, Regulation providerRegulation,
        Tenant consumerTenant, Division consumerDivision) =>
        new()
        {
            ProviderTenantId = providerTenant.Id,
            ProviderTenantIdentifier = providerTenant.Identifier,
            ProviderRegulationId = providerRegulation.Id,
            ProviderRegulationName = providerRegulation.Name,
            ConsumerTenantId = consumerTenant.Id,
            ConsumerTenantIdentifier = consumerTenant.Identifier,
            ConsumerDivisionId = consumerDivision?.Id,
            ConsumerDivisionName = consumerDivision?.Name
        };

    internal static void ShowHelp()
    {
        ConsoleTool.DisplayTitleLine("- RegulationShare");
        ConsoleTool.DisplayTextLine("      Manage regulation shares");
        ConsoleTool.DisplayTextLine("      Arguments:");
        ConsoleTool.DisplayTextLine("          1. provider tenant identifier (optional for /view) [ProviderTenant]");
        ConsoleTool.DisplayTextLine("          2. provider regulation name (optional for /view) [ProviderRegulation]");
        ConsoleTool.DisplayTextLine("          3. consumer tenant identifier (mandatory for share /set and /remove) [ConsumerTenant]");
        ConsoleTool.DisplayTextLine("          4. consumer tenant division identifier (undefined: all tenant divisions) [ConsumerDivision]");
        ConsoleTool.DisplayTextLine("      Toggles:");
        ConsoleTool.DisplayTextLine("          share mode: /view, /set or /remove (default: view)");
        ConsoleTool.DisplayTextLine("      Examples:");
        ConsoleTool.DisplayTextLine("          RegulationShare");
        ConsoleTool.DisplayTextLine("          RegulationShare ProviderTenantName ProviderRegulationName");
        ConsoleTool.DisplayTextLine("          RegulationShare ProviderTenantName ProviderRegulationName ShareTenantName");
        ConsoleTool.DisplayTextLine("          RegulationShare ProviderTenantName ProviderRegulationName ShareTenantName ShareDivisionName");
        ConsoleTool.DisplayTextLine("          RegulationShare ProviderTenantName ProviderRegulationName ShareTenantName /set");
        ConsoleTool.DisplayTextLine("          RegulationShare ProviderTenantName ProviderRegulationName ShareTenantName ShareDivisionName /set");
        ConsoleTool.DisplayTextLine("          RegulationShare ProviderTenantName ProviderRegulationName ShareTenantName /remove");
        ConsoleTool.DisplayTextLine("          RegulationShare ProviderTenantName ProviderRegulationName ShareTenantName ShareDivisionName /remove");
    }
}