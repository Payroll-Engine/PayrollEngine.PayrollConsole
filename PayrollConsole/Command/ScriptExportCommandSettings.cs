﻿using PayrollEngine.Client.Script;
// ReSharper disable PropertyCanBeMadeInitOnly.Global

namespace PayrollEngine.PayrollConsole.Command;

internal sealed class ScriptExportCommandSettings
{
    internal string TargetFolder { get; set; }
    internal string TenantIdentifier { get; set; }
    internal string UserIdentifier { get; set; }
    internal string EmployeeIdentifier { get; set; }
    internal string PayrollName { get; set; }
    internal string RegulationName { get; set; }
    internal ScriptExportMode ScriptExportMode { get; set; }
    internal ScriptExportObject ScriptObject { get; set; }
    internal string Namespace { get; set; }
}