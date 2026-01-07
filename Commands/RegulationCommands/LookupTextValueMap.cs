
namespace PayrollEngine.PayrollConsole.Commands.RegulationCommands;

/// <summary>
/// Mapping from text line part to lookup key/value part
/// </summary>
public class LookupTextValueMap
{
    /// <summary>
    /// Value name
    /// </summary>
    public string Name { get; set; }

    /// <summary>
    /// Value type (default: text)
    /// </summary>
    public LookupValueType ValueType { get; set; }

    /// <summary>
    /// Value start index in text line (0..n)
    /// </summary>
    public int Start { get; set; }

    /// <summary>
    /// Value length (1..)
    /// </summary>
    public int Length { get; set; }

    /// <summary>
    /// Decimal places for decimal values (default: 0)
    /// </summary>
    public int DecimalPlaces { get; set; }

    /// <inheritdoc />
    public override string ToString() => $"{Name} ({ValueType}) [{Start}-{Length}]";
}