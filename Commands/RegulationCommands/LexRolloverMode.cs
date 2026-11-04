namespace PayrollEngine.PayrollConsole.Commands.RegulationCommands;

/// <summary>
/// LexRollover execution mode
/// </summary>
public enum LexRolloverMode
{
    /// <summary>
    /// Create the satellite of the next cycle (default)
    /// </summary>
    Write,

    /// <summary>
    /// Show the planned satellite only — no files written
    /// </summary>
    DryRun
}
