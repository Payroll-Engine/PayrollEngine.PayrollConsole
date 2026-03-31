using System;
using System.IO;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using PayrollEngine.Client.Command;
using PayrollEngine.Client.Model;
using PayrollEngine.Client.Service;
using PayrollEngine.Client.Service.Api;

namespace PayrollEngine.PayrollConsole.Commands.HttpCommands;

/// <summary>
/// Base class for http commands.
/// Supports URL placeholders resolved against the backend at runtime.
/// Placeholders are resolved left-to-right, so {tenant:X} must appear
/// before any tenant-scoped placeholder in the URL.
///
/// Supported placeholders:
///   {tenant:Identifier}   → numeric tenant id
///   {user:Identifier}     → numeric user id       (requires tenant placeholder before)
///   {division:Name}       → numeric division id   (requires tenant placeholder before)
///   {employee:Identifier} → numeric employee id   (requires tenant placeholder before)
///   {regulation:Name}     → numeric regulation id (requires tenant placeholder before)
///   {payroll:Name}        → numeric payroll id    (requires tenant placeholder before)
///   {payrun:Name}         → numeric payrun id     (requires tenant placeholder before)
///   {payrunJob:Name}      → numeric payrun job id (requires tenant placeholder before)
/// </summary>
internal abstract class HttpCommandBase<TArgs> : CommandBase<TArgs> where TArgs : ICommandParameters, new()
{
    /// <summary>
    /// Resolve {type:value} placeholders in a URL against the backend.
    /// Placeholders are resolved left-to-right — {tenant:X} must precede
    /// all tenant-scoped placeholders.
    /// </summary>
    protected async Task<string> ResolveUrlAsync(CommandContext context, string url)
    {
        var regex = new Regex(@"\{(\w+):([^}]+)\}");
        var result = url;
        int? resolvedTenantId = null;

        foreach (Match match in regex.Matches(url))
        {
            var type  = match.Groups[1].Value.ToLowerInvariant();
            var value = match.Groups[2].Value;
            string resolved;

            if (type == "tenant")
            {
                var tenant = await ResolveTenantAsync(context, value);
                resolvedTenantId = tenant.Id;
                resolved = tenant.Id.ToString();
            }
            else
            {
                if (resolvedTenantId == null)
                {
                    throw new PayrollException(
                        $"Placeholder '{{{type}:{value}}}' requires a {{tenant:Identifier}} " +
                        $"placeholder earlier in the URL.");
                }
                resolved = await ResolveTenantScopedAsync(context, resolvedTenantId.Value, type, value);
            }

            result = result.Replace(match.Value, resolved);
        }

        return result;
    }

    private static async Task<Tenant> ResolveTenantAsync(CommandContext context, string identifier)
    {
        var service = new TenantService(context.HttpClient);
        var tenant = await service.GetAsync<Tenant>(new(), identifier);
        if (tenant == null)
        {
            throw new PayrollException($"Tenant '{identifier}' not found.");
        }
        return tenant;
    }

    private static async Task<string> ResolveTenantScopedAsync(
        CommandContext context, int tenantId, string type, string value)
    {
        var tenantContext = new TenantServiceContext(tenantId);

        switch (type)
        {
            case "user":
                var users = new UserService(context.HttpClient);
                var user = await users.GetAsync<User>(tenantContext, value);
                if (user == null)
                {
                    throw new PayrollException($"User '{value}' not found in tenant {tenantId}.");
                }
                return user.Id.ToString();

            case "division":
                var divisions = new DivisionService(context.HttpClient);
                var division = await divisions.GetAsync<Division>(tenantContext, value);
                if (division == null)
                {
                    throw new PayrollException($"Division '{value}' not found in tenant {tenantId}.");
                }
                return division.Id.ToString();

            case "employee":
                var employees = new EmployeeService(context.HttpClient);
                var employee = await employees.GetAsync<Employee>(tenantContext, value);
                if (employee == null)
                {
                    throw new PayrollException($"Employee '{value}' not found in tenant {tenantId}.");
                }
                return employee.Id.ToString();

            case "regulation":
                var regulations = new RegulationService(context.HttpClient);
                var regulation = await regulations.GetAsync<Regulation>(tenantContext, value);
                if (regulation == null)
                {
                    throw new PayrollException($"Regulation '{value}' not found in tenant {tenantId}.");
                }
                return regulation.Id.ToString();

            case "payroll":
                var payrolls = new PayrollService(context.HttpClient);
                var payroll = await payrolls.GetAsync<Payroll>(tenantContext, value);
                if (payroll == null)
                {
                    throw new PayrollException($"Payroll '{value}' not found in tenant {tenantId}.");
                }
                return payroll.Id.ToString();

            case "payrun":
                var payruns = new PayrunService(context.HttpClient);
                var payrun = await payruns.GetAsync<Payrun>(tenantContext, value);
                if (payrun == null)
                {
                    throw new PayrollException($"Payrun '{value}' not found in tenant {tenantId}.");
                }
                return payrun.Id.ToString();

            case "payrunJob":
                var payrunJobs = new PayrunJobService(context.HttpClient);
                var payrunJob = await payrunJobs.GetAsync<PayrunJob>(tenantContext, value);
                if (payrunJob == null)
                {
                    throw new PayrollException($"Payrun job '{value}' not found in tenant {tenantId}.");
                }
                return payrunJob.Id.ToString();

            default:
                throw new NotSupportedException(
                    $"Unknown URL placeholder type '{type}'. " +
                    $"Supported: tenant, user, division, employee, regulation, payrun, payrunJob");
        }
    }

    /// <summary>Get the file content</summary>
    protected static async Task<string> GetFileContent(string fileName)
    {
        var content = string.Empty;
        if (!string.IsNullOrWhiteSpace(fileName) && File.Exists(fileName))
        {
            content = await File.ReadAllTextAsync(fileName);
        }
        return content;
    }
}
