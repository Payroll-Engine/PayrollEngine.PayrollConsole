namespace PayrollEngine.PayrollConsole.Commands;

/// <summary>
/// Import mode
/// </summary>
public enum ImportMode
{
    /// <summary>
    /// Import to file
    /// </summary>
    File,

    /// <summary>
    /// Import to backend
    /// </summary>
    Backend,

    /// <summary>
    /// Import to file and backend
    /// </summary>
    All
}