using System;
using PayrollEngine.Client.Command;

namespace PayrollEngine.PayrollConsole.Commands.LoadTestCommands;

/// <summary>Load test case bulk setup parameters</summary>
public class LoadTestSetupCasesParameters : ICommandParameters
{
    /// <summary>Directory containing Setup-Cases-*.json files, or a single file path</summary>
    public string CasesPath { get; init; }

    /// <summary>HTTP batch size for bulk API calls (default: 500)</summary>
    public int BatchSize { get; init; } = 500;

    /// <inheritdoc />
    public Type[] Toggles => null;

    /// <inheritdoc />
    public string Test()
    {
        if (string.IsNullOrWhiteSpace(CasesPath))
        {
            return "Missing cases path (directory or file)";
        }
        return null;
    }

    /// <summary>Parse command parameters</summary>
    public static LoadTestSetupCasesParameters ParserFrom(CommandLineParser parser) =>
        new()
        {
            CasesPath = parser.Get(2, nameof(CasesPath)),
            BatchSize = int.TryParse(parser.Get(3, nameof(BatchSize)), out var b) && b > 0 ? b : 500
        };
}
