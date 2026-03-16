using System;
using System.Linq;
using PayrollEngine.Client.Command;

namespace PayrollEngine.PayrollConsole.Commands.LoadTestCommands;

/// <summary>Payrun load test parameters</summary>
public class PayrunLoadTestParameters : ICommandParameters
{
    /// <summary>Path to Payrun-Invocation exchange file</summary>
    public string InvocationFile { get; init; }

    /// <summary>Expected employee count (for report)</summary>
    public int EmployeeCount { get; init; }

    /// <summary>Number of repetitions (default: 3, median used)</summary>
    public int Repetitions { get; init; } = 3;

    /// <summary>Output CSV path for results</summary>
    public string ResultFile { get; init; }

    /// <summary>Output Excel path for results (optional, enables Excel report)</summary>
    public string ExcelFile { get; init; }

    /// <summary>Output Markdown path for results (optional, enables Markdown report)</summary>
    public string MarkdownFile { get; init; }

    /// <summary>Backend MaxParallelEmployees value for documentation in the Excel report</summary>
    public string ParallelSetting { get; init; }

    /// <summary>True if an Excel report should be written</summary>
    public bool ExcelReport => !string.IsNullOrWhiteSpace(ExcelFile);

    /// <summary>True if a Markdown report should be written</summary>
    public bool MarkdownReport => !string.IsNullOrWhiteSpace(MarkdownFile);

    /// <inheritdoc />
    public Type[] Toggles => null;

    private static string ResolveExcelFile(CommandLineParser parser)
    {
        // explicit path: /ExcelFile=path/to/report.xlsx
        var explicit_ = parser.GetByName(nameof(ExcelFile));
        if (!string.IsNullOrWhiteSpace(explicit_))
        {
            return explicit_;
        }

        // toggle only: /ExcelReport → derive from CSV path
        if (parser.GetToggles().Any(t => string.Equals(t.TrimStart('/', '-'), "ExcelReport", StringComparison.OrdinalIgnoreCase)))
        {
            var csv = parser.Get(5, nameof(ResultFile)) ?? "LoadTestResults.csv";
            return System.IO.Path.ChangeExtension(csv, ".xlsx");
        }
        return null;
    }

    private static string ResolveMarkdownFile(CommandLineParser parser)
    {
        // explicit path: /MarkdownFile=path/to/report.md
        var explicit_ = parser.GetByName(nameof(MarkdownFile));
        if (!string.IsNullOrWhiteSpace(explicit_))
        {
            return explicit_;
        }

        // toggle only: /MarkdownReport → derive from CSV path
        if (parser.GetToggles().Any(t => string.Equals(t.TrimStart('/', '-'), "MarkdownReport", StringComparison.OrdinalIgnoreCase)))
        {
            var csv = parser.Get(5, nameof(ResultFile)) ?? "LoadTestResults.csv";
            return System.IO.Path.ChangeExtension(csv, ".md");
        }
        return null;
    }

    /// <inheritdoc />
    public string Test()
    {
        if (string.IsNullOrWhiteSpace(InvocationFile))
        {
            return "Missing invocation file";
        }
        if (EmployeeCount <= 0)
        {
            return "Invalid employee count";
        }
        return null;
    }

    /// <summary>Parse command parameters</summary>
    public static PayrunLoadTestParameters ParserFrom(CommandLineParser parser) =>
        new()
        {
            InvocationFile = parser.Get(2, nameof(InvocationFile)),
            EmployeeCount = int.TryParse(parser.Get(3, nameof(EmployeeCount)), out var c) ? c : 0,
            Repetitions = int.TryParse(parser.Get(4, nameof(Repetitions)), out var r) && r > 0 ? r : 3,
            ResultFile = parser.Get(5, nameof(ResultFile)) ?? "LoadTestResults.csv",
            ExcelFile = ResolveExcelFile(parser),
            MarkdownFile = ResolveMarkdownFile(parser),
            ParallelSetting = parser.GetByName(nameof(ParallelSetting))
        };
}
