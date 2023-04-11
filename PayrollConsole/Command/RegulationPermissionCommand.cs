using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using PayrollEngine.Client;
using PayrollEngine.Client.Model;
using PayrollEngine.Client.Service.Api;
using PayrollEngine.PayrollConsole.Shared;
using Task = System.Threading.Tasks.Task;

namespace PayrollEngine.PayrollConsole.Command;

internal sealed class RegulationPermissionCommand : HttpCommandBase
{
    internal RegulationPermissionCommand(PayrollHttpClient httpClient) :
        base(httpClient)
    {
    }

    internal async Task<ProgramExitCode> ChangeAsync(string tenant,
        string regulation, string permissionTenant, string permissionDivision, PermissionMode permissionMode)
    {
        var changeMode = permissionMode != PermissionMode.View;
        if (changeMode && string.IsNullOrWhiteSpace(permissionTenant))
        {
            throw new ArgumentException("Missing permission tenant name");
        }
        if (changeMode && string.IsNullOrWhiteSpace(regulation))
        {
            throw new ArgumentException("Missing regulation name");
        }

        DisplayTitle("Regulation permission");
        if (!string.IsNullOrWhiteSpace(tenant))
        {
            ConsoleTool.DisplayTextLine($"Tenant               {tenant}");
        }
        if (!string.IsNullOrWhiteSpace(regulation))
        {
            ConsoleTool.DisplayTextLine($"Regulation           {regulation}");
        }
        if (!string.IsNullOrWhiteSpace(permissionTenant))
        {
            ConsoleTool.DisplayTextLine($"Permission tenant    {permissionTenant}");
        }
        if (!string.IsNullOrWhiteSpace(permissionDivision))
        {
            ConsoleTool.DisplayTextLine($"Permission division  {permissionDivision}");
        }
        ConsoleTool.DisplayTextLine($"Url                  {HttpClient}");
        ConsoleTool.DisplayTextLine($"Permission mode      {permissionMode}");
        ConsoleTool.DisplayNewLine();

        try
        {
            // permissions
            var permissionsService = new SharedRegulationService(HttpClient);

            // tenant
            Tenant tenantObject = null;
            if (!string.IsNullOrWhiteSpace(tenant))
            {
                tenantObject = await new TenantService(HttpClient).GetAsync<Tenant>(new(), tenant);
                if (tenantObject == null)
                {
                    ConsoleTool.DisplayErrorLine($"Unknown tenant {tenant}");
                    return ProgramExitCode.GenericError;
                }
            }
            if (changeMode && tenantObject == null)
            {
                ConsoleTool.DisplayErrorLine("Missing tenant name");
                return ProgramExitCode.ConnectionError;
            }

            // regulation
            Regulation regulationObject = null;
            if (tenantObject != null && !string.IsNullOrWhiteSpace(regulation))
            {
                regulationObject = await new RegulationService(HttpClient).GetAsync<Regulation>(
                    new(tenantObject.Id), regulation);
                if (regulationObject == null)
                {
                    ConsoleTool.DisplayErrorLine($"Unknown regulation {regulation}");
                    return ProgramExitCode.ConnectionError;
                }
            }
            if (changeMode && regulationObject == null)
            {
                ConsoleTool.DisplayErrorLine("Missing regulation name");
                return ProgramExitCode.ConnectionError;
            }

            // permission tenant
            Tenant permissionTenantObject = null;
            if (!string.IsNullOrWhiteSpace(permissionTenant))
            {
                permissionTenantObject = await new TenantService(HttpClient).GetAsync<Tenant>(new(), permissionTenant);
                if (permissionTenantObject == null)
                {
                    ConsoleTool.DisplayErrorLine($"Unknown permission tenant {permissionTenant}");
                    return ProgramExitCode.ConnectionError;
                }
            }
            if (changeMode && permissionTenantObject == null)
            {
                ConsoleTool.DisplayErrorLine("Missing permission tenant name");
                return ProgramExitCode.ConnectionError;
            }

            // permission division (optional)
            Division permissionDivisionObject = null;
            if (permissionTenantObject != null && !string.IsNullOrWhiteSpace(permissionDivision))
            {
                permissionDivisionObject = await new DivisionService(HttpClient).GetAsync<Division>(
                    new(permissionTenantObject.Id), permissionDivision);
                if (permissionDivisionObject == null)
                {
                    ConsoleTool.DisplayErrorLine($"Unknown permission division {permissionDivision}");
                    return ProgramExitCode.ConnectionError;
                }
            }

            // query
            var query = GetRegulationPermissionQuery(tenantObject, regulationObject, permissionTenantObject, permissionDivisionObject);

            ConsoleTool.DisplayNewLine();
            var permissions = await QueryPermissionsAsync(HttpClient, permissionsService, query);
            // view permissions
            if (!changeMode)
            {
                if (permissions.Count == 0)
                {
                    ConsoleTool.DisplayInfoLine("No regulation permissions available");
                }
                else
                {
                    ConsoleTool.DisplayTextLine($"Total permissions: {permissions.Count}");
                    ConsoleTool.DisplayNewLine();
                    foreach (var permission in permissions)
                    {
                        ReportPermission(permission, permission == permissions.First(), permission == permissions.Last());
                    }
                }
            }

            // set permission
            else if (permissionMode == PermissionMode.Set)
            {
                var permission = BuildRegulationPermission(tenantObject, regulationObject, permissionTenantObject, permissionDivisionObject);
                if (permissions == null || !permissions.Any())
                {
                    await CreatePermissionAsync(permissionsService, permission);
                }
                else if (permissions.Count == 1)
                {
                    ConsoleTool.DisplayInfoLine("Permission already set");
                }
                else
                {
                    ConsoleTool.DisplayInfoLine("Removing duplicates...");
                    // replace multiple permissions by one
                    foreach (var regulationPermission in permissions)
                    {
                        await permissionsService.DeleteAsync(new(), regulationPermission.Id);
                        ReportPermission(regulationPermission, regulationPermission == permissions.First(), regulationPermission == permissions.Last());
                    }
                    await CreatePermissionAsync(permissionsService, permission);
                }
            }
            // remove permission
            else if (permissionMode == PermissionMode.Remove)
            {
                await RemovePermissionsAsync(permissions, permissionsService);
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

    private static async Task RemovePermissionsAsync(List<RegulationPermission> permissions, SharedRegulationService permissionsService)
    {
        if (permissions == null || !permissions.Any())
        {
            ConsoleTool.DisplayInfoLine("Permission not set");
        }
        else
        {
            // remove multiple permissions
            foreach (var regulationPermission in permissions)
            {
                await permissionsService.DeleteAsync(new(), regulationPermission.Id);
                ReportPermission(regulationPermission, regulationPermission == permissions.First(),
                    regulationPermission == permissions.Last());
            }

            ConsoleTool.DisplayNewLine();
            ConsoleTool.DisplaySuccessLine("Permission successfully removed");
            ConsoleTool.DisplayNewLine();
        }
    }

    private static async Task CreatePermissionAsync(SharedRegulationService permissionsService,
        RegulationPermission permission)
    {
        await permissionsService.CreateAsync(new(), permission);
        ConsoleTool.DisplayNewLine();
        ConsoleTool.DisplaySuccessLine("Permission successfully set");
        ConsoleTool.DisplayNewLine();
        ReportPermission(permission, true, true);
    }

    private static Query GetRegulationPermissionQuery(Tenant tenantObject, Regulation regulationObject,
        Tenant permissionTenant, Division permissionDivision)
    {
        var query = new Query();
        if (tenantObject != null)
        {
            query.Filter = $"{nameof(RegulationPermission.TenantId)} eq {tenantObject.Id}";
            if (regulationObject != null)
            {
                query.Filter += $" and {nameof(RegulationPermission.RegulationId)} eq {regulationObject.Id}";
                if (permissionTenant != null)
                {
                    query.Filter +=
                        $" and {nameof(RegulationPermission.PermissionTenantId)} eq {permissionTenant.Id}";
                    if (permissionDivision != null)
                    {
                        query.Filter +=
                            $" and {nameof(RegulationPermission.PermissionDivisionId)} eq {permissionDivision.Id}";
                    }
                }
            }
        }
        return query;
    }

    private static void ReportPermission(RegulationPermission permission, bool start, bool end)
    {
        const int columnWidth = 30;
        var line = new string('-', 4 * columnWidth);

        // start
        if (start)
        {
            Console.WriteLine(line);
            Console.Write("Tenant".PadRight(columnWidth));
            Console.Write("Regulation".PadRight(columnWidth));
            Console.Write("Permission tenant".PadRight(columnWidth));
            Console.Write("Permission division".PadRight(columnWidth));
            Console.WriteLine();
            Console.WriteLine(line);
        }

        // permission
        Console.Write(permission.TenantIdentifier.PadRight(columnWidth));
        Console.Write(permission.RegulationName.PadRight(columnWidth));
        Console.Write(permission.PermissionTenantIdentifier.PadRight(columnWidth));
        if (!string.IsNullOrWhiteSpace(permission.PermissionDivisionName))
        {
            Console.Write(permission.PermissionDivisionName.PadRight(columnWidth));
        }
        Console.WriteLine();

        // end
        if (end)
        {
            Console.WriteLine(line);
        }
    }

    private static async Task<List<RegulationPermission>> QueryPermissionsAsync(PayrollHttpClient httpClient,
        SharedRegulationService permissionsService, Query query)
    {
        var permissions = await permissionsService.QueryAsync<RegulationPermission>(new(), query);

        var tenantService = new TenantService(httpClient);
        var regulationService = new RegulationService(httpClient);
        var divisionService = new DivisionService(httpClient);
        foreach (var permission in permissions)
        {
            permission.TenantIdentifier = (await tenantService.GetAsync<Tenant>(
                new(), permission.TenantId)).Identifier;
            permission.RegulationName = (await regulationService.GetAsync<Regulation>(
                new(permission.TenantId), permission.RegulationId)).Name;
            permission.PermissionTenantIdentifier = (await tenantService.GetAsync<Tenant>(
                new(), permission.PermissionTenantId)).Identifier;
            if (permission.PermissionDivisionId.HasValue)
            {
                permission.PermissionDivisionName = (await divisionService.GetAsync<Division>(
                    new(permission.PermissionTenantId), permission.PermissionDivisionId.Value)).Name;
            }
        }
        return permissions;
    }

    private static RegulationPermission BuildRegulationPermission(Tenant tenantObject, Regulation regulationObject,
        Tenant permissionTenantObject, Division permissionDivisionObject) =>
        new()
        {
            TenantId = tenantObject.Id,
            TenantIdentifier = tenantObject.Identifier,
            RegulationId = regulationObject.Id,
            RegulationName = regulationObject.Name,
            PermissionTenantId = permissionTenantObject.Id,
            PermissionTenantIdentifier = permissionTenantObject.Identifier,
            PermissionDivisionId = permissionDivisionObject?.Id,
            PermissionDivisionName = permissionDivisionObject?.Name
        };

    internal static void ShowHelp()
    {
        ConsoleTool.DisplayTitleLine("- RegulationPermission");
        ConsoleTool.DisplayTextLine("      Manage the shared regulation permission");
        ConsoleTool.DisplayTextLine("      Arguments:");
        ConsoleTool.DisplayTextLine("          1. tenant name (optional for /view)");
        ConsoleTool.DisplayTextLine("          2. regulation name (optional for /view)");
        ConsoleTool.DisplayTextLine("          3. permission tenant name (mandatory for permission /set and /remove)");
        ConsoleTool.DisplayTextLine("          4. permission division name (undefined: all tenant divisions)");
        ConsoleTool.DisplayTextLine("      Toggles:");
        ConsoleTool.DisplayTextLine("          permission mode: /view, /set or /remove (default: view)");
        ConsoleTool.DisplayTextLine("      Examples:");
        ConsoleTool.DisplayTextLine("          RegulationPermission");
        ConsoleTool.DisplayTextLine("          RegulationPermission ProviderTenantName ProviderRegulationName");
        ConsoleTool.DisplayTextLine("          RegulationPermission ProviderTenantName ProviderRegulationName PermissionTenantName");
        ConsoleTool.DisplayTextLine("          RegulationPermission ProviderTenantName ProviderRegulationName PermissionTenantName PermissionDivisionName");
        ConsoleTool.DisplayTextLine("          RegulationPermission ProviderTenantName ProviderRegulationName PermissionTenantName /set");
        ConsoleTool.DisplayTextLine("          RegulationPermission ProviderTenantName ProviderRegulationName PermissionTenantName PermissionDivisionName /set");
        ConsoleTool.DisplayTextLine("          RegulationPermission ProviderTenantName ProviderRegulationName PermissionTenantName /remove");
        ConsoleTool.DisplayTextLine("          RegulationPermission ProviderTenantName ProviderRegulationName PermissionTenantName PermissionDivisionName /remove");
    }
}