using System;
using PayrollEngine.Client.Command;
using PayrollEngine.Client.Exchange;

namespace PayrollEngine.PayrollConsole.Commands;

public class PayrollImportParameters : ICommandParameters
{
    public string SourceFileName { get; init; }
    public string OptionsFileName { get; init; }
    public string Namespace { get; init; }
    public DataImportMode ImportMode { get; private init; } = DataImportMode.Single;

    public Type[] Toggles =>
    [
        typeof(DataImportMode)
    ];

    public string Test() =>
        string.IsNullOrWhiteSpace(SourceFileName) ? "Missing source file name or file mask" : null;

    public static PayrollImportParameters ParserFrom(CommandLineParser parser) =>
        new()
        {
            SourceFileName = parser.Get(2, nameof(SourceFileName)),
            OptionsFileName = parser.Get(3, nameof(OptionsFileName)),
            Namespace = parser.Get(4, nameof(Namespace)),
            ImportMode = parser.GetEnumToggle(DataImportMode.Single)
        };
}