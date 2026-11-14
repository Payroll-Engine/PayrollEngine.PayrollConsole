using PayrollEngine.Lex;
using PayrollEngine.PayrollConsole.Commands.RegulationCommands;
using Xunit;

namespace PayrollEngine.PayrollConsole.Commands.Tests;

/// <summary>LexCheck shows the warnings of the check result (checks that could not run)</summary>
public sealed class LexCheckWarningTests
{
    [Fact]
    public void WarningLines_WithoutWarnings_AreEmpty()
    {
        Assert.Empty(LexCheckCommand.WarningLines(new CheckResult()));
    }

    [Fact]
    public void WarningLines_ListEveryWarningWithPrefix()
    {
        var result = new CheckResult();
        result.Warnings.Add("git not available: evidence versioning not checked");
        result.Warnings.Add("not inside a git repository: C:/Repos/XX");

        var lines = LexCheckCommand.WarningLines(result).ToList();

        Assert.Equal(2, lines.Count);
        Assert.Equal("         warning: git not available: evidence versioning not checked", lines[0]);
        Assert.EndsWith("not inside a git repository: C:/Repos/XX", lines[1]);
    }
}
