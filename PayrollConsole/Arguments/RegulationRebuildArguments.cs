﻿using System;
using PayrollEngine.Client;
using PayrollEngine.Client.Script;

namespace PayrollEngine.PayrollConsole.Arguments;

public static class RegulationRebuildArguments
{
    public static string Tenant =>
        ConsoleArguments.GetMember(typeof(RegulationRebuildArguments), 2);

    public static string RegulationName =>
        ConsoleArguments.GetMember(typeof(RegulationRebuildArguments), 3);

    private static string ObjectType =>
        ConsoleArguments.GetMember(typeof(RegulationRebuildArguments), 4);

    public static string ObjectKey =>
        ConsoleArguments.GetMember(typeof(RegulationRebuildArguments), 5);

    public static Type[] Toggles => null;

    public static RegulationScriptObject? ScriptObject
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

    public static string TestArguments()
    {
        if (string.IsNullOrWhiteSpace(Tenant))
        {
            return "Missing tenant";
        }
        if (!string.IsNullOrWhiteSpace(ObjectType) && ScriptObject == null)
        {
            return $"Unknown object type {ObjectType}";
        }
        return string.IsNullOrWhiteSpace(RegulationName) ? "Missing regulation name" : null;
    }
}