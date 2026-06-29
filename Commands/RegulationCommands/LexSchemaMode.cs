namespace PayrollEngine.PayrollConsole.Commands.RegulationCommands;

/// <summary>
/// LexSnapshot schema output mode
/// </summary>
public enum LexSchemaMode
{
    /// <summary>
    /// Normal execution (default)
    /// </summary>
    NoSchema,

    /// <summary>
    /// Print the JSON Schema for lex.json and exit
    /// </summary>
    Schema
}
