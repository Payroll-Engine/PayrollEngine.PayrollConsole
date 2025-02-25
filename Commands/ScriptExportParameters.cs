using System;
using PayrollEngine.Client.Script;
using PayrollEngine.Client.Command;

namespace PayrollEngine.PayrollConsole.Commands;

/// <summary>
/// Script export command parameters
/// </summary>
public class ScriptExportParameters : ICommandParameters
{
    /// <summary>
    /// Target folder
    /// </summary>
    public string TargetFolder { get; init; }

    /// <summary>
    /// Tenant
    /// </summary>
    public string Tenant { get; init; }

    /// <summary>
    /// User
    /// </summary>
    public string User { get; init; }

    /// <summary>
    /// Employee
    /// </summary>
    public string Employee { get; init; }

    /// <summary>
    /// Payroll
    /// </summary>
    public string Payroll { get; init; }

    /// <summary>
    /// Regulation
    /// </summary>
    public string Regulation { get; init; }

    /// <summary>
    /// Namespace
    /// </summary>
    public string Namespace { get; init; }

    /// <summary>
    /// EExport mode (default: existing)
    /// </summary>
    public ScriptExportMode ExportMode { get; private init; } = ScriptExportMode.Existing;

    /// <summary>
    /// Script object (default : all)
    /// </summary>
    public ScriptExportObject ScriptObject { get; private init; } = ScriptExportObject.All;

    /// <inheritdoc />
    public Type[] Toggles =>
    [
        typeof(ScriptExportMode)
    ];

    /// <inheritdoc />
    public string Test()
    {
        if (string.IsNullOrWhiteSpace(TargetFolder))
        {
            return "Missing target folder";
        }
        if (string.IsNullOrWhiteSpace(Tenant))
        {
            return "Missing tenant";
        }
        if (string.IsNullOrWhiteSpace(User))
        {
            return "Missing user";
        }
        if (string.IsNullOrWhiteSpace(Employee))
        {
            return "Missing employee";
        }
        if (string.IsNullOrWhiteSpace(Payroll))
        {
            return "Missing payroll";
        }
        if (string.IsNullOrWhiteSpace(Regulation))
        {
            return "Missing regulation";
        }
        return null;
    }

    /// <summary>
    /// Parse command parameters
    /// </summary>
    /// <param name="parser">Parameter parser</param>
    public static ScriptExportParameters ParserFrom(CommandLineParser parser) =>
        new()
        {
            TargetFolder = parser.Get(2, nameof(TargetFolder)),
            Tenant = parser.Get(3, nameof(Tenant)),
            User = parser.Get(4, nameof(User)),
            Employee = parser.Get(5, nameof(Employee)),
            Payroll = parser.Get(6, nameof(Payroll)),
            Regulation = parser.Get(7, nameof(Regulation)),
            Namespace = parser.Get(8, nameof(Namespace)),
            ExportMode = parser.GetEnumToggle(ScriptExportMode.Existing),
            ScriptObject = parser.GetEnumToggle(ScriptExportObject.All)
        };
}