using System.Text.Json.Serialization;

namespace PayrollEngine.PayrollConsole.Commands.RegulationCommands;

/// <summary>
/// Lookup value type
/// </summary>
[JsonConverter(typeof(JsonStringEnumConverter))]
public enum LookupValueType
{
    /// <summary>
    /// Text value
    /// </summary>
    Text,

    /// <summary>
    /// Decimal value
    /// </summary>
    Decimal,

    /// <summary>
    /// Integer value
    /// </summary>
    Integer,

    /// <summary>
    /// Boolean value
    /// </summary>
    Boolean
}