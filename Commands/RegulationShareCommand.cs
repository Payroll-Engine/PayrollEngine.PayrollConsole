using System;
using System.Linq;
using System.Threading.Tasks;
using System.Collections.Generic;
using Task = System.Threading.Tasks.Task;
using PayrollEngine.Client;
using PayrollEngine.Client.Command;
using PayrollEngine.Client.Model;
using PayrollEngine.Client.Service.Api;
using PayrollEngine.Client.QueryExpression;

namespace PayrollEngine.PayrollConsole.Commands;

[Command("RegulationShare")]
// ReSharper disable once UnusedType.Global
internal sealed class RegulationShareCommand : CommandBase<RegulationShareParameters>
{
    /// <summary>Regulation share setup</summary>
    protected override async Task<int> Execute(CommandContext context, RegulationShareParameters parameters)
    {
        var changeMode = parameters.ShareMode != ShareMode.View;
        if (changeMode && string.IsNullOrWhiteSpace(parameters.ConsumerTenant))
        {
            throw new ArgumentException("Missing provider tenant.");
        }
        if (changeMode && string.IsNullOrWhiteSpace(parameters.ProviderRegulation))
        {
            throw new ArgumentException("Missing provider regulation.");
        }

        DisplayTitle(context.Console, "Regulation share");
        if (context.DisplayLevel == DisplayLevel.Full)
        {
            if (!string.IsNullOrWhiteSpace(parameters.ProviderTenant))
            {
                context.Console.DisplayTextLine($"Tenant               {parameters.ProviderTenant}");
            }

            if (!string.IsNullOrWhiteSpace(parameters.ProviderRegulation))
            {
                context.Console.DisplayTextLine($"Regulation           {parameters.ProviderRegulation}");
            }

            if (!string.IsNullOrWhiteSpace(parameters.ConsumerTenant))
            {
                context.Console.DisplayTextLine($"Share tenant    {parameters.ConsumerTenant}");
            }

            if (!string.IsNullOrWhiteSpace(parameters.ConsumerDivision))
            {
                context.Console.DisplayTextLine($"Share division  {parameters.ConsumerDivision}");
            }

            context.Console.DisplayTextLine($"Url                  {context.HttpClient}");
            context.Console.DisplayTextLine($"Share mode      {parameters.ShareMode}");
        }

        context.Console.DisplayNewLine();

        try
        {
            // shares
            var sharesService = new RegulationShareService(context.HttpClient);

            // tenant
            Tenant tenantObject = null;
            if (!string.IsNullOrWhiteSpace(parameters.ProviderTenant))
            {
                tenantObject = await new TenantService(context.HttpClient).GetAsync<Tenant>(new(), parameters.ProviderTenant);
                if (tenantObject == null)
                {
                    context.Console.DisplayErrorLine($"Unknown tenant {parameters.ProviderTenant}");
                    return (int)ProgramExitCode.GenericError;
                }
            }
            if (changeMode && tenantObject == null)
            {
                context.Console.DisplayErrorLine("Missing tenant identifier");
                return (int)ProgramExitCode.ConnectionError;
            }

            // regulation
            Regulation regulationObject = null;
            if (tenantObject != null && !string.IsNullOrWhiteSpace(parameters.ProviderRegulation))
            {
                regulationObject = await new RegulationService(context.HttpClient).GetAsync<Regulation>(
                    new(tenantObject.Id), parameters.ProviderRegulation);
                if (regulationObject == null)
                {
                    context.Console.DisplayErrorLine($"Unknown regulation {parameters.ProviderRegulation}");
                    return (int)ProgramExitCode.ConnectionError;
                }
            }
            if (changeMode && regulationObject == null)
            {
                context.Console.DisplayErrorLine("Missing regulation name");
                return (int)ProgramExitCode.ConnectionError;
            }

            // share tenant
            Tenant shareTenantObject = null;
            if (!string.IsNullOrWhiteSpace(parameters.ConsumerTenant))
            {
                shareTenantObject = await new TenantService(context.HttpClient).GetAsync<Tenant>(new(), parameters.ConsumerTenant);
                if (shareTenantObject == null)
                {
                    context.Console.DisplayErrorLine($"Unknown share tenant {parameters.ConsumerTenant}");
                    return (int)ProgramExitCode.ConnectionError;
                }
            }
            if (changeMode && shareTenantObject == null)
            {
                context.Console.DisplayErrorLine("Missing share tenant identifier");
                return (int)ProgramExitCode.ConnectionError;
            }

            // share division (optional)
            Division shareDivisionObject = null;
            if (shareTenantObject != null && !string.IsNullOrWhiteSpace(parameters.ConsumerDivision))
            {
                shareDivisionObject = await new DivisionService(context.HttpClient).GetAsync<Division>(
                    new(shareTenantObject.Id), parameters.ConsumerDivision);
                if (shareDivisionObject == null)
                {
                    context.Console.DisplayErrorLine($"Unknown share division {parameters.ConsumerDivision}");
                    return (int)ProgramExitCode.ConnectionError;
                }
            }

            // query
            var query = GetRegulationShareQuery(tenantObject, regulationObject, shareTenantObject, shareDivisionObject);

            context.Console.DisplayNewLine();
            var shares = await QuerySharesAsync(context.HttpClient, sharesService, query);
            // view shares
            if (!changeMode)
            {
                if (shares.Count == 0)
                {
                    context.Console.DisplayInfoLine("No regulation shares available");
                }
                else
                {
                    context.Console.DisplayTextLine($"Total shares: {shares.Count}");
                    context.Console.DisplayNewLine();
                    foreach (var share in shares)
                    {
                        ReportShare(share, share == shares.First(), share == shares.Last());
                    }
                }
            }

            // set share
            else if (parameters.ShareMode == ShareMode.Set)
            {
                var share = BuildRegulationShare(tenantObject, regulationObject, shareTenantObject, shareDivisionObject);
                if (shares == null || !shares.Any())
                {
                    await CreateShareAsync(context.Console, sharesService, share);
                }
                else if (shares.Count == 1)
                {
                    context.Console.DisplayInfoLine("Share already set");
                }
                else
                {
                    context.Console.DisplayInfoLine("Removing duplicates...");
                    // replace multiple shares by one
                    foreach (var regulationShare in shares)
                    {
                        await sharesService.DeleteAsync(new(), regulationShare.Id);
                        ReportShare(regulationShare, regulationShare == shares.First(), regulationShare == shares.Last());
                    }
                    await CreateShareAsync(context.Console, sharesService, share);
                }
            }
            // remove share
            else if (parameters.ShareMode == ShareMode.Remove)
            {
                await RemoveSharesAsync(context.Console, shares, sharesService);
            }
            context.Console.DisplayNewLine();
            return (int)ProgramExitCode.Ok;
        }
        catch (Exception exception)
        {
            ProcessError(context.Console, exception);
            return (int)ProgramExitCode.GenericError;
        }
    }

    private static async Task RemoveSharesAsync(ICommandConsole console,
        List<RegulationShare> shares, RegulationShareService sharesService)
    {
        if (shares == null || !shares.Any())
        {
            console.DisplayInfoLine("Share not set");
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

            console.DisplayNewLine();
            console.DisplaySuccessLine("Share successfully removed");
            console.DisplayNewLine();
        }
    }

    private static async Task CreateShareAsync(ICommandConsole console,
        RegulationShareService sharesService, RegulationShare share)
    {
        await sharesService.CreateAsync(new(), share);
        console.DisplayNewLine();
        console.DisplaySuccessLine("Share successfully set");
        console.DisplayNewLine();
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

    public override ICommandParameters GetParameters(CommandLineParser parser) =>
        RegulationShareParameters.ParserFrom(parser);

    public override void ShowHelp(ICommandConsole console)
    {
        console.DisplayTitleLine("- RegulationShare");
        console.DisplayTextLine("      Manage regulation shares");
        console.DisplayTextLine("      Arguments:");
        console.DisplayTextLine("          1. provider tenant identifier (optional for /view) [ProviderTenant]");
        console.DisplayTextLine("          2. provider regulation name (optional for /view) [ProviderRegulation]");
        console.DisplayTextLine("          3. consumer tenant identifier (mandatory for share /set and /remove) [ConsumerTenant]");
        console.DisplayTextLine("          4. consumer tenant division identifier (undefined: all tenant divisions) [ConsumerDivision]");
        console.DisplayTextLine("      Toggles:");
        console.DisplayTextLine("          share mode: /view, /set or /remove (default: view)");
        console.DisplayTextLine("      Examples:");
        console.DisplayTextLine("          RegulationShare");
        console.DisplayTextLine("          RegulationShare ProviderTenantName ProviderRegulationName");
        console.DisplayTextLine("          RegulationShare ProviderTenantName ProviderRegulationName ShareTenantName");
        console.DisplayTextLine("          RegulationShare ProviderTenantName ProviderRegulationName ShareTenantName ShareDivisionName");
        console.DisplayTextLine("          RegulationShare ProviderTenantName ProviderRegulationName ShareTenantName /set");
        console.DisplayTextLine("          RegulationShare ProviderTenantName ProviderRegulationName ShareTenantName ShareDivisionName /set");
        console.DisplayTextLine("          RegulationShare ProviderTenantName ProviderRegulationName ShareTenantName /remove");
        console.DisplayTextLine("          RegulationShare ProviderTenantName ProviderRegulationName ShareTenantName ShareDivisionName /remove");
    }
}