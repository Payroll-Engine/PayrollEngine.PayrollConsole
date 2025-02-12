using System;
using PayrollEngine.Client.Command;
using PayrollEngine.Client.Script;

namespace PayrollEngine.PayrollConsole.Commands;

public class RegulationRebuildParameters : ICommandParameters
{
    public string Tenant{ get; init; }
    public string Regulation { get; init; }
    public string ObjectType { get; init; }
    public string ObjectKey { get; init; }
    public Type[] Toggles => null;

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
    
    public static RegulationRebuildParameters ParserFrom(CommandLineParser parser) =>
        new()
        {
            Tenant = parser.Get(2, nameof(Tenant)),
            Regulation = parser.Get(3, nameof(Regulation)),
            ObjectType = parser.Get(4, nameof(ObjectType)),
            ObjectKey = parser.Get(5, nameof(ObjectKey))
        };
}