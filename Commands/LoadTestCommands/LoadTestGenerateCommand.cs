using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using PayrollEngine.Client.Command;
using PayrollEngine.Client.Exchange;
using PayrollEngine.Client.Model;

namespace PayrollEngine.PayrollConsole.Commands.LoadTestCommands;

/// <summary>Generate scaled exchange files for payrun load testing</summary>
[Command("LoadTestGenerate")]
// ReSharper disable once UnusedType.Global
internal sealed class LoadTestGenerateCommand : CommandBase<LoadTestGenerateParameters>
{
    private static readonly JsonSerializerOptions JsonCloneOptions = new()
    {
        PropertyNamingPolicy = null,
        WriteIndented = false
    };

    /// <summary>Generate load test exchange files</summary>
    protected override async Task<int> Execute(CommandContext context, LoadTestGenerateParameters parameters)
    {
        var console = context.Console;

        DisplayTitle(console, "Load test exchange file generation");
        if (context.DisplayLevel == DisplayLevel.Full)
        {
            console.DisplayTextLine($"Template         {parameters.TemplatePath}");
            console.DisplayTextLine($"Employee count   {parameters.EmployeeCount}");
            console.DisplayTextLine($"Output dir       {parameters.OutputDir}");
        }
        console.DisplayNewLine();

        try
        {
            // 1. load template
            var template = await FileReader.ReadAsync<Exchange>(parameters.TemplatePath);
            if (template?.Tenants == null || template.Tenants.Count == 0)
            {
                console.DisplayErrorLine("Invalid template: no tenants found.");
                return -1;
            }

            var templateTenant = template.Tenants[0];

            // payroll with cases
            var templatePayroll = templateTenant.Payrolls?.FirstOrDefault();
            var templateCases = templatePayroll?.Cases;
            if (templateCases == null || templateCases.Count == 0)
            {
                console.DisplayErrorLine("Invalid template: no payroll cases found.");
                return -1;
            }

            // 2. extract employee templates from case changes
            var employeeTemplates = ExtractEmployeeTemplates(templateTenant, templateCases);
            if (employeeTemplates.Count == 0)
            {
                console.DisplayErrorLine("Invalid template: no employee identifiers found in cases.");
                return -1;
            }

            console.DisplayTextLine($"Template: {employeeTemplates.Count} employees, " +
                                    $"{templateCases.Count} case changes");

            // 3. generate scaled employees
            var outputDir = Path.GetFullPath(parameters.OutputDir);
            Directory.CreateDirectory(outputDir);

            var scaledEmployees = GenerateScaledEmployees(employeeTemplates, parameters.EmployeeCount);
            console.DisplayTextLine($"Generated {scaledEmployees.Count} employees");

            // 4. write Setup-Employees.json (for bulk import via LoadTestSetup)
            var employeesExchange = new Exchange
            {
                Tenants =
                [
                    new ExchangeTenant
                    {
                        Identifier = templateTenant.Identifier,
                        Employees = scaledEmployees
                            .Select(e => e.Employee)
                            .ToList()
                    }
                ]
            };
            var employeesFile = Path.Combine(outputDir, "Setup-Employees.json");
            await FileWriter.WriteAsync(employeesExchange, employeesFile);
            console.DisplayTextLine($"  {Path.GetFileName(employeesFile)}");

            // 5. write Setup-Cases-NNN.json (batches of 500 employees)
            const int batchSize = 500;
            var batches = scaledEmployees.Chunk(batchSize).ToList();
            var tickOffset = 1L;
            for (var i = 0; i < batches.Count; i++)
            {
                var batchCases = new List<CaseChangeSetup>();
                foreach (var scaledEmployee in batches[i])
                {
                    var originalCases = employeeTemplates
                        .First(t => string.Equals(t.OriginalIdentifier, scaledEmployee.TemplateId))
                        .Cases;

                    foreach (var templateCase in originalCases)
                    {
                        var cloned = Clone(templateCase);
                        cloned.EmployeeIdentifier = scaledEmployee.Employee.Identifier;

                        // offset Created on each case value to avoid unique index
                        // conflicts when template has multiple changes for same
                        // field/start combination
                        if (cloned.Case?.Values != null)
                        {
                            foreach (var value in cloned.Case.Values)
                            {
                                value.Created = value.Created.AddTicks(tickOffset++);
                            }
                        }

                        batchCases.Add(cloned);
                    }
                }

                var casesExchange = new Exchange
                {
                    Tenants =
                    [
                        new ExchangeTenant
                        {
                            Identifier = templateTenant.Identifier,
                            Payrolls =
                            [
                                new PayrollSet
                                {
                                    Name = templatePayroll.Name,
                                    DivisionName = templatePayroll.DivisionName
                                                    ?? batchCases.FirstOrDefault()?.DivisionName,
                                    Cases = batchCases
                                }
                            ]
                        }
                    ]
                };
                var casesFile = Path.Combine(outputDir, $"Setup-Cases-{i + 1:D3}.json");
                await FileWriter.WriteAsync(casesExchange, casesFile);
                console.DisplayTextLine($"  {Path.GetFileName(casesFile)} ({batchCases.Count} case changes)");
            }

            // 6. write Payrun-Invocation.json (from template invocations)
            var invocations = ExtractInvocations(templateTenant, parameters.EmployeeCount);
            if (invocations.Count == 0)
            {
                console.DisplayErrorLine("Invalid template: no payrun job invocations found.");
                return -1;
            }

            var invocationExchange = new Exchange
            {
                Tenants =
                [
                    new ExchangeTenant
                    {
                        Identifier = templateTenant.Identifier,
                        PayrunJobInvocations = invocations
                    }
                ]
            };
            var invocationFile = Path.Combine(outputDir, "Payrun-Invocation.json");
            await FileWriter.WriteAsync(invocationExchange, invocationFile);
            console.DisplayTextLine($"  {Path.GetFileName(invocationFile)} ({invocations.Count} invocations)");

            // summary
            console.DisplayNewLine();
            console.DisplaySuccessLine(
                $"Generated {parameters.EmployeeCount} employees in {batches.Count + 2} files → {outputDir}");

            return 0;
        }
        catch (Exception exception)
        {
            ProcessError(context.Console, exception);
            return -2;
        }
    }

    /// <summary>
    /// Extract employee templates from exchange data.
    /// Handles two cases:
    /// - Template has Employees section → use EmployeeSet objects
    /// - Template has NO Employees section → extract identifiers from case changes
    /// Division is resolved from existing employee or from case change DivisionName.
    /// </summary>
    private static List<EmployeeTemplate> ExtractEmployeeTemplates(
        ExchangeTenant tenant, List<CaseChangeSetup> cases)
    {
        var casesByEmployee = cases
            .Where(c => !string.IsNullOrWhiteSpace(c.EmployeeIdentifier))
            .GroupBy(c => c.EmployeeIdentifier)
            .ToDictionary(g => g.Key, g => g.ToList());

        if (casesByEmployee.Count == 0)
        {
            return [];
        }

        var templates = new List<EmployeeTemplate>();
        foreach (var (identifier, employeeCases) in casesByEmployee)
        {
            var existingEmployee = tenant.Employees?
                .FirstOrDefault(e => string.Equals(e.Identifier, identifier));

            var employee = existingEmployee ?? CreateEmployeeFromIdentifier(identifier);

            // ensure division is set (required for payrun processing)
            if (employee.Divisions == null || employee.Divisions.Count == 0)
            {
                var divisionName = employeeCases
                    .Select(c => c.DivisionName)
                    .FirstOrDefault(d => !string.IsNullOrWhiteSpace(d));
                if (!string.IsNullOrWhiteSpace(divisionName))
                {
                    employee.Divisions = [divisionName];
                }
            }

            templates.Add(new EmployeeTemplate
            {
                OriginalIdentifier = identifier,
                Employee = employee,
                Cases = employeeCases
            });
        }
        return templates;
    }

    /// <summary>
    /// Create an EmployeeSet from an identifier.
    /// Parses LastName and FirstName from the identifier parts.
    /// </summary>
    private static EmployeeSet CreateEmployeeFromIdentifier(string identifier)
    {
        var employee = new EmployeeSet { Identifier = identifier };

        var parts = identifier.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        switch (parts.Length)
        {
            case >= 3:
                employee.LastName = parts[1];
                employee.FirstName = string.Join(" ", parts[2..]);
                break;
            case 2:
                employee.LastName = parts[1];
                employee.FirstName = parts[1];
                break;
            default:
                employee.LastName = identifier;
                employee.FirstName = identifier;
                break;
        }
        return employee;
    }

    /// <summary>Generate N employees by multiplying templates</summary>
    private static List<ScaledEmployee> GenerateScaledEmployees(
        List<EmployeeTemplate> templates, int targetCount)
    {
        var result = new List<ScaledEmployee>(targetCount);
        var copyIndex = 0;

        while (result.Count < targetCount)
        {
            foreach (var template in templates)
            {
                if (result.Count >= targetCount)
                {
                    break;
                }
                copyIndex++;

                var cloned = Clone(template.Employee);
                var suffix = $"-C{copyIndex:D4}";
                cloned.Identifier = $"{template.OriginalIdentifier}{suffix}";

                if (string.IsNullOrWhiteSpace(cloned.FirstName) ||
                    string.IsNullOrWhiteSpace(cloned.LastName))
                {
                    var source = CreateEmployeeFromIdentifier(template.OriginalIdentifier);
                    cloned.FirstName = source.FirstName;
                    cloned.LastName = source.LastName;
                }

                result.Add(new ScaledEmployee
                {
                    Employee = cloned,
                    TemplateId = template.OriginalIdentifier
                });
            }
        }
        return result;
    }

    /// <summary>Deep clone via JSON serialization</summary>
    private static T Clone<T>(T source) =>
        JsonSerializer.Deserialize<T>(
            JsonSerializer.Serialize(source, JsonCloneOptions), JsonCloneOptions);

    /// <inheritdoc />
    public override ICommandParameters GetParameters(CommandLineParser parser) =>
        LoadTestGenerateParameters.ParserFrom(parser);

    /// <inheritdoc />
    public override void ShowHelp(ICommandConsole console)
    {
        console.DisplayTitleLine("- LoadTestGenerate");
        console.DisplayTextLine("      Generate scaled exchange files for payrun load testing");
        console.DisplayTextLine("      Arguments:");
        console.DisplayTextLine("          1. Path to exchange template file [TemplatePath]");
        console.DisplayTextLine("          2. Target employee count (100, 1000, 10000) [EmployeeCount]");
        console.DisplayTextLine("          3. Output directory (optional, default: LoadTest{count}) [OutputDir]");
        console.DisplayTextLine("      Payrun invocations are extracted from the template (deduplicated by period).");
        console.DisplayTextLine("      Examples:");
        console.DisplayTextLine("          LoadTestGenerate MyTemplate.et.json 100");
        console.DisplayTextLine("          LoadTestGenerate MyTemplate.et.json 1000 LoadTest1000");
        console.DisplayTextLine("          LoadTestGenerate MyTemplate.et.json 10000 LoadTest10000");
    }

    /// <summary>
    /// Extract unique invocations from template, deduplicated by PeriodStart.
    /// Removes EmployeeIdentifiers so all employees are included.
    /// </summary>
    private static List<PayrunJobInvocation> ExtractInvocations(
        ExchangeTenant tenant, int employeeCount)
    {
        var templateInvocations = tenant.PayrunJobInvocations;
        if (templateInvocations == null || templateInvocations.Count == 0)
        {
            return [];
        }

        // deduplicate by PeriodStart, keep first occurrence
        var seen = new HashSet<DateTime>();
        var result = new List<PayrunJobInvocation>();
        foreach (var invocation in templateInvocations.OrderBy(i => i.PeriodStart))
        {
            if (!seen.Add(invocation.PeriodStart))
            {
                continue;
            }

            result.Add(new PayrunJobInvocation
            {
                Name = $"LoadTest-{employeeCount}-{invocation.PeriodStart:yyyyMM}",
                PayrunName = invocation.PayrunName,
                UserIdentifier = invocation.UserIdentifier,
                JobStatus = invocation.JobStatus,
                PeriodStart = invocation.PeriodStart,
                EvaluationDate = invocation.EvaluationDate,
                Reason = $"Load test {employeeCount} employees",
                StoreEmptyResults = invocation.StoreEmptyResults,
                LogLevel = invocation.LogLevel
                // no EmployeeIdentifiers → all employees
            });
        }
        return result;
    }

    /// <summary>Employee template with associated cases</summary>
    private sealed record EmployeeTemplate
    {
        internal string OriginalIdentifier { get; init; }
        internal EmployeeSet Employee { get; init; }
        internal List<CaseChangeSetup> Cases { get; init; }
    }

    /// <summary>Scaled employee with reference to original template</summary>
    private sealed record ScaledEmployee
    {
        internal EmployeeSet Employee { get; init; }
        internal string TemplateId { get; init; }
    }
}
