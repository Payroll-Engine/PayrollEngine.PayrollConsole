using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using NPOI.SS.UserModel;
using PayrollEngine.Client;
using PayrollEngine.Client.Model;
using PayrollEngine.Client.Service;
using PayrollEngine.Client.Service.Api;
using PayrollEngine.PayrollConsole.Excel;
using Task = System.Threading.Tasks.Task;

namespace PayrollEngine.PayrollConsole.Command;

internal static class ExchangeExcelCaseReader
{
    internal static async Task ReadAsync(PayrollHttpClient httpClient,
        ExchangeTenant tenant, IWorkbook workbook, string fileName)
    {
        var user = await GetUserAsync(httpClient, workbook, tenant.Id);
        var division = await GetDivisionAsync(httpClient, workbook, tenant.Id);
        var caseValueReason = GetCaseValueReason(workbook, fileName);

        // payroll
        var payroll = await GetPayrollAsync(httpClient, workbook, tenant.Id);
        payroll.Cases = [];
        tenant.Payrolls ??= [];
        tenant.Payrolls.Add(payroll);

        // national case value changes
        if (workbook.HasSheet(SpecificationSheet.NationalCaseValues))
        {
            var sheet = workbook.GetSheet(SpecificationSheet.NationalCaseValues);
            var nationalCases = GetCaseChange(sheet, division, user, caseValueReason);
            if (nationalCases != null)
            {
                payroll.Cases.Add(nationalCases);
            }
        }

        // company case value changes
        if (workbook.HasSheet(SpecificationSheet.CompanyCaseValues))
        {
            var sheet = workbook.GetSheet(SpecificationSheet.CompanyCaseValues);
            var companyCases = GetCaseChange(sheet, division, user, caseValueReason);
            if (companyCases != null)
            {
                payroll.Cases.Add(companyCases);
            }
        }

        // employee case value changes
        if (workbook.HasSheet(SpecificationSheet.EmployeeCaseValues))
        {
            var sheet = workbook.GetSheet(SpecificationSheet.EmployeeCaseValues);
            var employeeCases = await GetEmployeeCaseChanges(httpClient, tenant, sheet, division, user, caseValueReason);
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
    /// <returns>Case change</returns>
    private static CaseChangeSetup GetCaseChange(ISheet worksheet, Division division, User user, string reason)
    {
        // columns
        var titles = new[]
        {
            SpecificationCaseValue.CaseChange,
            SpecificationCaseValue.CaseName,
            SpecificationCaseValue.CaseFieldName,
            SpecificationCaseValue.CaseSlot,
            SpecificationCaseValue.Created,
            SpecificationCaseValue.Value,
            SpecificationCaseValue.Start,
            SpecificationCaseValue.End,
            SpecificationCaseValue.Cancellation
        };
        var columns = GetColumnIndexes(worksheet, titles);

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
            var caseFieldName = row.GetCell(columns[SpecificationCaseValue.CaseFieldName]).GetCellValue<string>();
            var caseSlot = row.GetCell(columns[SpecificationCaseValue.CaseSlot]).GetCellValue<string>();
            var created = row.GetCell(columns[SpecificationCaseValue.Created]).GetCellValue<DateTime?>();
            var value = row.GetCell(columns[SpecificationCaseValue.Value]).GetCellValue();
            var start = row.GetCell(columns[SpecificationCaseValue.Start]).GetCellValue<DateTime>();
            var end = row.GetCell(columns[SpecificationCaseValue.End]).GetCellValue<DateTime?>();
            var cancellationDate = row.GetCell(columns[SpecificationCaseValue.Cancellation]).GetCellValue<DateTime?>();

            if (value == null)
            {
                throw new PayrollException($"Missing Lookup value in row {i + 1}");
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
    private static async Task<IDictionary<Tuple<string, string>, CaseChangeSetup>> GetEmployeeCaseChanges(
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
        var columns = GetColumnIndexes(worksheet, titles);

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
                throw new PayrollException($"Unknown employee {employeeIdentifier}");
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

    /// <summary>
    /// Gets sheet column indexed by column name
    /// </summary>
    /// <param name="worksheet">The worksheet</param>
    /// <param name="titles">The column titles</param>
    private static IDictionary<string, int> GetColumnIndexes(ISheet worksheet, ICollection<string> titles)
    {
        var columns = new Dictionary<string, int>();
        foreach (var headerCell in worksheet.HeaderCells())
        {
            var columnName = headerCell.StringCellValue.Trim();
            if (titles.Contains(columnName))
            {
                columns.Add(columnName, headerCell.ColumnIndex);
            }
        }

        // verify that all columns are present
        foreach (var title in titles)
        {
            if (!columns.ContainsKey(title))
            {
                throw new PayrollException($"Missing Excel column {title}");
            }
        }

        return columns;
    }

    private static async Task<User> GetUserAsync(PayrollHttpClient httpClient, IWorkbook workbook, int tenantId)
    {
        var userIdentifier = workbook.GetNamedValue<string>(Specification.UserRegionName);
        if (string.IsNullOrWhiteSpace(userIdentifier))
        {
            throw new PayrollException("Missing user identifier");
        }

        // get existing user
        var user = await new UserService(httpClient).GetAsync<User>(new(tenantId), userIdentifier);
        if (user == null)
        {
            throw new PayrollException($"Unknown user with identifier {userIdentifier}");
        }
        return user;
    }

    private static async Task<Division> GetDivisionAsync(PayrollHttpClient httpClient, IWorkbook workbook, int tenantId)
    {
        var divisionName = workbook.GetNamedValue<string>(Specification.DivisionRegionName);
        if (string.IsNullOrWhiteSpace(divisionName))
        {
            throw new PayrollException("Missing division identifier");
        }

        // get existing division
        var division = await new DivisionService(httpClient).GetAsync<Division>(new(tenantId), divisionName);
        if (division == null)
        {
            throw new PayrollException($"Unknown division with identifier {divisionName}");
        }
        return division;
    }

    private static async Task<PayrollSet> GetPayrollAsync(PayrollHttpClient httpClient, IWorkbook workbook, int tenantId)
    {
        var payrollName = workbook.GetNamedValue<string>(Specification.PayrollRegionName);
        if (string.IsNullOrWhiteSpace(payrollName))
        {
            throw new PayrollException("Missing payroll name");
        }

        // get existing payroll
        var payroll = await new PayrollService(httpClient).GetAsync<PayrollSet>(new(tenantId), payrollName);
        if (payroll == null)
        {
            throw new PayrollException($"Unknown payroll with name {payrollName}");
        }
        return payroll;
    }

    private static string GetCaseValueReason(IWorkbook workbook, string fileName)
    {
        var caseValueReason = workbook.GetNamedValue<string>(Specification.CaseValueReasonRegionName);
        return string.IsNullOrWhiteSpace(caseValueReason) ?
            $"Import case values from {fileName}" :
            caseValueReason;
    }
}