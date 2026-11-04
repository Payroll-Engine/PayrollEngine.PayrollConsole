using PayrollEngine.Client.Command;
using PayrollEngine.PayrollConsole.Commands.RegulationCommands;
using Xunit;

namespace PayrollEngine.PayrollConsole.Commands.Tests;

public class LexApplyParametersTests
{
    private static LexApplyParameters Parse(params string[] arguments) =>
        LexApplyParameters.ParserFrom(new CommandLineParser(["pecmd", "LexApply", .. arguments]));

    [Fact]
    public void Defaults()
    {
        var parameters = Parse("lex.json");
        Assert.Equal("lex.json", parameters.FileMask);
        Assert.Equal(DirectoryMode.Top, parameters.DirectoryMode);
        Assert.Equal(LexApplyMode.Write, parameters.ApplyMode);
        Assert.Null(parameters.Test());
    }

    [Fact]
    public void DryRunAndRecursive()
    {
        var parameters = Parse("lex.json", "/recursive", "/dryrun");
        Assert.Equal(DirectoryMode.Recursive, parameters.DirectoryMode);
        Assert.Equal(LexApplyMode.DryRun, parameters.ApplyMode);
    }

    [Fact]
    public void ToggleIsNoFileMask() =>
        Assert.NotNull(Parse("/dryrun").Test());

    [Fact]
    public void Toggles_AreDeclared()
    {
        var toggles = Parse("lex.json").Toggles;
        Assert.Contains(typeof(DirectoryMode), toggles);
        Assert.Contains(typeof(LexApplyMode), toggles);
    }
}
