using System;
using PayrollEngine.Client.Command;

namespace PayrollEngine.PayrollConsole.Commands.LoadTestCommands;

/// <summary>Load test exchange file generator parameters</summary>
public class LoadTestGenerateParameters : ICommandParameters
{
    /// <summary>Path to exchange template file</summary>
    public string TemplatePath { get; init; }

    /// <summary>Target employee count (100, 1000, 10000)</summary>
    public int EmployeeCount { get; init; }

    /// <summary>Output directory for generated files</summary>
    public string OutputDir { get; init; }

    /// <inheritdoc />
    public Type[] Toggles => [];

    /// <inheritdoc />
    public string Test()
    {
        if (string.IsNullOrWhiteSpace(TemplatePath))
            return "Missing template path";
        if (EmployeeCount <= 0)
            return "Invalid employee count";
        return null;
    }

    /// <summary>
    /// Parser from command line arguments
    /// </summary>
    /// <param name="parser">Command line parser</param>
    /// <returns>Command parameters</returns>
    public static LoadTestGenerateParameters ParserFrom(CommandLineParser parser) =>
        new()
        {
            TemplatePath = parser.Get(2, nameof(TemplatePath)),
            EmployeeCount = int.TryParse(parser.Get(3, nameof(EmployeeCount)), out var c) ? c : 0,
            OutputDir = parser.Get(4, nameof(OutputDir)) ?? $"LoadTest{parser.Get(3, nameof(EmployeeCount))}"
        };
}
