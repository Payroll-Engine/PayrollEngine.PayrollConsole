using ClosedXML.Excel;
using PayrollEngine.Client.Model;
using PayrollEngine.Client.Scripting;
using PayrollEngine.PayrollConsole.Commands.Excel;
using Xunit;
using static PayrollEngine.PayrollConsole.Commands.Tests.ExcelTestWorkbook;
using Task = System.Threading.Tasks.Task;

namespace PayrollEngine.PayrollConsole.Commands.Tests;

/// <summary>Regulation import from an xlsx file created in the test</summary>
public class ExcelRegulationImportTests
{
    private static readonly DateTime Created = new(2026, 1, 1);

    private static XLWorkbook CreateRegulationWorkbook()
    {
        var workbook = new XLWorkbook();

        // exchange with defined names
        var exchange = workbook.Worksheets.Add("Exchange");
        exchange.Cell("C4").Value = "Test.Tenant";
        exchange.Cell("C5").Value = "Test";
        SetValue(exchange.Cell("C7"), Created);
        workbook.DefinedNames.Add(RegionNames.TenantRegionName, exchange.Range("C4"));
        workbook.DefinedNames.Add(RegionNames.NamespaceRegionName, exchange.Range("C5"));
        workbook.DefinedNames.Add(RegionNames.CreatedObjectDateRegionName, exchange.Range("C7"));

        AddSheet(workbook, "Regulation",
            ["Name", "Name.de", "Version", "SharedRegulation", "ValidFrom", "Description", "Created", "Attributes"],
            ["Regulation", "Regelung", 2, true, Created, "Test regulation", Created, "\"Source\": \"Test\""]);

        AddSheet(workbook, "Case",
            ["Name", "CaseType", "Description", "Hidden", "Clusters"],
            ["Salary", "Employee", "Monthly salary", false, "A;B"]);

        AddSheet(workbook, "CaseField",
            ["CaseName", "Name", "ValueType", "ValueMandatory", "Order"],
            ["Salary", "Salary", "Money", true, 1],
            ["Salary", "Bonus", "Money", false, 2]);

        AddSheet(workbook, "Collector",
            ["Name", "Negated"],
            ["Income", false]);

        AddSheet(workbook, "WageType",
            ["WageTypeNumber", "Name", "ValueType", "Collectors"],
            [100, "Salary", "Money", "Income"],
            [200.5, "Bonus", "Money", null]);

        AddSheet(workbook, "Lookup",
            ["Name", "RangeSize"],
            ["Rates", null],
            ["Levels", 1000]);

        AddSheet(workbook, "LookupValue",
            ["LookupName", "Key", "Value", "RangeValue"],
            ["Rates", "A", "8.5", null],
            ["Rates", "B", "10", 2.5]);

        // per-lookup sheet: plain number above the old date threshold, date value, empty gap row
        AddSheet(workbook, "Lookup.Levels",
            ["Range", "Key", "Value"],
            [0, "Low", 50000],
            [60000, "High", new DateTime(2026, 7, 1)],
            null,
            [100000, "Top", 1]);

        return workbook;
    }

    private static async Task<Exchange> ImportAsync(XLWorkbook workbook)
    {
        var fileName = SaveTemp(workbook);
        try
        {
            return await ExchangeRegulationImport.ReadAsync(fileName);
        }
        finally
        {
            File.Delete(fileName);
        }
    }

    [Fact]
    public async Task Exchange_TenantAndDefinedNames()
    {
        using var workbook = CreateRegulationWorkbook();
        var exchange = await ImportAsync(workbook);

        var tenant = Assert.Single(exchange.Tenants);
        Assert.Equal("Test.Tenant", tenant.Identifier);
        Assert.Equal(Created.ToUtcTime(), exchange.CreatedObjectDate);
        Assert.Null(exchange.Schema);
    }

    [Fact]
    public async Task Regulation()
    {
        using var workbook = CreateRegulationWorkbook();
        var regulation = Assert.Single((await ImportAsync(workbook)).Tenants[0].Regulations);

        Assert.Equal("Test.Regulation", regulation.Name);
        Assert.Equal("Regelung", regulation.NameLocalizations["de"]);
        Assert.Equal(2, regulation.Version);
        Assert.True(regulation.SharedRegulation);
        Assert.Equal(Created, regulation.ValidFrom);
        Assert.Equal(Created, regulation.Created);
        Assert.Equal("Test regulation", regulation.Description);
        Assert.Equal("Test", regulation.Attributes["Source"]?.ToString());
    }

    [Fact]
    public async Task CasesAndCaseFields()
    {
        using var workbook = CreateRegulationWorkbook();
        var regulation = (await ImportAsync(workbook)).Tenants[0].Regulations[0];

        var @case = Assert.Single(regulation.Cases);
        Assert.Equal("Test.Salary", @case.Name);
        Assert.Equal(CaseType.Employee, @case.CaseType);
        Assert.False(@case.Hidden);
        Assert.Equal(["A", "B"], @case.Clusters);

        Assert.Equal(["Test.Salary", "Test.Bonus"], @case.Fields.Select(x => x.Name));
        Assert.All(@case.Fields, x => Assert.Equal(ValueType.Money, x.ValueType));
        Assert.True(@case.Fields[0].ValueMandatory);
        Assert.Equal(2, @case.Fields[1].Order);
    }

    [Fact]
    public async Task CollectorsAndWageTypes()
    {
        using var workbook = CreateRegulationWorkbook();
        var regulation = (await ImportAsync(workbook)).Tenants[0].Regulations[0];

        Assert.Equal("Test.Income", Assert.Single(regulation.Collectors).Name);
        Assert.Equal([100m, 200.5m], regulation.WageTypes.Select(x => x.WageTypeNumber));
        Assert.Equal(["Test.Income"], regulation.WageTypes[0].Collectors);
        Assert.Null(regulation.WageTypes[1].Collectors);
    }

    [Fact]
    public async Task LookupValues_DefaultSheet()
    {
        using var workbook = CreateRegulationWorkbook();
        var regulation = (await ImportAsync(workbook)).Tenants[0].Regulations[0];

        var rates = regulation.Lookups.Single(x => x.Name == "Test.Rates");
        Assert.Equal(["A", "B"], rates.Values.Select(x => x.Key));
        Assert.Equal(["8.5", "10"], rates.Values.Select(x => x.Value));
        Assert.Equal([null, 2.5m], rates.Values.Select(x => x.RangeValue));
        Assert.Equal(1000m, regulation.Lookups.Single(x => x.Name == "Test.Levels").RangeSize);
    }

    [Fact]
    public async Task LookupValues_LookupSheet()
    {
        using var workbook = CreateRegulationWorkbook();
        var regulation = (await ImportAsync(workbook)).Tenants[0].Regulations[0];

        var levels = regulation.Lookups.Single(x => x.Name == "Test.Levels");
        // the row after the empty gap row is read
        Assert.Equal(["Low", "High", "Top"], levels.Values.Select(x => x.Key));
        Assert.Equal([0m, 60000m, 100000m], levels.Values.Select(x => x.RangeValue));
        // numbers are numbers, dates require a date format
        Assert.Equal($"{50000d}", levels.Values[0].Value);
        Assert.Equal($"{new DateTime(2026, 7, 1)}", levels.Values[1].Value);
        Assert.Equal($"{1d}", levels.Values[2].Value);
    }

    [Fact]
    public async Task MissingTenant_Throws()
    {
        using var workbook = CreateRegulationWorkbook();
        workbook.DefinedNames.Delete(RegionNames.TenantRegionName);

        var exception = await Assert.ThrowsAsync<PayrollException>(() => ImportAsync(workbook));
        Assert.Equal("Missing tenant identifier.", exception.Message);
    }

    [Fact]
    public async Task RegulationSheet_OnlyOneRow()
    {
        using var workbook = CreateRegulationWorkbook();
        workbook.Worksheet("Regulation").Cell(3, 1).Value = "Second";

        var exception = await Assert.ThrowsAsync<PayrollException>(() => ImportAsync(workbook));
        Assert.Equal("Only one row allowed in sheet Regulation.", exception.Message);
    }
}
