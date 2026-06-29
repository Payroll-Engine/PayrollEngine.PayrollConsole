using System.Collections.Generic;
using System.Text.Json.Serialization;
// ReSharper disable CommentTypo

namespace PayrollEngine.PayrollConsole.Commands.RegulationCommands.Lex;

/// <summary>
/// Deserialized lex.json manifest — source of truth for compliance value provenance
/// </summary>
public sealed class LexManifest
{
    /// <summary>
    /// JSON Schema URI ($schema field)
    /// </summary>
    [JsonPropertyName("$schema")]
    public string Schema { get; init; }

    /// <summary>
    /// Country/jurisdiction code (e.g. DE, CH, SG)
    /// </summary>
    public string Authority { get; init; }

    /// <summary>
    /// Regulation identifier (e.g. Data.LSt, Data.QST)
    /// </summary>
    public string Regulation { get; init; }

    /// <summary>
    /// Effective date in YYYY-MM-DD format
    /// </summary>
    public string ValidFrom { get; init; }

    /// <summary>
    /// Issuing authority (e.g. BMF, ESTV, HMRC)
    /// </summary>
    public string Provider { get; init; }

    /// <summary>
    /// Document-level source references
    /// </summary>
    public List<LexSource> Sources { get; init; } = [];

    /// <summary>
    /// Compliance values with content and provenance
    /// </summary>
    public List<LexValue> Values { get; init; } = [];

    /// <summary>
    /// Source documents to copy into the .lex folder
    /// </summary>
    public List<LexFile> Files { get; init; } = [];
}
