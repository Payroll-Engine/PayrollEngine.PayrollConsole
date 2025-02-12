using System;
using PayrollEngine.Client.Command;
using PayrollEngine.Client.Exchange;

namespace PayrollEngine.PayrollConsole.Commands;

public class PayrollImportExcelParameters : ICommandParameters
{
    public string FileName { get; init; }
    public string Tenant { get; init; }
    public DataImportMode ImportMode { get; private init; } = DataImportMode.Single;
    public Type[] Toggles =>
    [
        typeof(DataImportMode)
    ];

    public string Test() =>
        string.IsNullOrWhiteSpace(FileName) ? "Missing tenant" : null;

    public static PayrollImportExcelParameters ParserFrom(CommandLineParser parser) =>
        new()
        {
            FileName = parser.Get(2, nameof(FileName)),
            Tenant = parser.Get(3, nameof(Tenant)),
            ImportMode = parser.GetEnumToggle(DataImportMode.Single)
        };
}