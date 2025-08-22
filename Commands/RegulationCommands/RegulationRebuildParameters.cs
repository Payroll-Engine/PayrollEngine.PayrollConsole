using System;
using PayrollEngine.Client.Command;
using PayrollEngine.Client.Script;

namespace PayrollEngine.PayrollConsole.Commands.RegulationCommands;

/// <summary>
/// Regulation rebuild command parameters
/// </summary>
public class RegulationRebuildParameters : ICommandParameters
{
    /// <summary>
    /// Tenant
    /// </summary>
    public string Tenant { get; init; }

    /// <summary>
    /// Regulation
    /// </summary>
    public string Regulation { get; init; }

    /// <summary>
    /// Object type
    /// </summary>
    public string ObjectType { get; init; }

    /// <summary>
    /// Objectkey
    /// </summary>
    public string ObjectKey { get; init; }

    /// <inheritdoc />
    public Type[] Toggles => null;

    /// <inheritdoc />
    public string Test()
    {
        if (string.IsNullOrWhiteSpace(Tenant))
        {
            return "Missing tenant";
        }
        if (!string.IsNullOrWhiteSpace(ObjectType) && ScriptObject == null)
        {
            return $"Unknown object type {ObjectType}";
        }
        return string.IsNullOrWhiteSpace(Regulation) ? "Missing regulation name" : null;
    }

    /// <summary>
    /// Script object
    /// </summary>
    public RegulationScriptObject? ScriptObject
    {
        get
        {
            if (Enum.TryParse<RegulationScriptObject>(ObjectType, true, out var scriptObject))
            {
                return scriptObject;
            }
            return null;
        }
    }

    /// <summary>
    /// Parse command parameters
    /// </summary>
    /// <param name="parser">Parameter parser</param>
    public static RegulationRebuildParameters ParserFrom(CommandLineParser parser) =>
        new()
        {
            Tenant = parser.Get(2, nameof(Tenant)),
            Regulation = parser.Get(3, nameof(Regulation)),
            ObjectType = parser.Get(4, nameof(ObjectType)),
            ObjectKey = parser.Get(5, nameof(ObjectKey))
        };
}