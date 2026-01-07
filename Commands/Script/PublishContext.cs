using System.Threading.Tasks;
using PayrollEngine.Client;
using PayrollEngine.Client.Model;
using PayrollEngine.Client.Scripting;
using PayrollEngine.Client.Service;
using PayrollEngine.Client.Service.Api;

namespace PayrollEngine.PayrollConsole.Commands.Script;

internal sealed class PublishContext
{
    internal PayrollHttpClient HttpClient { get; }
    internal ScriptAttribute ScriptAttribute { get; }
    internal FunctionAttribute FunctionAttribute { get; }
    internal string MethodBody { get; }

    internal int TenantId => TenantContext.TenantId;

    internal TenantServiceContext TenantContext
    {
        get
        {
            if (field == null)
            {
                var tenant = GetTenantAsync(FunctionAttribute.TenantIdentifier).Result;
                if (tenant == null)
                {
                    throw new PayrollException($"Invalid tenant {FunctionAttribute.TenantIdentifier}.");
                }
                field = new(tenant.Id);
            }
            return field;
        }
    }

    internal PublishContext(PayrollHttpClient httpClient, ScriptAttribute scriptAttribute,
        FunctionAttribute functionAttribute, string methodBody)
    {
        HttpClient = httpClient;
        ScriptAttribute = scriptAttribute;
        FunctionAttribute = functionAttribute;
        MethodBody = methodBody;
    }

    private async Task<Tenant> GetTenantAsync(string tenantIdentifier) =>
        await new TenantService(HttpClient).GetAsync<Tenant>(new(), tenantIdentifier);

    internal async Task<Regulation> GetRegulationAsync(int tenantId, string regulationName) =>
        await new RegulationService(HttpClient).GetAsync<Regulation>(new(tenantId), regulationName);
}