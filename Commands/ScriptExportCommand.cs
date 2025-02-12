using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using PayrollEngine.Client.Command;
using PayrollEngine.Client.Model;
using PayrollEngine.Client.Script;
using PayrollEngine.Client.Service.Api;

namespace PayrollEngine.PayrollConsole.Commands;

[Command("ScriptExport")]
// ReSharper disable once UnusedType.Global
internal sealed class ScriptExportCommand : CommandBase<ScriptExportParameters>
{
    /// <summary>Export script</summary>
    protected override async Task<int> Execute(CommandContext context, ScriptExportParameters parameters)
    {
        if (string.IsNullOrWhiteSpace(parameters.TargetFolder))
        {
            throw new PayrollException("Missing target folder.");
        }
        if (string.IsNullOrWhiteSpace(parameters.Tenant))
        {
            throw new PayrollException("Missing tenant.");
        }
        if (string.IsNullOrWhiteSpace(parameters.User))
        {
            throw new PayrollException("Missing user.");
        }
        if (string.IsNullOrWhiteSpace(parameters.Employee))
        {
            throw new PayrollException("Missing employee.");
        }
        if (string.IsNullOrWhiteSpace(parameters.Payroll))
        {
            throw new PayrollException("Missing payroll.");
        }
        if (string.IsNullOrWhiteSpace(parameters.Regulation))
        {
            throw new PayrollException("Missing regulation.");
        }

        DisplayTitle(context.Console, "Script export");
        if (context.DisplayLevel == DisplayLevel.Full)
        {
            context.Console.DisplayTextLine($"Target folder    {parameters.TargetFolder}");
            context.Console.DisplayTextLine($"Tenant           {parameters.Tenant}");
            context.Console.DisplayTextLine($"User             {parameters.User}");
            context.Console.DisplayTextLine($"Employee         {parameters.Employee}");
            context.Console.DisplayTextLine($"Payroll          {parameters.Payroll}");
            context.Console.DisplayTextLine($"Regulation       {parameters.Regulation}");
            context.Console.DisplayTextLine($"Url              {context.HttpClient}");
            context.Console.DisplayTextLine($"Build mode       {parameters.ExportMode}");
        }

        context.Console.DisplayNewLine();

        // tenant
        var tenant = await new TenantService(context.HttpClient)
            .GetAsync<Tenant>(new(), parameters.Tenant);
        if (tenant == null)
        {
            throw new PayrollException($"Invalid tenant {parameters.Tenant}.");
        }
        // user
        var user = await new UserService(context.HttpClient)
            .GetAsync<User>(new(tenant.Id), parameters.User);
        if (user == null)
        {
            throw new PayrollException($"Invalid user {parameters.User}.");
        }
        // employee
        var employee = await new EmployeeService(context.HttpClient)
            .GetAsync<Employee>(new(tenant.Id), parameters.Employee);
        if (employee == null)
        {
            throw new PayrollException($"Invalid employee {parameters.Employee}.");
        }
        // payroll
        var payroll = await new PayrollService(context.HttpClient)
            .GetAsync<Payroll>(new(tenant.Id), parameters.Payroll);
        if (payroll == null)
        {
            throw new PayrollException($"Invalid payroll {parameters.Payroll}.");
        }
        // regulation
        var regulation = await new RegulationService(context.HttpClient)
            .GetAsync<Regulation>(new(tenant.Id), parameters.Regulation);
        if (regulation == null)
        {
            throw new PayrollException($"Invalid regulation {parameters.Regulation}.");
        }

        // build
        try
        {
            var exportContext = new ScriptExportContext
            {
                Tenant = tenant,
                User = user,
                Employee = employee,
                Payroll = payroll,
                Regulation = regulation,
                ExportMode = parameters.ExportMode,
                ScriptObject = parameters.ScriptObject,
                Namespace = parameters.Namespace
            };
            var export = new PayrollScriptExport(context.HttpClient, exportContext);
            var scripts = await export.ExportAsync(regulation.Id);
            if (scripts.Any())
            {
                // ensure target folder
                if (!Directory.Exists(parameters.TargetFolder))
                {
                    Directory.CreateDirectory(parameters.TargetFolder);
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
                    var filePath = Path.Combine(parameters.TargetFolder, fileName);
                    context.Console.DisplayText($"Exporting script to {filePath}...");
                    if (File.Exists(filePath))
                    {
                        var existingContent = await File.ReadAllTextAsync(filePath);
                        write = !string.Equals(existingContent, script.Content);
                    }

                    if (write)
                    {
                        await File.WriteAllTextAsync(filePath, script.Content);
                    }
                    context.Console.DisplayNewLine();
                }

                context.Console.DisplayNewLine();
                context.Console.DisplaySuccessLine("Scripts successfully exported");
                context.Console.DisplayNewLine();
            }
            else
            {
                context.Console.DisplayNewLine();
                context.Console.WriteErrorLine("No scripts found to export");
                context.Console.DisplayNewLine();
            }
            return (int)ProgramExitCode.Ok;
        }
        catch (Exception exception)
        {
            if (context.Console.DisplayLevel == DisplayLevel.Silent)
            {
                context.Console.WriteErrorLine($"Script export error: {exception.GetBaseMessage()}");
            }
            ProcessError(context.Console, exception);
            return (int)ProgramExitCode.GenericError;
        }
    }

    public override ICommandParameters GetParameters(CommandLineParser parser) =>
        ScriptExportParameters.ParserFrom(parser);

    public override void ShowHelp(ICommandConsole console)
    {
        console.DisplayTitleLine("- ScriptExport");
        console.DisplayTextLine("      Export regulation scripts to folder");
        console.DisplayTextLine("      Arguments:");
        console.DisplayTextLine("          1. target folder [TargetFolder]");
        console.DisplayTextLine("          2. tenant identifier [Tenant]");
        console.DisplayTextLine("          3. user identifier [User]");
        console.DisplayTextLine("          4. employee identifier [Employee]");
        console.DisplayTextLine("          5. payroll name [Payroll]");
        console.DisplayTextLine("          6. regulation name [Regulation]");
        console.DisplayTextLine("          7. namespace [Namespace]");
        console.DisplayTextLine("      Toggles:");
        console.DisplayTextLine("          script export mode: /existing or /all (default: existing)");
        console.DisplayTextLine("      Examples:");
        console.DisplayTextLine("          ScriptExport scripts MyTenant MyUser MyEmployee MyPayroll MyRegulation");
        console.DisplayTextLine("          ScriptExport scripts MyTenant MyUser MyEmployee MyPayroll MyRegulation /all");
        console.DisplayTextLine("          ScriptExport scripts\\cases MyTenant MyUser MyEmployee MyPayroll MyRegulation Case");
    }
}