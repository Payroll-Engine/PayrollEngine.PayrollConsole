using System;
using PayrollEngine.Client.Command;

namespace PayrollEngine.PayrollConsole.Commands;

public class PayrollResultsParameters : ICommandParameters
{
    public string Tenant { get; init; }
    public int TopFilter { get; init; }
    public ReportExportMode ResultExportMode { get; private init; } = ReportExportMode.NoExport;
    public Type[] Toggles =>
    [
        typeof(ReportExportMode)
    ];

    public string Test() =>
        string.IsNullOrWhiteSpace(Tenant) ? "Missing tenant" : null;

    public static PayrollResultsParameters ParserFrom(CommandLineParser parser) =>
        new()
        {
            Tenant = parser.Get(2, nameof(Tenant)),
            TopFilter = parser.GetInt(3, 1, nameof(TopFilter)),
            ResultExportMode = parser.GetEnumToggle(ReportExportMode.NoExport)
        };
}