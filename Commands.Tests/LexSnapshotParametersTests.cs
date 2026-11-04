using PayrollEngine.Client.Command;
using PayrollEngine.PayrollConsole.Commands.RegulationCommands;
using Xunit;

namespace PayrollEngine.PayrollConsole.Commands.Tests;

public class LexSnapshotParametersTests
{
    private static LexSnapshotParameters Parse(params string[] arguments) =>
        LexSnapshotParameters.ParserFrom(new CommandLineParser(["pecmd", "LexSnapshot", .. arguments]));

    [Fact]
    public void OverwriteMode_DefaultsToSkip()
    {
        var parameters = Parse("lex.json");
        Assert.Equal("lex.json", parameters.FileMask);
        Assert.Equal(LexOverwriteMode.Skip, parameters.OverwriteMode);
    }

    [Fact]
    public void OverwriteToggle_SetsOverwriteMode()
    {
        var parameters = Parse("lex.json", "/recursive", "/overwrite");
        Assert.Equal(LexOverwriteMode.Overwrite, parameters.OverwriteMode);
        Assert.Equal(DirectoryMode.Recursive, parameters.DirectoryMode);
        Assert.Equal(LexSnapshotMode.Execute, parameters.SnapshotMode);
    }

    [Fact]
    public void OverwriteMode_IsDeclaredToggle()
    {
        Assert.Contains(typeof(LexOverwriteMode), Parse("lex.json").Toggles);
    }
}
