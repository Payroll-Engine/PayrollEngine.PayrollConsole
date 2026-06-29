namespace PayrollEngine.PayrollConsole.Commands.RegulationCommands.Lex;

/// <summary>
/// Document-level source reference in lex.json
/// </summary>
public sealed class LexSource
{
    /// <summary>
    /// URL to the source document or page
    /// </summary>
    public string Url { get; init; }

    /// <summary>
    /// Human-readable description
    /// </summary>
    public string Description { get; init; }

    /// <summary>
    /// Legal basis (e.g. §39b EStG)
    /// </summary>
    public string LegalBasis { get; init; }
}
