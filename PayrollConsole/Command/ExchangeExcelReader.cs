using System;
using System.IO;
using System.Threading.Tasks;
using NPOI.SS.UserModel;
using NPOI.XSSF.UserModel;
using PayrollEngine.Client;
using PayrollEngine.Client.Model;
using PayrollEngine.Client.Service.Api;
using PayrollEngine.PayrollConsole.Excel;

namespace PayrollEngine.PayrollConsole.Command;

internal sealed class ExchangeExcelReader
{
    private PayrollHttpClient HttpClient { get; }

    internal ExchangeExcelReader(PayrollHttpClient httpClient)
    {
        HttpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
    }

    internal async Task<Exchange> ReadAsync(string fileName, string overrideTenant = null)
    {
        if (string.IsNullOrWhiteSpace(fileName))
        {
            throw new ArgumentException(nameof(fileName));
        }

        // import file
        if (!File.Exists(fileName))
        {
            throw new PayrollException($"Missing Payroll Excel file {fileName}");
        }

        // workbook (file share to load open/locked excel document)
        await using var stream = new FileStream(fileName, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
        IWorkbook workbook = new XSSFWorkbook(stream);

        // import tenant
        var tenant = await GetTenantAsync(workbook);
        // tenant override
        if (!string.IsNullOrWhiteSpace(overrideTenant))
        {
            tenant.Identifier = overrideTenant;
        }

        // import regulation lookups
        await ExchangeExcelRegulationLookupReader.ReadAsync(HttpClient, tenant, workbook);

        // import case values
        await ExchangeExcelCaseReader.ReadAsync(HttpClient, tenant, workbook, fileName);

        return new()
        {
            Tenants = new() { tenant }
        };
    }

    private async Task<ExchangeTenant> GetTenantAsync(IWorkbook workbook)
    {
        var tenantName = workbook.GetNamedValue<string>(Specification.TenantRegionName);
        if (string.IsNullOrWhiteSpace(tenantName))
        {
            throw new PayrollException("Missing tenant identifier");
        }

        // get existing tenant
        var tenant = await new TenantService(HttpClient).GetAsync<ExchangeTenant>(new(), tenantName);
        if (tenant == null)
        {
            throw new PayrollException($"Unknown tenant with identifier {tenantName}");
        }
        return tenant;
    }
}