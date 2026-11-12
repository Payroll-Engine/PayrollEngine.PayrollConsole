using PayrollEngine.PayrollConsole.Commands.RegulationCommands;
using Xunit;

namespace PayrollEngine.PayrollConsole.Commands.Tests;

/// <summary>LexCheck /release: the vocabulary folder reaches the source verification</summary>
public sealed class LexCheckReleaseTests : IDisposable
{
    private const string Danish = """
        {
          "language": "da",
          "name": "Danish",
          "percentWords": ["procent"],
          "enumerationWords": ["og", "eller"],
          "rangeWords": ["til"],
          "validityBefore": ["med virkning fra"],
          "months": [["januar"], ["februar"], ["marts"], ["april"], ["maj"], ["juni"], ["juli"], ["august"],
            ["september"], ["oktober"], ["november"], ["december"]]
        }
        """;

    private readonly string folder = Path.Combine(Path.GetTempPath(), "lexcheck-" + Guid.NewGuid().ToString("N"));

    private string Vocabulary => Path.Combine(folder, "Vocabulary");

    private string WriteSatellite(string languages)
    {
        var satellite = Path.Combine(folder, "Data.Test.2026");
        var lex = Path.Combine(satellite, ".lex");
        Directory.CreateDirectory(Path.Combine(satellite, "Regulation"));
        Directory.CreateDirectory(lex);
        Directory.CreateDirectory(Vocabulary);
        File.WriteAllText(Path.Combine(Vocabulary, "da.json"), Danish);
        File.WriteAllText(Path.Combine(satellite, "Regulation", "Test.json"),
            """
            { "tenants": [ { "regulations": [ { "name": "Test.Regulation", "validFrom": "2026-01-01T00:00:00Z",
              "lookups": [ { "name": "Rates", "values": [ { "key": "A", "value": "7.5" } ] } ] } ] } ] }
            """);
        var path = Path.Combine(lex, "lex.json");
        File.WriteAllText(path,
            $$"""
            {
              "authority": "XX", "regulation": "Data.Test", "validFrom": "2026-01-01", "provider": "Test Authority",
              {{languages}}
              "sources": [ { "url": "https://example.org/doc", "description": "Test source" } ],
              "values": [ { "key": "Rates.A", "content": "7.5", "unit": "%", "sourceUrls": [ "https://example.org/doc" ],
                "sourceLocation": "p. 1", "legalReference": "Test Act s. 1" } ]
            }
            """);
        return path;
    }

    [Fact]
    public async Task Release_AdditionalLanguage_UsesVocabulary()
    {
        var path = WriteSatellite("\"languages\": [ \"da\" ],");

        // the language has no file without the folder: the same error as the strict gate
        await Assert.ThrowsAsync<ArgumentException>(() =>
            LexCheckCommand.CheckFile(path, false, false, true, null));
        await Assert.ThrowsAsync<ArgumentException>(() =>
            // ReSharper disable once RedundantArgumentDefaultValue
            LexCheckCommand.CheckFile(path, false, false, true, null, null));

        var result = await LexCheckCommand.CheckFile(path, false, false, true, null, Vocabulary);
        Assert.True(result.SourcesVerified);
        Assert.Null(result.Error);
    }

    [Fact]
    public async Task Release_PackageLanguages_UnchangedWithVocabulary()
    {
        var path = WriteSatellite(string.Empty);

        var plain = await LexCheckCommand.CheckFile(path, false, false, true, null);
        var withVocabulary = await LexCheckCommand.CheckFile(path, false, false, true, null, Vocabulary);

        Assert.True(plain.SourcesVerified);
        Assert.Equal(plain.NotVerifiable, withVocabulary.NotVerifiable);
        Assert.Equal(plain.Ambiguous, withVocabulary.Ambiguous);
        Assert.Equal(plain.HasFindings, withVocabulary.HasFindings);
    }

    [Fact]
    public async Task Development_DoesNotVerifySources()
    {
        var path = WriteSatellite("\"languages\": [ \"da\" ],");

        var result = await LexCheckCommand.CheckFile(path, false, false, false, null);

        Assert.False(result.SourcesVerified);
    }

    public void Dispose()
    {
        try { Directory.Delete(folder, true); }
        catch (IOException) { /* best effort */ }
    }
}
