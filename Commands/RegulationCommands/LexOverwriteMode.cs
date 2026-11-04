namespace PayrollEngine.PayrollConsole.Commands.RegulationCommands;

/// <summary>
/// Lex command handling of existing results: snapshot.json (LexSnapshot), evidence files (LexFetch)
/// and manual verification records (LexAttest)
/// </summary>
public enum LexOverwriteMode
{
    /// <summary>
    /// Keep the existing result (default)
    /// </summary>
    Skip,

    /// <summary>
    /// Replace the existing result
    /// </summary>
    Overwrite
}
