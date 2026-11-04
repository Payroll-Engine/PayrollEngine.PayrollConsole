using PayrollEngine.Client.Command;
using PayrollEngine.PayrollConsole.Commands.RegulationCommands;
using Xunit;

namespace PayrollEngine.PayrollConsole.Commands.Tests;

public class LexCheckParametersTests
{
    private static LexCheckParameters Parse(params string[] arguments) =>
        LexCheckParameters.ParserFrom(new CommandLineParser(["pecmd", "LexCheck", .. arguments]));

    [Fact]
    public void Defaults()
    {
        var parameters = Parse("lex.json");
        Assert.Equal("lex.json", parameters.FileMask);
        Assert.Null(parameters.Root);
        Assert.Null(parameters.RepoMask);
        Assert.Equal(LexCheckMode.Strict, parameters.CheckMode);
        Assert.Equal(LexVerificationMode.VerificationOptional, parameters.VerificationMode);
        Assert.Null(parameters.Test());
    }

    [Fact]
    public void VerificationRequiredToggle_SetsVerificationMode()
    {
        var parameters = Parse("lex.json", "/recursive", "/verificationrequired");
        Assert.Equal(LexVerificationMode.VerificationRequired, parameters.VerificationMode);
        Assert.Equal(LexLocationMode.Optional, parameters.LocationMode);
        Assert.Equal(DirectoryMode.Recursive, parameters.DirectoryMode);
    }

    [Fact]
    public void RootAndRepoMask_AreParsed()
    {
        var parameters = Parse("lex.json", "/recursive", @"Root:C:\Repos", "RepoMask:Payroll.*");
        Assert.Equal(@"C:\Repos", parameters.Root);
        Assert.Equal("Payroll.*", parameters.RepoMask);
        Assert.Null(parameters.Test());
    }

    [Fact]
    public void RepoMaskWithoutRoot_IsInvalid() =>
        Assert.NotNull(Parse("lex.json", "RepoMask:Payroll.*").Test());

    [Fact]
    public void RootWithAbsoluteFileMask_IsInvalid() =>
        Assert.NotNull(Parse(Path.Combine(Path.GetTempPath(), "lex.json"), @"Root:C:\Repos").Test());

    [Fact]
    public void VerificationMode_IsDeclaredToggle() =>
        Assert.Contains(typeof(LexVerificationMode), Parse("lex.json").Toggles);
}
