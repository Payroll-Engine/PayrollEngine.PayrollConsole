using System;
using System.IO;
using System.Threading.Tasks;
using NPOI.SS.UserModel;
using NPOI.XSSF.UserModel;
using PayrollEngine.Client;
using PayrollEngine.Client.Model;
using PayrollEngine.Client.Scripting;

namespace PayrollEngine.PayrollConsole.Commands.Excel;

internal static class ExchangeRegulationImport
{
    internal static async Task<Exchange> ReadAsync(string fileName)
    {
        if (string.IsNullOrWhiteSpace(fileName))
        {
            throw new ArgumentException(nameof(fileName));
        }

        // import file
        if (!File.Exists(fileName))
        {
            throw new PayrollException($"Missing Payroll Excel file {fileName}.");
        }

        // workbook (file share to load open/locked excel document)
        await using var stream = new FileStream(fileName, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
        IWorkbook workbook = new XSSFWorkbook(stream);

        // read tenant
        var tenant = ReadTenant(workbook);

        // read regulation
        var regulation = RegulationImport.Import(workbook);
        tenant.Regulations.Add(regulation);
        // case
        regulation.Cases = CaseImport.Import(workbook);
        CaseFieldConvert.ReadCaseFields(workbook, regulation.Cases);
        regulation.CaseRelations = CaseRelationImport.Import(workbook);
        // payrun
        regulation.Collectors = CollectorImport.Import(workbook);
        regulation.WageTypes = WageTypeImport.Import(workbook);
        // report
        regulation.Reports = ReportImport.Import(workbook);
        ReportParameterImport.Import(workbook, regulation.Reports);
        ReportTemplateImport.Import(workbook, regulation.Reports);
        // lookup
        regulation.Lookups = LookupImport.Import(workbook);
        LookupValueImport.Import(workbook, regulation.Lookups);
        // script
        regulation.Scripts = ScriptImport.Import(workbook);

        // created date
        var createdObjectDate = workbook.GetNamedValue<DateTime?>(RegionNames.CreatedObjectDateRegionName);
        if (createdObjectDate != null)
        {
            createdObjectDate = createdObjectDate.Value.ToUtcTime();
        }

        return new()
        {
            CreatedObjectDate = createdObjectDate,
            Schema = workbook.GetNamedValue<string>(RegionNames.JsonSchemaRegionName),
            Tenants = [tenant]
        };
    }

    private static ExchangeTenant ReadTenant(IWorkbook workbook)
    {
        var tenantName = workbook.GetNamedValue<string>(RegionNames.TenantRegionName);
        if (string.IsNullOrWhiteSpace(tenantName))
        {
            throw new PayrollException("Missing tenant identifier.");
        }

        var tenant = new ExchangeTenant
        {
            Identifier = tenantName,
            // tenant create, no update
            UpdateMode = UpdateMode.NoUpdate,
            Regulations = []
        };
        return tenant;
    }
}