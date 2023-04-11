using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using PayrollEngine.Client;
using PayrollEngine.Client.Model;
using PayrollEngine.Client.Script;
using PayrollEngine.Client.Service.Api;
using PayrollEngine.PayrollConsole.Shared;

namespace PayrollEngine.PayrollConsole.Command;

internal sealed class ScriptExportCommand : HttpCommandBase
{
    internal ScriptExportCommand(PayrollHttpClient httpClient) :
        base(httpClient)
    {
    }

    internal async Task<ProgramExitCode> ExportAsync(ExportScriptSettings settings)
    {
        if (string.IsNullOrWhiteSpace(settings.TargetFolder))
        {
            throw new PayrollException("Missing target folder");
        }
        if (string.IsNullOrWhiteSpace(settings.TenantIdentifier))
        {
            throw new PayrollException("Missing tenant");
        }
        if (string.IsNullOrWhiteSpace(settings.UserIdentifier))
        {
            throw new PayrollException("Missing user");
        }
        if (string.IsNullOrWhiteSpace(settings.EmployeeIdentifier))
        {
            throw new PayrollException("Missing employee");
        }
        if (string.IsNullOrWhiteSpace(settings.PayrollName))
        {
            throw new PayrollException("Missing payroll");
        }
        if (string.IsNullOrWhiteSpace(settings.RegulationName))
        {
            throw new PayrollException("Missing regulation");
        }

        DisplayTitle("Export regulation scripts");
        ConsoleTool.DisplayTextLine($"Target folder    {settings.TargetFolder}");
        ConsoleTool.DisplayTextLine($"Tenant           {settings.TenantIdentifier}");
        ConsoleTool.DisplayTextLine($"User             {settings.UserIdentifier}");
        ConsoleTool.DisplayTextLine($"Employee         {settings.EmployeeIdentifier}");
        ConsoleTool.DisplayTextLine($"Payroll          {settings.PayrollName}");
        ConsoleTool.DisplayTextLine($"Regulation       {settings.RegulationName}");
        ConsoleTool.DisplayTextLine($"Url              {HttpClient}");
        ConsoleTool.DisplayTextLine($"Build mode       {settings.ScriptExportMode}");
        ConsoleTool.DisplayNewLine();

        // tenant
        var tenant = await new TenantService(HttpClient)
            .GetAsync<Tenant>(new(), settings.TenantIdentifier);
        if (tenant == null)
        {
            throw new PayrollException($"Invalid tenant {settings.TenantIdentifier}");
        }
        // user
        var user = await new UserService(HttpClient)
            .GetAsync<User>(new(tenant.Id), settings.UserIdentifier);
        if (user == null)
        {
            throw new PayrollException($"Invalid user {settings.UserIdentifier}");
        }
        // employee
        var employee = await new EmployeeService(HttpClient)
            .GetAsync<Employee>(new(tenant.Id), settings.EmployeeIdentifier);
        if (employee == null)
        {
            throw new PayrollException($"Invalid employee {settings.EmployeeIdentifier}");
        }
        // payroll
        var payroll = await new PayrollService(HttpClient)
            .GetAsync<Payroll>(new(tenant.Id), settings.PayrollName);
        if (payroll == null)
        {
            throw new PayrollException($"Invalid payroll {settings.PayrollName}");
        }
        // regulation
        var regulation = await new RegulationService(HttpClient)
            .GetAsync<Regulation>(new(tenant.Id), settings.RegulationName);
        if (regulation == null)
        {
            throw new PayrollException($"Invalid regulation {settings.RegulationName}");
        }

        // build
        try
        {
            var context = new ScriptExportContext
            {
                Tenant = tenant,
                User = user,
                Employee = employee,
                Payroll = payroll,
                Regulation = regulation,
                ExportMode = settings.ScriptExportMode,
                ScriptObject = settings.ScriptObject,
                Namespace = settings.Namespace
            };
            var export = new PayrollScriptExport(HttpClient, context);
            var scripts = await export.ExportAsync(regulation.Id);
            if (scripts.Any())
            {
                // ensure target folder
                if (!Directory.Exists(settings.TargetFolder))
                {
                    Directory.CreateDirectory(settings.TargetFolder);
                }

                // save any script to file
                foreach (var script in scripts)
                {
                    if (string.IsNullOrWhiteSpace(script.ScriptName) ||
                        string.IsNullOrWhiteSpace(script.Content))
                    {
                        continue;
                    }

                    var fileName = script.ScriptName;
                    if (script.FunctionType.HasValue)
                    {
                        fileName += $".{script.FunctionType}.cs";
                    }
                    else
                    {
                        fileName += ".cs";
                    }

                    // conditional write (keep timestamp for unchanged scripts)
                    var write = true;
                    var filePath = Path.Combine(settings.TargetFolder, fileName);
                    ConsoleTool.DisplayText($"Exporting script to {filePath}...");
                    if (File.Exists(filePath))
                    {
                        var existingContent = await File.ReadAllTextAsync(filePath);
                        write = !string.Equals(existingContent, script.Content);
                    }

                    if (write)
                    {
                        await File.WriteAllTextAsync(filePath, script.Content);
                    }
                    ConsoleTool.DisplayNewLine();
                }

                ConsoleTool.DisplayNewLine();
                ConsoleTool.DisplaySuccessLine("Scripts successfully exported");
                ConsoleTool.DisplayNewLine();
            }
            else
            {
                ConsoleTool.DisplayNewLine();
                ConsoleTool.WriteErrorLine("No scripts found to export");
                ConsoleTool.DisplayNewLine();
            }
            return ProgramExitCode.Ok;
        }
        catch (Exception exception)
        {
            if (ConsoleTool.DisplayMode == ConsoleDisplayMode.Silent)
            {
                ConsoleTool.WriteErrorLine($"Script export error: {exception.GetBaseMessage()}");
            }
            return ProgramExitCode.GenericError;
        }
    }

    internal static void ShowHelp()
    {
        ConsoleTool.DisplayTitleLine("- ScriptExport");
        ConsoleTool.DisplayTextLine("      Export regulation scripts to folder");
        ConsoleTool.DisplayTextLine("      Arguments:");
        ConsoleTool.DisplayTextLine("          1. target folder");
        ConsoleTool.DisplayTextLine("          2. tenant identifier");
        ConsoleTool.DisplayTextLine("          3. user identifier");
        ConsoleTool.DisplayTextLine("          4. employee identifier");
        ConsoleTool.DisplayTextLine("          5. payroll name");
        ConsoleTool.DisplayTextLine("          6. regulation name");
        ConsoleTool.DisplayTextLine("          7. object type: Case, CaseRelation, Collector, WageType or Report (default: all)");
        ConsoleTool.DisplayTextLine("      Toggles:");
        ConsoleTool.DisplayTextLine("          script export mode: /existing or /all (default: existing)");
        ConsoleTool.DisplayTextLine("      Examples:");
        ConsoleTool.DisplayTextLine("          ScriptExport scripts MyTenant MyUser MyEmployee MyPayroll MyRegulation");
        ConsoleTool.DisplayTextLine("          ScriptExport scripts MyTenant MyUser MyEmployee MyPayroll MyRegulation /all");
        ConsoleTool.DisplayTextLine("          ScriptExport scripts\\cases MyTenant MyUser MyEmployee MyPayroll MyRegulation Case");
    }
}