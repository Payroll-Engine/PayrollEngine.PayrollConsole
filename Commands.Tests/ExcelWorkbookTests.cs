using ClosedXML.Excel;
using PayrollEngine.PayrollConsole.Commands.Excel;
using Xunit;
using static PayrollEngine.PayrollConsole.Commands.Tests.ExcelTestWorkbook;

namespace PayrollEngine.PayrollConsole.Commands.Tests;

/// <summary>Cell, row, sheet and defined name access of the Excel import</summary>
public class ExcelWorkbookTests
{
    private static ExcelWorkbook Open(XLWorkbook workbook) =>
        ExcelWorkbook.Open(SaveStream(workbook));

    private static ExcelCell ReadCell(Action<IXLCell> setup)
    {
        using var source = new XLWorkbook();
        var sheet = source.Worksheets.Add("Data");
        sheet.Cell("A1").Value = "Header";
        setup(sheet.Cell("A2"));
        var workbook = Open(source);
        return workbook.GetSheet("Data").GetRow(1).GetCell(0);
    }

    [Fact]
    public void Cell_Blank()
    {
        var cell = ReadCell(_ => { });
        Assert.Equal(ExcelCellType.Blank, cell.CellType);
        Assert.Null(cell.GetCellValue());
        Assert.Null(cell.GetCellValue<string>());
        Assert.Null(cell.GetCellValue<DateTime?>());
        Assert.Equal(0, cell.GetCellValue<int>());
    }

    [Fact]
    public void Cell_Text()
    {
        var cell = ReadCell(x => x.Value = "Alpha");
        Assert.Equal(ExcelCellType.String, cell.CellType);
        Assert.Equal("Alpha", cell.GetCellValue());
        Assert.Equal("Alpha", cell.GetCellValue<string>());
        Assert.Equal(ValueType.String, cell.GetValueType());
    }

    [Fact]
    public void Cell_Number()
    {
        var cell = ReadCell(x => x.Value = 50000);
        Assert.Equal(ExcelCellType.Numeric, cell.CellType);
        Assert.Equal(50000d, cell.GetCellValue());
        Assert.Equal(ValueType.Decimal, cell.GetValueType());
        Assert.Equal(50000, cell.GetCellValue<int>());
        Assert.Equal(50000, cell.GetCellValue<int?>());
        Assert.Equal(50000m, cell.GetCellValue<decimal>());
        Assert.Equal(50000m, cell.GetCellValue<decimal?>());
        Assert.Equal(50000d, cell.GetCellValue<double?>());
    }

    [Fact]
    public void Cell_Date()
    {
        var date = new DateTime(2026, 3, 31);
        var cell = ReadCell(x => SetValue(x, date));
        Assert.Equal(ExcelCellType.Numeric, cell.CellType);
        Assert.Equal(date, cell.GetCellValue());
        Assert.Equal(ValueType.DateTime, cell.GetValueType());
        Assert.Equal(date, cell.GetCellValue<DateTime>());
        Assert.Equal(date, cell.GetCellValue<DateTime?>());
    }

    [Fact]
    public void Cell_DateFromNumberAndText()
    {
        var date = new DateTime(2026, 3, 31);
        Assert.Equal(date, ReadCell(x => x.Value = date.ToOADate()).GetCellValue<DateTime>());
        Assert.Equal(date, ReadCell(x => x.Value = "2026-03-31").GetCellValue<DateTime?>());
    }

    [Fact]
    public void Cell_Boolean()
    {
        var cell = ReadCell(x => x.Value = true);
        Assert.Equal(true, cell.GetCellValue());
        Assert.True(cell.GetCellValue<bool>());
        Assert.True(cell.GetCellValue<bool?>());
        Assert.Equal(ValueType.Boolean, cell.GetValueType());
    }

    [Fact]
    public void Cell_Formula()
    {
        var cell = ReadCell(x => x.FormulaA1 = "1+1");
        Assert.Equal(ExcelCellType.Formula, cell.CellType);
        Assert.Equal("1+1", cell.GetCellValue());
        Assert.Throws<PayrollException>(() => cell.GetCellValue<int>());
    }

    [Fact]
    public void Cell_Error()
    {
        var cell = ReadCell(x => x.Value = XLError.DivisionByZero);
        Assert.Equal(ExcelCellType.Error, cell.CellType);
        Assert.Equal((byte)0x07, cell.GetCellValue());
    }

    [Fact]
    public void Cell_InvalidType_Throws()
    {
        var cell = ReadCell(x => x.Value = 12);
        var exception = Assert.Throws<PayrollException>(() => cell.GetCellValue<string>());
        Assert.Equal("Invalid cell value type Numeric on cell A2, expected String.", exception.Message);
    }

    [Fact]
    public void Sheet_RowCountAndBlankRows()
    {
        using var source = new XLWorkbook();
        AddSheet(source, "Data", ["Name"], ["A"], null, ["C"]);
        using var workbook = Open(source);
        var sheet = workbook.GetSheet("Data");

        Assert.Equal(4, sheet.RowCount);
        Assert.False(sheet.GetRow(1).IsBlank());
        Assert.True(sheet.GetRow(2).IsBlank());
        Assert.False(sheet.GetRow(3).IsBlank());
        Assert.True(sheet.GetRow(10).IsBlank());
    }

    [Fact]
    public void Sheet_HeaderColumns()
    {
        using var source = new XLWorkbook();
        var data = AddSheet(source, "Data", ["Name", "", " Name.de ", "Name.fr", "Value"]);
        data.Cell(1, 2).Clear();
        using var workbook = Open(source);
        var sheet = workbook.GetSheet("Data");

        Assert.Equal(0, sheet.GetHeaderColumn("name"));
        Assert.Equal(4, sheet.GetHeaderColumn("Value"));
        Assert.Null(sheet.GetHeaderColumn("Missing"));
        Assert.Equal(new Dictionary<int, string> { [2] = "de", [3] = "fr" }, sheet.GetHeaderMultiColumns("Name"));
        Assert.Null(sheet.HeaderCells()[1]);
        Assert.Equal(new Dictionary<string, int> { ["Name"] = 0, ["Value"] = 4 },
            sheet.GetColumnIndexes(["Name", "Value"]));
        Assert.Throws<PayrollException>(() => sheet.GetColumnIndexes(["Name", "Missing"]));
    }

    [Fact]
    public void Workbook_SheetsAndMask()
    {
        using var source = new XLWorkbook();
        source.Worksheets.Add("Lookup");
        source.Worksheets.Add("Lookup.A");
        source.Worksheets.Add("Lookup.B");
        using var workbook = Open(source);

        Assert.True(workbook.HasSheet("Lookup"));
        Assert.False(workbook.HasSheet("lookup"));
        Assert.Null(workbook.GetSheet("Missing"));
        Assert.Equal(["Lookup.A", "Lookup.B"], workbook.GetSheetsOf("Lookup.").Select(x => x.SheetName));
    }

    [Fact]
    public void Workbook_DefinedNames()
    {
        using var source = new XLWorkbook();
        var sheet = source.Worksheets.Add("My Sheet");
        sheet.Cell("B2").Value = "Value";
        sheet.Cell("B3").Value = "Local";
        source.DefinedNames.Add("Single", sheet.Range("B2"));
        source.DefinedNames.Add("Multiple", sheet.Range("B2:B3"));
        sheet.DefinedNames.Add("SheetScope", sheet.Range("B3"));
        using var workbook = Open(source);

        Assert.Equal("Value", workbook.GetNamedValue<string>("Single"));
        Assert.Equal("Local", workbook.GetNamedValue<string>("SheetScope"));
        Assert.Null(workbook.GetNamedValue<string>("Missing"));
        // the delegate runs synchronously inside Assert.Throws, before the workbook is disposed
        // ReSharper disable once AccessToDisposedClosure
        Assert.Throws<PayrollException>(() => workbook.GetNamedValue<string>("Multiple"));
    }

    [Fact]
    public void Workbook_Namespace()
    {
        using var source = new XLWorkbook();
        var sheet = source.Worksheets.Add("Exchange");
        sheet.Cell("A1").Value = "Test";
        source.DefinedNames.Add(RegionNames.NamespaceRegionName, sheet.Range("A1"));
        using var workbook = Open(source);

        Assert.Equal("Test.Salary", workbook.EnsureNamespace("Salary"));
        Assert.Equal("Test.Salary", workbook.EnsureNamespace("Test.Salary"));
    }

    [Theory]
    [InlineData("Regulation.xlsx", "Exchange")]
    [InlineData("CaseChange.xlsx", "Start")]
    public void Templates_Open(string template, string firstSheet)
    {
        var fileName = Path.Combine(FindRepositoryRoot(), "PayrollConsole", "Templates", template);
        using var stream = File.OpenRead(fileName);
        using var workbook = ExcelWorkbook.Open(stream);

        Assert.Equal(firstSheet, workbook.Sheets[0].SheetName);
        // defined names with sheet references, structured references and #REF! must be readable
        _ = workbook.GetNamedValue<string>(RegionNames.TenantRegionName);
        _ = workbook.GetNamedValue<string>(RegionNames.NamespaceRegionName);
        _ = workbook.GetNamedValue<string>("Lookups");
    }

    private static string FindRepositoryRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory != null && !Directory.Exists(Path.Combine(directory.FullName, "PayrollConsole", "Templates")))
        {
            directory = directory.Parent;
        }
        return directory?.FullName ?? throw new DirectoryNotFoundException("Repository root not found.");
    }
}
