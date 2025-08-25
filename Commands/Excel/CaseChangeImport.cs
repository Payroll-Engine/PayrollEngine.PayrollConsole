using System;
using System.Linq;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.IO;
using NPOI.SS.UserModel;
using NPOI.XSSF.UserModel;
using PayrollEngine.Client;
using PayrollEngine.Client.Model;
using PayrollEngine.Client.Service;
using PayrollEngine.Client.Service.Api;
using Task = System.Threading.Tasks.Task;

namespace PayrollEngine.PayrollConsole.Commands.Excel;

internal sealed class CaseChangeImport
{
    private PayrollHttpClient HttpClient { get; }

    internal CaseChangeImport(PayrollHttpClient httpClient)
    {
        HttpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
    }

    internal async Task<Exchange> ReadCaseChangesAsync(string fileName, string overrideTenant = null)
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

        // import tenant
        var tenant = await GetTenantAsync(workbook);
        // tenant override
        if (!string.IsNullOrWhiteSpace(overrideTenant))
        {
            tenant.Identifier = overrideTenant;
        }

        // import case changes
        await ReadAsync(HttpClient, tenant, workbook, fileName);

        return new()
        {
            Tenants = [tenant]
        };
    }

    private async Task<ExchangeTenant> GetTenantAsync(IWorkbook workbook)
    {
        var tenantName = workbook.GetNamedValue<string>(RegionNames.TenantRegionName);
        if (string.IsNullOrWhiteSpace(tenantName))
        {
            throw new PayrollException("Missing tenant identifier.");
        }

        // get existing tenant
        var tenant = await new TenantService(HttpClient).GetAsync<ExchangeTenant>(new(), tenantName);
        if (tenant == null)
        {
            throw new PayrollException($"Unknown tenant with identifier {tenantName}.");
        }
        return tenant;
    }

    /// <summary>
    /// Read case changes from excel file
    /// </summary>
    /// <param name="httpClient">Http client</param>
    /// <param name="tenant">Tenant</param>
    /// <param name="workbook">Excel workbook</param>
    /// <param name="fileName">Excel file name</param>
    private static async Task ReadAsync(PayrollHttpClient httpClient,
        ExchangeTenant tenant, IWorkbook workbook, string fileName)
    {
        var user = await GetUserAsync(httpClient, workbook, tenant.Id);
        var division = await GetDivisionAsync(httpClient, workbook, tenant.Id);
        var caseValueReason = GetCaseValueReason(workbook, fileName);
        var @namespace = GetNamespace(workbook);

        // payroll
        var payroll = await GetPayrollAsync(httpClient, workbook, tenant.Id);
        payroll.Cases = [];
        tenant.Payrolls ??= [];
        tenant.Payrolls.Add(payroll);

        // case changes
        await GetCaseChangesAsync(httpClient, tenant, workbook, division, user, caseValueReason, payroll);

        // case data
        GetCaseData(workbook, division, user, payroll, caseValueReason, @namespace);
    }

    #region Case Data

    /// <summary>
    /// Get case data
    /// </summary>
    /// <param name="workbook">Workbook</param>
    /// <param name="division">The division</param>
    /// <param name="user">The user</param>
    /// <param name="payroll">The payroll</param>
    /// <param name="reason">The reason</param>
    /// <param name="namespace">The case and case field namespace</param>
    private static void GetCaseData(IWorkbook workbook, Division division, User user,
        PayrollSet payroll, string reason, string @namespace)
    {
        foreach (var sheet in workbook.GetSheetsOf(CaseDataSheetSpecification.DataSheetPrefix))
        {
            AddCaseData(sheet, payroll, division, user, reason, @namespace);
        }
    }

    /// <summary>Add case data</summary>
    /// <param name="worksheet">The worksheet</param>
    /// <param name="payroll">The payroll</param>
    /// <param name="division">The division</param>
    /// <param name="user">The user</param>
    /// <param name="reason">The reason</param>
    /// <param name="namespace">The case and case field namespace</param>
    private static void AddCaseData(ISheet worksheet, PayrollSet payroll,
        Division division, User user, string reason, string @namespace)
    {
        // columns
        var titles = new[]
        {
            CaseDataSheetSpecification.Employee,
            CaseDataSheetSpecification.Created
        };
        var columns = worksheet.GetColumnIndexes(titles);

        var caseColumns = GetCaseColumns(worksheet, titles.Length);
        if (!caseColumns.Any())
        {
            return;
        }

        if (!string.IsNullOrWhiteSpace(@namespace))
        {
            @namespace = @namespace.EnsureEnd(".");
        }

        var caseName = @namespace + worksheet.SheetName.RemoveFromStart(CaseDataSheetSpecification.DataSheetPrefix);

        // collect case values from the rows (skip header row)
        for (var i = 1; i < worksheet.PhysicalNumberOfRows; i++)
        {
            var row = worksheet.GetRow(i);
            if (row.IsBlank())
            {
                continue;
            }

            var employee = row.GetCell(columns[CaseDataSheetSpecification.Employee]).GetCellValue<string>();
            var created = row.GetCell(columns[CaseDataSheetSpecification.Created]).GetCellValue<DateTime?>();

            // case values
            var caseValues = new List<CaseValueSetup>();
            foreach (var caseColumn in caseColumns)
            {
                var caseFieldName = @namespace + caseColumn.Key;
                var cell = row.GetCell(caseColumn.Value);
                var value = cell.GetCellValue();
                if (value == null)
                {
                    continue;
                }

                var caseValue = new CaseValueSetup
                {
                    DivisionId = division.Id,
                    CaseName = caseName,
                    CaseFieldName = caseFieldName,
                    Value = ValueConvert.ToJson(value),
                    ValueType = cell.GetValueType()
                };
                caseValues.Add(caseValue);
            }
            if (!caseValues.Any())
            {
                continue;
            }

            // case change
            var caseChangeSetup = new CaseChangeSetup
            {
                DivisionId = division.Id,
                Reason = reason,
                UserIdentifier = user.Identifier,
                UserId = user.Id,
                EmployeeIdentifier = employee,
                Case = new()
                {
                    CaseName = caseName,
                    Values = caseValues
                }
            };
            if (created.HasValue)
            {
                caseChangeSetup.Created = created.Value;
            }
            payroll.Cases.Add(caseChangeSetup);
        }
    }

    /// <summary>
    /// Gets sheet column indexed by column name
    /// </summary>
    /// <param name="worksheet">The worksheet</param>
    /// <param name="firstColumnIndex">The first column index</param>
    private static IDictionary<string, int> GetCaseColumns(ISheet worksheet, int firstColumnIndex)
    {
        var columns = new Dictionary<string, int>();
        var headerCells = worksheet.HeaderCells();
        for (var i = firstColumnIndex; i < headerCells.Count; i++)
        {
            var cell = headerCells[i];
            if (cell == null)
            {
                continue;
            }
            var columnName = cell.StringCellValue.Trim();
            columns.Add(columnName.RemoveFromStart(CaseDataSheetSpecification.DataSheetPrefix), i);

        }
        return columns;
    }

    #endregion

    #region Case Change

    /// <summary>
    /// Get case changes
    /// </summary>
    /// <param name="httpClient">Http client</param>
    /// <param name="tenant"></param>
    /// <param name="workbook"></param>
    /// <param name="division"></param>
    /// <param name="user"></param>
    /// <param name="caseValueReason"></param>
    /// <param name="payroll"></param>
    /// <returns></returns>
    private static async Task GetCaseChangesAsync(PayrollHttpClient httpClient, ExchangeTenant tenant,
        IWorkbook workbook, Division division, User user, string caseValueReason, PayrollSet payroll)
    {
        // global case value changes
        if (workbook.HasSheet(SheetSpecification.GlobalCaseValues))
        {
            var sheet = workbook.GetSheet(SheetSpecification.GlobalCaseValues);
            var globalCases = GetCaseChange(sheet, division, user, caseValueReason);
            if (globalCases != null)
            {
                payroll.Cases.Add(globalCases);
            }
        }

        // national case value changes
        if (workbook.HasSheet(SheetSpecification.NationalCaseValues))
        {
            var sheet = workbook.GetSheet(SheetSpecification.NationalCaseValues);
            var nationalCases = GetCaseChange(sheet, division, user, caseValueReason);
            if (nationalCases != null)
            {
                payroll.Cases.Add(nationalCases);
            }
        }

        // company case value changes
        if (workbook.HasSheet(SheetSpecification.CompanyCaseValues))
        {
            var sheet = workbook.GetSheet(SheetSpecification.CompanyCaseValues);
            var companyCases = GetCaseChange(sheet, division, user, caseValueReason);
            if (companyCases != null)
            {
                payroll.Cases.Add(companyCases);
            }
        }

        // employee case value changes
        if (workbook.HasSheet(SheetSpecification.EmployeeCaseValues))
        {
            var sheet = workbook.GetSheet(SheetSpecification.EmployeeCaseValues);
            var employeeCases = await GetEmployeeCaseChangesAsync(httpClient, tenant, sheet, division, user, caseValueReason);
            foreach (var employeeCase in employeeCases)
            {
                payroll.Cases.Add(employeeCase.Value);
            }
        }
    }

    /// <summary>Gets the case change</summary>
    /// <param name="worksheet">The worksheet</param>
    /// <param name="division">The division</param>
    /// <param name="user">The user</param>
    /// <param name="reason">The reason</param>
    /// <returns>Case change setup</returns>
    private static CaseChangeSetup GetCaseChange(ISheet worksheet, Division division, User user, string reason)
    {
        // columns
        var titles = new[]
        {
            CaseValueSheetSpecification.CaseChange,
            CaseValueSheetSpecification.CaseName,
            CaseValueSheetSpecification.CaseFieldName,
            CaseValueSheetSpecification.CaseSlot,
            CaseValueSheetSpecification.Created,
            CaseValueSheetSpecification.Value,
            CaseValueSheetSpecification.Start,
            CaseValueSheetSpecification.End,
            CaseValueSheetSpecification.Cancellation
        };
        var columns = worksheet.GetColumnIndexes(titles);

        // collect case values from the rows (skip header row)
        var caseValues = new List<CaseValueSetup>();
        for (var i = 1; i < worksheet.PhysicalNumberOfRows; i++)
        {
            var row = worksheet.GetRow(i);
            if (row.IsBlank())
            {
                continue;
            }

            var caseName = row.GetCell(columns[SpecificationEmployeeCaseValue.CaseName]).GetCellValue<string>();
            var caseFieldName = row.GetCell(columns[CaseValueSheetSpecification.CaseFieldName]).GetCellValue<string>();
            var caseSlot = row.GetCell(columns[CaseValueSheetSpecification.CaseSlot]).GetCellValue<string>();
            var created = row.GetCell(columns[CaseValueSheetSpecification.Created]).GetCellValue<DateTime?>();
            var value = row.GetCell(columns[CaseValueSheetSpecification.Value]).GetCellValue();
            var start = row.GetCell(columns[CaseValueSheetSpecification.Start]).GetCellValue<DateTime>();
            var end = row.GetCell(columns[CaseValueSheetSpecification.End]).GetCellValue<DateTime?>();
            var cancellationDate = row.GetCell(columns[CaseValueSheetSpecification.Cancellation]).GetCellValue<DateTime?>();

            if (value == null)
            {
                throw new PayrollException($"Missing Lookup value in row {i + 1}.");
            }

            var caseValue = new CaseValueSetup
            {
                DivisionId = division.Id,
                CaseName = caseName ?? caseFieldName, // fallback to case field name
                CaseFieldName = caseFieldName,
                CaseSlot = caseSlot,
                Value = value.ToString(),
                Start = start,
                End = end,
                CancellationDate = cancellationDate
            };

            if (created.HasValue)
            {
                caseValue.Created = created.Value;
            }

            caseValues.Add(caseValue);
        }

        // root case
        var rootCaseValue = caseValues.FirstOrDefault();
        if (rootCaseValue == null)
        {
            return null;
        }

        // case change and root case setup
        var caseChangeSetup = new CaseChangeSetup
        {
            DivisionId = division.Id,
            Reason = reason,
            UserIdentifier = user.Identifier,
            UserId = user.Id,
            Case = new()
            {
                CaseName = rootCaseValue.CaseName,
            }
        };

        // case values
        // currently only one level of relation supported
        var caseValuesByCases = caseValues.GroupBy(x => x.CaseName, x => x);
        foreach (var caseValuesByCase in caseValuesByCases)
        {
            if (string.Equals(caseValuesByCase.Key, rootCaseValue.CaseName))
            {
                // root case value
                caseChangeSetup.Case.Values = caseValuesByCase.ToList();
            }
            else
            {
                // related case value
                caseChangeSetup.Case.RelatedCases ??= [];
                caseChangeSetup.Case.RelatedCases.Add(new()
                {
                    CaseName = caseValuesByCase.Key,
                    Values = caseValuesByCase.ToList()
                });
            }
        }

        return caseChangeSetup;
    }

    /// <summary>Gets the employee case changes grouped by employee identifier</summary>
    /// <param name="httpClient">The http client</param>
    /// <param name="tenant">The tenant</param>
    /// <param name="worksheet">The worksheet</param>
    /// <param name="division">The division</param>
    /// <param name="user">The user</param>
    /// <param name="reason">The reason</param>
    /// <returns>Case changes by employee</returns>
    private static async Task<IDictionary<Tuple<string, string>, CaseChangeSetup>> GetEmployeeCaseChangesAsync(
        PayrollHttpClient httpClient, ExchangeTenant tenant,
        ISheet worksheet, Division division, User user, string reason)
    {
        // columns
        var titles = new[]
        {
            SpecificationEmployeeCaseValue.Identifier,
            SpecificationEmployeeCaseValue.CaseChange,
            SpecificationEmployeeCaseValue.CaseName,
            SpecificationEmployeeCaseValue.CaseFieldName,
            SpecificationEmployeeCaseValue.CaseSlot,
            SpecificationEmployeeCaseValue.Created,
            SpecificationEmployeeCaseValue.Value,
            SpecificationEmployeeCaseValue.Start,
            SpecificationEmployeeCaseValue.End,
            SpecificationEmployeeCaseValue.Cancellation
        };
        var columns = worksheet.GetColumnIndexes(titles);

        // collect case values from the rows (skip header row), by employee identifier and case change
        var caseValuesByChanges = new Dictionary<Tuple<string, string>, List<CaseValueSetup>>();
        for (var i = 1; i < worksheet.PhysicalNumberOfRows; i++)
        {
            var row = worksheet.GetRow(i);
            if (row.IsBlank())
            {
                continue;
            }

            var identifier = row.GetCell(columns[SpecificationEmployeeCaseValue.Identifier]).GetCellValue<string>();
            var caseChange = row.GetCell(columns[SpecificationEmployeeCaseValue.CaseChange]).GetCellValue<string>();
            var caseName = row.GetCell(columns[SpecificationEmployeeCaseValue.CaseName]).GetCellValue<string>();
            var caseFieldName = row.GetCell(columns[SpecificationEmployeeCaseValue.CaseFieldName]).GetCellValue<string>();
            var caseSlot = row.GetCell(columns[SpecificationEmployeeCaseValue.CaseSlot]).GetCellValue<string>();
            var created = row.GetCell(columns[SpecificationEmployeeCaseValue.Created]).GetCellValue<DateTime?>();
            var value = row.GetCell(columns[SpecificationEmployeeCaseValue.Value]).GetCellValue();
            var start = row.GetCell(columns[SpecificationEmployeeCaseValue.Start]).GetCellValue<DateTime?>();
            var end = row.GetCell(columns[SpecificationEmployeeCaseValue.End]).GetCellValue<DateTime?>();
            var cancellationDate = row.GetCell(columns[SpecificationEmployeeCaseValue.Cancellation]).GetCellValue<DateTime?>();

            // group case changes by employee and case change
            var key = new Tuple<string, string>(identifier, caseChange);
            if (!caseValuesByChanges.ContainsKey(key))
            {
                caseValuesByChanges.Add(key, []);
            }

            var caseValue = new CaseValueSetup
            {
                // ensure same division on case change and case value
                DivisionId = division.Id,
                DivisionName = division.Name,
                CaseName = caseName ?? caseFieldName, // fallback to case field name
                CaseFieldName = caseFieldName,
                CaseSlot = caseSlot,
                Value = value.ToString(),
                Start = start,
                End = end,
                CancellationDate = cancellationDate
            };

            if (created.HasValue)
            {
                caseValue.Created = created.Value;
            }

            caseValuesByChanges[key].Add(caseValue);
        }

        // apply employee changes, grouped by employee identifier and case change
        var context = new TenantServiceContext(tenant.Id);
        var caseSetupByChanges = new Dictionary<Tuple<string, string>, CaseChangeSetup>();
        foreach (var caseValuesByChange in caseValuesByChanges)
        {
            // employee
            var employeeIdentifier = caseValuesByChange.Key.Item1;
            var employee = await new EmployeeService(httpClient).GetAsync<EmployeeSet>(context, employeeIdentifier);
            if (employee == null)
            {
                throw new PayrollException($"Unknown employee {employeeIdentifier}.");
            }
            // ensure employee in tenant
            tenant.Employees ??= [];
            if (!tenant.Employees.Any(x => string.Equals(x.Identifier, employee.Identifier)))
            {
                tenant.Employees.Add(employee);
            }

            // root case
            var rootCaseValue = caseValuesByChange.Value.FirstOrDefault();
            if (rootCaseValue == null)
            {
                continue;
            }

            // case change and root case setup
            var caseChangeSetup = new CaseChangeSetup
            {
                DivisionId = division.Id,
                DivisionName = division.Name,
                EmployeeId = employee.Id,
                EmployeeIdentifier = employee.Identifier,
                Reason = reason,
                UserIdentifier = user.Identifier,
                UserId = user.Id,
                Case = new()
                {
                    CaseName = rootCaseValue.CaseName,
                }
            };

            // case values
            // currently only one level of relation supported
            var caseValuesByCases = caseValuesByChange.Value.GroupBy(x => x.CaseName, x => x);
            foreach (var caseValuesByCase in caseValuesByCases)
            {
                if (string.Equals(caseValuesByCase.Key, rootCaseValue.CaseName))
                {
                    // root case value
                    caseChangeSetup.Case.Values = caseValuesByCase.ToList();
                }
                else
                {
                    // related case value
                    caseChangeSetup.Case.RelatedCases ??= [];
                    caseChangeSetup.Case.RelatedCases.Add(new()
                    {
                        CaseName = caseValuesByCase.Key,
                        Values = caseValuesByCase.ToList()
                    });
                }
            }
            caseSetupByChanges.Add(caseValuesByChange.Key, caseChangeSetup);
        }
        return caseSetupByChanges;
    }

    #endregion

    private static string GetCaseValueReason(IWorkbook workbook, string fileName)
    {
        var caseValueReason = workbook.GetNamedValue<string>(RegionNames.CaseValueReasonRegionName);
        return string.IsNullOrWhiteSpace(caseValueReason) ?
            $"Import case values from {fileName}" :
            caseValueReason;
    }

    private static string GetNamespace(IWorkbook workbook) =>
        workbook.GetNamedValue<string>(RegionNames.NamespaceRegionName);

    private static async Task<User> GetUserAsync(PayrollHttpClient httpClient, IWorkbook workbook, int tenantId)
    {
        var userIdentifier = workbook.GetNamedValue<string>(RegionNames.UserRegionName);
        if (string.IsNullOrWhiteSpace(userIdentifier))
        {
            throw new PayrollException("Missing user identifier.");
        }

        // get existing user
        var user = await new UserService(httpClient).GetAsync<User>(new(tenantId), userIdentifier);
        if (user == null)
        {
            throw new PayrollException($"Unknown user with identifier {userIdentifier}.");
        }
        return user;
    }

    private static async Task<Division> GetDivisionAsync(PayrollHttpClient httpClient, IWorkbook workbook, int tenantId)
    {
        var divisionName = workbook.GetNamedValue<string>(RegionNames.DivisionRegionName);
        if (string.IsNullOrWhiteSpace(divisionName))
        {
            throw new PayrollException("Missing division identifier.");
        }

        // get existing division
        var division = await new DivisionService(httpClient).GetAsync<Division>(new(tenantId), divisionName);
        if (division == null)
        {
            throw new PayrollException($"Unknown division with identifier {divisionName}.");
        }
        return division;
    }

    private static async Task<PayrollSet> GetPayrollAsync(PayrollHttpClient httpClient, IWorkbook workbook, int tenantId)
    {
        var payrollName = workbook.GetNamedValue<string>(RegionNames.PayrollRegionName);
        if (string.IsNullOrWhiteSpace(payrollName))
        {
            throw new PayrollException("Missing payroll name.");
        }

        // get existing payroll
        var payroll = await new PayrollService(httpClient).GetAsync<PayrollSet>(new(tenantId), payrollName);
        if (payroll == null)
        {
            throw new PayrollException($"Unknown payroll with name {payrollName}.");
        }
        return payroll;
    }
}