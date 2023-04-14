using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using PayrollEngine.Client;
using PayrollEngine.Client.Model;
using PayrollEngine.Client.Service;
using PayrollEngine.Client.Service.Api;
using PayrollEngine.PayrollConsole.Shared;
using Task = System.Threading.Tasks.Task;

namespace PayrollEngine.PayrollConsole.Command;

public class PayrollResultsReport
{
    private sealed class ReportColumn
    {
        internal ReportColumn(string name, bool alignment, int width)
        {
            Name = name;
            Alignment = alignment;
            Width = width;
        }
        internal string Name { get; }
        internal bool Alignment { get; }
        internal int Width { get; }
    }

    private static readonly ReportColumn[] ReportColumns = {
        new("source", false, 16),
        new("key", false, 15),
        new("tags", false, 15),
        new("id", false, 8),
        new("name", false, 24),
        new("start", false, 12),
        new("end", false, 12),
        new("type", false, 10),
        new("attributes", false, 36),
        new("value", true, 12)
    };

    public static readonly string ExportSeparator = "\t";
    public static readonly string ResultsFolderName = "Results";

    public PayrollHttpClient HttpClient { get; }
    public int TopFilter { get; }
    public ReportExportMode ExportMode { get; }

    public PayrollResultsReport(PayrollHttpClient httpClient, int topFilter, ReportExportMode exportMode)
    {
        HttpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
        TopFilter = topFilter;
        ExportMode = exportMode;
    }

    public async Task ConsoleWriteAsync(string tenantIdentifier)
    {
        // tenant
        if (string.IsNullOrWhiteSpace(tenantIdentifier))
        {
            throw new PayrollException("Missing tenant identifier");
        }
        var tenant = await GetTenantAsync(tenantIdentifier);
        if (tenant == null)
        {
            throw new PayrollException($"Unknown tenant with identifier {tenantIdentifier}");
        }

        // top filter
        if (TopFilter < 1 || TopFilter > 100)
        {
            throw new PayrollException($"Top filter {TopFilter} out of range (1-100)");
        }

        // payrun jobs
        var context = new TenantServiceContext(tenant.Id);
        var jobQuery = new Query
        {
            Status = ObjectStatus.Active,
            OrderBy = $"{nameof(IModel.Created)} desc",
            Top = TopFilter
        };
        var payrunJobs = (await new PayrunJobService(HttpClient).QueryAsync<PayrunJob>(context, jobQuery)).ToList();
        if (payrunJobs.Count == 0)
        {
            Log.Information($"No payrun jobs found for tenant {tenantIdentifier} (filter={TopFilter})");
            return;
        }

        var resultValues = new List<object[]>();
        var jobMarker = new string('=', 30);
        var resultMarker = new string('-', 30);
        foreach (var payrunJob in payrunJobs)
        {
            Console.WriteLine();
            Console.WriteLine($"{jobMarker} PayrunJob {payrunJobs.IndexOf(payrunJob) + 1} of {payrunJobs.Count} {jobMarker}");

            // result set
            var resultQuery = QueryFactory.NewEqualFilterQuery(nameof(PayrollResult.PayrunJobId), payrunJob.Id);
            var resultSets = (await new PayrollResultService(HttpClient).QueryPayrollResultSetsAsync<PayrollResultSet>(context, resultQuery)).ToList();

            // missing results
            if (resultSets.Count == 0)
            {
                Log.Error($"Missing payroll results for payrun job {payrunJob.Name}{Environment.NewLine}{payrunJob.Message}");
                continue;
            }

            foreach (var resultSet in resultSets)
            {
                // payroll
                var payroll = await GetPayrollAsync(tenant.Id, payrunJob.PayrollId);
                if (payroll == null)
                {
                    Console.WriteLine($"Missing payroll with id {payrunJob.PayrollId}");
                    continue;
                }

                // payrun
                var payrun = await GetPayrunAsync(tenant.Id, payrunJob.PayrunId);
                if (payrun == null)
                {
                    Console.WriteLine($"Missing payrun with id {payrunJob.PayrunId}");
                    continue;
                }

                // division
                var division = await new DivisionService(HttpClient).GetAsync<Division>(context, payroll.DivisionId);

                // employee
                var employee = await GetEmployeeAsync(tenant.Id, resultSet.EmployeeId);
                if (employee == null)
                {
                    Console.WriteLine($"Missing employee with id {resultSet.EmployeeId}");
                    continue;
                }

                // report item header
                Console.WriteLine();
                Console.WriteLine($"{resultMarker} Results {resultSets.IndexOf(resultSet) + 1} of {resultSets.Count} {resultMarker}");
                Console.WriteLine();
                Console.WriteLine($"Tenant           {tenant.Identifier} [#{tenant.Id}]");
                Console.WriteLine($"Employee         {employee.Identifier} [#{employee.Id}]");
                Console.WriteLine($"Payroll          {payroll.Name} [#{payroll.Id}]");
                Console.WriteLine($"Division         {division.Name} [#{division.Id}]");
                Console.WriteLine($"Payrun           {payrun.Name} [#{payrun.Id}]");
                Console.WriteLine($"Payrun job       {payrunJob.Name} [#{payrunJob.Id}]");
                Console.WriteLine($"Result           {resultSet.Created} [#{resultSet.Id}]");
                if (!string.IsNullOrWhiteSpace(payrunJob.Forecast))
                {
                    Console.WriteLine($"Forecast         {payrunJob.Forecast}");
                }
                Console.WriteLine($"Culture          {payrunJob.Culture}");
                Console.WriteLine($"Period           {payrunJob.PeriodName} ({payrunJob.CycleName})");
                Console.WriteLine($"Job status       {payrunJob.JobStatus}");
                Console.WriteLine($"Job start        {payrunJob.JobStart.ToUtc()}");
                Console.WriteLine($"Job end          {payrunJob.JobEnd?.ToUtc()}");

                Console.WriteLine($"Reason           {payrunJob.Reason}");
                Console.WriteLine($"Message          {payrunJob.Message}");
                if (!string.IsNullOrWhiteSpace(payrunJob.ErrorMessage))
                {
                    Console.WriteLine($"Error            {payrunJob.ErrorMessage}");
                }

                // stats
                Console.WriteLine();
                var jobPeriod = new DatePeriod(payrunJob.JobStart, payrunJob.JobEnd);
                Console.WriteLine($"Duration         {jobPeriod.Duration.TotalMilliseconds:#0} ms");
                Console.WriteLine($"# Wage Types     {resultSet.WageTypeResults?.Count ?? 0}");
                Console.WriteLine($"# Collectors     {resultSet.CollectorResults?.Count ?? 0}");
                Console.WriteLine();

                var columnNames = ReportColumns.Select(x => (object)x.Name).ToArray();
                Console.WriteLine(FormatConsoleResults(columnNames));
                Console.WriteLine(MaskResults(columnNames, '-'));

                // wage type results
                if (resultSet.WageTypeResults != null)
                {
                    foreach (var wageTypeResult in resultSet.WageTypeResults)
                    {
                        //var wageTypeName = wageTypeResult.WageTypeName;
                        var values = new object[] {
                            "WageType",
                            wageTypeResult.WageTypeNumber.ToString("0.####", CultureInfo.InvariantCulture),
                            FormatValue(wageTypeResult.Tags, "tags"),
                            wageTypeResult.Id.ToString(),
                            FormatValue(wageTypeResult.WageTypeName, "name"),
                            wageTypeResult.Start.ToPeriodStartString(),
                            wageTypeResult.End.ToPeriodEndString(),
                            null,
                            FormatValue(wageTypeResult.Attributes.ToText(), "attributes"),
                            FormatValue(wageTypeResult.Value)
                        };
                        resultValues.Add(values);
                        Console.WriteLine(FormatConsoleResults(values));

                        // wage type custom results
                        if (wageTypeResult.CustomResults != null && wageTypeResult.CustomResults.Any())
                        {
                            foreach (var customResult in wageTypeResult.CustomResults)
                            {
                                var customValues = new object[] {
                                    "WageTypeCustom",
                                    null, // no number
                                    FormatValue(customResult.Tags, "tags"),
                                    customResult.Id,
                                    FormatValue(customResult.Source, "name"),
                                    customResult.Start.ToPeriodStartString(),
                                    customResult.End.ToPeriodEndString(),
                                    ValueType.Decimal.ToString(),
                                    FormatValue(customResult.Attributes.ToText(), "attributes"),
                                    FormatValue(customResult.Value)
                                };
                                Console.WriteLine(FormatConsoleResults(customValues));
                            }
                        }
                    }
                }

                // collector results
                if (resultSet.CollectorResults != null)
                {
                    foreach (var collectorResult in resultSet.CollectorResults)
                    {
                        var values = new object[] {
                            "Collector",
                            null, // no number
                            FormatValue(collectorResult.Tags, "tags"),
                            collectorResult.Id,
                            FormatValue(collectorResult.CollectorName, "name"),
                            collectorResult.Start.ToPeriodStartString(),
                            collectorResult.End.ToPeriodEndString(),
                            collectorResult.CollectType,
                            FormatValue(collectorResult.Attributes.ToText(), "attributes"),
                            FormatValue(collectorResult.Value)
                        };
                        resultValues.Add(values);
                        Console.WriteLine(FormatConsoleResults(values));

                        // collector custom results
                        if (collectorResult.CustomResults != null && collectorResult.CustomResults.Any())
                        {
                            foreach (var customResult in collectorResult.CustomResults)
                            {
                                var customValues = new object[] {
                                    "CollectorCustom",
                                    null, // no number
                                    FormatValue(customResult.Tags, "tags"),
                                    customResult.Id,
                                    FormatValue(customResult.Source, "name"),
                                    customResult.Start.ToPeriodStartString(),
                                    customResult.End.ToPeriodEndString(),
                                    ValueType.Decimal.ToString(),
                                    FormatValue(customResult.Attributes.ToText(), "attributes"),
                                    FormatValue(customResult.Value)
                                };
                                Console.WriteLine(FormatConsoleResults(customValues));
                            }
                        }
                    }
                }

                // payrun results
                if (resultSet.PayrunResults != null)
                {
                    foreach (var payrunResult in resultSet.PayrunResults)
                    {
                        var values = new object[] {
                            "Payrun",
                            null, // no number
                            FormatValue(payrunResult.Tags, "tags"),
                            payrunResult.Id,
                            FormatValue(payrunResult.Name, "name"),
                            payrunResult.Start.ToPeriodStartString(),
                            payrunResult.End.ToPeriodEndString(),
                            payrunResult.ValueType,
                            null, // no attributes
                            FormatValue(payrunResult.Value, payrunResult.ValueType)
                        };
                        resultValues.Add(values);
                        Console.WriteLine(FormatConsoleResults(values));
                    }
                }

                // results export
                if (ExportMode == ReportExportMode.Export)
                {
                    var targetDirectory = ResultsFolderName;
                    if (!Directory.Exists(targetDirectory))
                    {
                        Directory.CreateDirectory(targetDirectory);
                    }
                    var fileName = $"{targetDirectory}\\PayrollResult#{resultSet.Id}_Employee#{resultSet.EmployeeId}.csv";
                    File.WriteAllLines(fileName, GetExportResults(resultValues));
                }
            }
        }
    }

    private static string FormatValue<T>(IEnumerable<T> values, string columnName)
    {
        if (values == null)
        {
            return null;
        }
        return FormatValue(string.Join(", ", values), columnName);
    }

    private static string FormatValue(string value, string columnName)
    {
        if (string.IsNullOrEmpty(value))
        {
            return null;
        }

        var column = ReportColumns.FirstOrDefault(x => string.Equals(columnName, x.Name));
        if (ReportColumns != null && column != null && value.Length > column.Width && column.Width > 4)
        {
            return $"{value.Substring(0, column.Width - 4)}...";
        }
        return value;
    }

    private static string FormatValue(decimal? value) =>
        value?.ToString("0.000", CultureInfo.InvariantCulture);

    private static string FormatValue(string value, ValueType valueType)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }
        var nativeValue = ValueConvert.ToValue(value, valueType);
        return valueType.IsDecimal() ? FormatValue((decimal)nativeValue) : nativeValue.ToString();
    }

    private static string FormatConsoleResults(params object[] values)
    {
        var line = new StringBuilder();
        var index = 0;
        foreach (var value in values)
        {
            string text;
            if (value == null)
            {
                text = new(' ', ReportColumns[index].Width);
            }
            else
            {
                text = ReportColumns[index].Alignment
                    // ReSharper disable PossibleNullReferenceException
                    ? value.ToString().PadLeft(ReportColumns[index].Width)
                    : value.ToString().PadRight(ReportColumns[index].Width);
                // ReSharper restore PossibleNullReferenceException
            }
            line.Append(text);
            index++;
        }
        return line.ToString();
    }

    private static string MaskResults(object[] values, char c)
    {
        var line = new StringBuilder();
        var index = 0;
        foreach (var value in values)
        {
            string text;
            if (value == null)
            {
                text = new(c, ReportColumns[index].Width);
            }
            else
            {
                // ReSharper disable once PossibleNullReferenceException
                var mask = new string(c, value.ToString().Length);
                text = ReportColumns[index].Alignment
                    ? mask.PadLeft(ReportColumns[index].Width)
                    : mask.PadRight(ReportColumns[index].Width);
            }
            line.Append(text);
            index++;
        }
        return line.ToString();
    }

    private static string[] GetExportResults(List<object[]> valueRows)
    {
        var results = new List<string>
        {
            string.Join(ExportSeparator, ReportColumns.Select(x => x.Name))
        };

        foreach (var valueRow in valueRows)
        {
            var buffer = new StringBuilder();
            foreach (var value in valueRow)
            {
                // delimiter
                if (buffer.Length > 0)
                {
                    buffer.Append(ExportSeparator);
                }
                buffer.Append(value);
            }
            results.Add(buffer.ToString());
        }
        return results.ToArray();
    }

    private async Task<Tenant> GetTenantAsync(string identifier) =>
        await new TenantService(HttpClient).GetAsync<Tenant>(new(), identifier);

    private async Task<Payroll> GetPayrollAsync(int tenantId, int payrollId) =>
        await new PayrollService(HttpClient).GetAsync<Payroll>(new(tenantId), payrollId);

    private async Task<Payrun> GetPayrunAsync(int tenantId, int payrunId) =>
        await new PayrunService(HttpClient).GetAsync<Payrun>(new(tenantId), payrunId);

    private async Task<Employee> GetEmployeeAsync(int tenantId, int employeeId) =>
        await new EmployeeService(HttpClient).GetAsync<Employee>(new(tenantId), employeeId);
}