namespace PayrollEngine.PayrollConsole.Commands.ExtensionCommands;

/// <summary>
/// Extension file with its expected hash
/// </summary>
public sealed class ExtensionFile
{
    /// <summary>
    /// File path, relative to the extension folder
    /// </summary>
    public string Path { get; set; }

    /// <summary>
    /// SHA-256 hash of the file as hex string
    /// </summary>
    public string Sha256 { get; set; }
}
