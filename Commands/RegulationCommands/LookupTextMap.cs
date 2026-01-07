using System.Collections.Generic;

namespace PayrollEngine.PayrollConsole.Commands.RegulationCommands;

/// <summary>
/// Mapping from text line to lookup value
/// </summary>
public class LookupTextMap
{
    /// <summary>
    /// Key mapping
    /// </summary>
    public LookupTextValueMap Key { get; set; }

    /// <summary>
    /// Keys mapping
    /// </summary>
    public List<LookupTextValueMap> Keys { get; set; }

    /// <summary>
    /// Range value mapping
    /// </summary>
    public LookupTextValueMap RangeValue { get; set; }

    /// <summary>
    /// Value mapping
    /// </summary>
    public LookupTextValueMap Value { get; set; }

    /// <summary>
    /// Values mapping
    /// </summary>
    public List<LookupTextValueMap> Values { get; set; }
}