namespace PayrollEngine.PayrollConsole.Commands.RegulationCommands;

/// <summary>
/// Regulation package dry-run mode
/// </summary>
public enum RegulationDryRun
{
    /// <summary>
    /// Execute the installation (default)
    /// </summary>
    Execute,

    /// <summary>
    /// Validate and report what would be imported without making any changes
    /// </summary>
    DryRun
}
