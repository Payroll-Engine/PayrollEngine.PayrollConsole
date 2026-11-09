using PayrollEngine.Client;
using PayrollEngine.Client.Command;
using PayrollEngine.Lex;

namespace PayrollEngine.PayrollConsole.Commands.RegulationCommands;

internal sealed class LookupTextImportContext
{
    internal PayrollHttpClient HttpClient { get; init; }
    internal ILogger Logger { get; init; }
    internal ICommandConsole Console { get; init; }
    internal TextTableMap Mapping { get; init; }
    internal string TargetFolder { get; init; }
}
