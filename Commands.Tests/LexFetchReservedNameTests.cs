using PayrollEngine.Client.Command;
using PayrollEngine.PayrollConsole.Commands.RegulationCommands;
using Xunit;

namespace PayrollEngine.PayrollConsole.Commands.Tests;

/// <summary>LexFetch refuses the manifest and the snapshot as target and leaves the files untouched</summary>
[Collection("ConsoleOutput")]
public sealed class LexFetchReservedNameTests : IDisposable
{
    private const string Manifest = """
        {
          "authority": "XX", "regulation": "Data.Test", "validFrom": "2026-01-01", "provider": "Test Authority",
          "sources": [ { "url": "https://example.invalid/doc", "description": "Test source" } ],
          "values": []
        }
        """;

    private readonly string folder = Path.Combine(Path.GetTempPath(), "lexfetch-" + Guid.NewGuid().ToString("N"));

    private string LexFolder => Path.Combine(folder, ".lex");

    private string ManifestPath => Path.Combine(LexFolder, "lex.json");

    public LexFetchReservedNameTests()
    {
        Directory.CreateDirectory(LexFolder);
        File.WriteAllText(ManifestPath, Manifest);
    }

    [Theory]
    [InlineData("lex.json")]
    [InlineData("snapshot.json")]
    [InlineData("LEX.JSON")]
    [InlineData("lex.json.")]
    [InlineData("lex.json ")]
    public async Task Fetch_ReservedFileName_IsInvalidInput(string fileName)
    {
        var before = await File.ReadAllBytesAsync(ManifestPath);
        var parameters = LexFetchParameters.ParserFrom(new CommandLineParser(
            ["pecmd", "LexFetch", "https://example.invalid/rates.json", fileName, "Lex:" + LexFolder, "/overwrite"]));
        Assert.Equal(fileName, parameters.FileName);

        var output = new StringWriter();
        var original = Console.Out;
        int exitCode;
        try
        {
            Console.SetOut(output);
            var context = new CommandContext(commandManager: TestExtensions.NewCommandManager(),
                console: new CommandConsole());
            exitCode = await new LexFetchCommand().ExecuteAsync(context, parameters);
        }
        finally
        {
            Console.SetOut(original);
        }

        Assert.Equal((int)ProgramExitCode.InvalidInput, exitCode);
        Assert.Contains("Reserved file name", output.ToString());
        Assert.Equal(before, await File.ReadAllBytesAsync(ManifestPath));
        Assert.False(File.Exists(Path.Combine(LexFolder, "snapshot.json")));
    }

    public void Dispose()
    {
        try { Directory.Delete(folder, true); }
        catch (IOException) { /* best effort */ }
    }
}
