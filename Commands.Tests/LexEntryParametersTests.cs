using PayrollEngine.Client.Command;
using PayrollEngine.PayrollConsole.Commands.RegulationCommands;
using Xunit;

namespace PayrollEngine.PayrollConsole.Commands.Tests;

public class LexEntryParametersTests
{
    private static CommandLineParser Parser(string command, params string[] arguments) =>
        new(["pecmd", command, .. arguments]);

    private static LexFetchParameters Fetch(params string[] arguments) =>
        LexFetchParameters.ParserFrom(Parser("LexFetch", arguments));

    private static LexAddParameters Add(params string[] arguments) =>
        LexAddParameters.ParserFrom(Parser("LexAdd", arguments));

    private static LexAttestParameters Attest(params string[] arguments) =>
        LexAttestParameters.ParserFrom(Parser("LexAttest", arguments));

    [Fact]
    public void Fetch_Defaults()
    {
        var parameters = Fetch("https://example.org/rates.pdf");
        Assert.Equal("https://example.org/rates.pdf", parameters.Url);
        Assert.Null(parameters.FileName);
        Assert.Null(parameters.Lex);
        Assert.Equal(LexOverwriteMode.Skip, parameters.OverwriteMode);
        Assert.Null(parameters.Test());
    }

    [Fact]
    public void Fetch_NamedArgumentsAndToggle_DoNotFillFileName()
    {
        var parameters = Fetch("https://example.org/rates.pdf", @"Lex:C:\Repo\.lex", "/overwrite");
        Assert.Null(parameters.FileName);
        Assert.Equal(@"C:\Repo\.lex", parameters.Lex);
        Assert.Equal(LexOverwriteMode.Overwrite, parameters.OverwriteMode);
    }

    [Fact]
    public void Fetch_FileNameAndDescription()
    {
        var parameters = Fetch("https://example.org/rates", "rates-2026.html", "Description:Tax rates 2026");
        Assert.Equal("rates-2026.html", parameters.FileName);
        Assert.Equal("Tax rates 2026", parameters.Description);
    }

    [Theory]
    [InlineData("ftp://example.org/rates.pdf")]
    [InlineData("rates.pdf")]
    [InlineData(@"Lex:C:\Repo")]
    public void Fetch_InvalidUrl(string url) =>
        Assert.NotNull(Fetch(url).Test());

    [Fact]
    public void Add_ParsesNamedFields()
    {
        var parameters = Add("Rates.Standard", "Content:8.5", "Unit:rate", "SourceUrl:https://example.org/doc",
            "SourceLocation:Table 1", "LegalReference:Art. 5", "DocumentRef:doc.pdf", "Description:Standard rate");
        Assert.Equal("Rates.Standard", parameters.Key);
        Assert.Equal("8.5", parameters.Content);
        Assert.Equal("rate", parameters.Unit);
        Assert.Equal("https://example.org/doc", parameters.SourceUrl);
        Assert.Equal("Table 1", parameters.SourceLocation);
        Assert.Equal("Art. 5", parameters.LegalReference);
        Assert.Equal("doc.pdf", parameters.DocumentRef);
        Assert.Equal("Standard rate", parameters.Description);
        Assert.Null(parameters.Test());
    }

    [Fact]
    public void Add_MissingKey_IsInvalid() =>
        Assert.NotNull(Add("Content:8.5").Test());

    [Theory]
    [InlineData("Rates")]
    [InlineData("Rates.*")]
    public void Add_InvalidKey(string key) =>
        Assert.NotNull(Add(key).Test());

    [Fact]
    public void Add_InvalidJsonContent() =>
        Assert.NotNull(Add("Rates.Table", "Content:{ \"rate\": ").Test());

    [Fact]
    public void Add_HasNoToggles() =>
        Assert.Null(Add("Rates.Standard").Toggles);

    [Fact]
    public void Attest_Verification()
    {
        var parameters = Attest("Rates", "VerifiedBy:Jane Doe", "Evidence:doc.pdf", "VerifiedOn:2026-11-03");
        Assert.Equal("Rates", parameters.Key);
        Assert.False(parameters.IsReview);
        Assert.Null(parameters.Test());
    }

    [Fact]
    public void Attest_Review()
    {
        var parameters = Attest("Rates*", "ReviewedBy:John Doe", "/overwrite");
        Assert.True(parameters.IsReview);
        Assert.Equal(LexOverwriteMode.Overwrite, parameters.OverwriteMode);
        Assert.Null(parameters.Test());
    }

    [Theory]
    [InlineData("VerifiedBy:Jane Doe", "Evidence:doc.pdf", "ReviewedBy:John Doe")]
    [InlineData("VerifiedBy:Jane Doe")]
    [InlineData("Evidence:doc.pdf")]
    [InlineData("VerifiedBy:Jane Doe", "Evidence:doc.pdf", "VerifiedOn:03.11.2026")]
    [InlineData("ReviewedBy:John Doe", "ReviewedOn:2026-13-01")]
    [InlineData("VerifiedBy:Jane Doe", "Evidence:doc.pdf", "ReviewedOn:2026-11-03")]
    [InlineData()]
    public void Attest_InvalidCombination(params string[] arguments) =>
        Assert.NotNull(Attest(["Rates", .. arguments]).Test());

    [Fact]
    public void Attest_MissingKey_IsInvalid() =>
        Assert.NotNull(Attest("VerifiedBy:Jane Doe", "Evidence:doc.pdf").Test());
}
