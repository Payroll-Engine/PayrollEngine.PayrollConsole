namespace PayrollEngine.PayrollConsole.Commands.RegulationCommands;

/// <summary>
/// LexSnapshot handling of an existing snapshot.json
/// </summary>
public enum LexOverwriteMode
{
    /// <summary>
    /// Skip folders with an existing snapshot.json (default)
    /// </summary>
    Skip,

    /// <summary>
    /// Regenerate an existing snapshot.json
    /// </summary>
    Overwrite
}
