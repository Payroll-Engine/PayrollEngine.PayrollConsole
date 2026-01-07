using PayrollEngine.Client;
using PayrollEngine.Client.Command;

namespace PayrollEngine.PayrollConsole.Commands.RegulationCommands;

internal sealed class LookupTextImportContext
{
    internal PayrollHttpClient HttpClient { get; init; }
    internal ILogger Logger { get; init; }
    internal ICommandConsole Console { get; init; }
    internal LookupTextMap Mapping { get; init; }
    internal string TargetFolder { get; init; }
}