using PayrollEngine.Client.Command;
using PayrollEngine.PayrollConsole.Commands.RegulationCommands;
using Xunit;

namespace PayrollEngine.PayrollConsole.Commands.Tests;

public class LexVerifySourceParametersTests
{
    private static LexVerifySourceParameters Parse(params string[] arguments) =>
        LexVerifySourceParameters.ParserFrom(new CommandLineParser(["pecmd", "LexVerifySource", .. arguments]));

    [Fact]
    public void SourceMode_DefaultsToLocal()
    {
        var parameters = Parse("lex.json");
        Assert.Equal(LexSourceMode.Local, parameters.SourceMode);
        Assert.Equal(LexSourceWriteMode.ReportOnly, parameters.WriteMode);
    }

    [Fact]
    public void LiveToggle_SetsLiveMode()
    {
        var parameters = Parse("lex.json", "/recursive", "/live", "/writelocations");
        Assert.Equal(LexSourceMode.Live, parameters.SourceMode);
        Assert.Equal(LexSourceWriteMode.WriteLocations, parameters.WriteMode);
        Assert.Equal(DirectoryMode.Recursive, parameters.DirectoryMode);
    }

    [Fact]
    public void RefreshLocationsToggle_SetsWriteMode()
    {
        var parameters = Parse("lex.json", "/recursive", "/refreshlocations");
        Assert.Equal(LexSourceWriteMode.RefreshLocations, parameters.WriteMode);
        Assert.Equal(LexSourceMode.Local, parameters.SourceMode);
    }

    [Fact]
    public void StrictToggle_SetsCheckMode() =>
        Assert.Equal(LexCheckMode.Strict, Parse("lex.json", "/strict").CheckMode);

    [Fact]
    public void Vocabulary_FolderByName()
    {
        Assert.Null(Parse("lex.json").Vocabulary);
        var parameters = Parse("lex.json", "/recursive", @"Vocabulary:C:\Lex\LexVocabulary");
        Assert.Equal(@"C:\Lex\LexVocabulary", parameters.Vocabulary);
        Assert.Equal("lex.json", parameters.FileMask);
    }

    [Fact]
    public void SourceMode_IsDeclaredToggle() =>
        Assert.Contains(typeof(LexSourceMode), Parse("lex.json").Toggles);
}
