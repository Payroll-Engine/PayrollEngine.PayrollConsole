using PayrollEngine.Client.Command;
using PayrollEngine.Lex;
using System;

namespace PayrollEngine.PayrollConsole.Commands.RegulationCommands;

/// <summary>
/// LexRollover command parameters
/// </summary>
public class LexRolloverParameters : ICommandParameters
{
    /// <summary>
    /// Satellite directory of the previous cycle (contains .lex/lex.json)
    /// </summary>
    public string Satellite { get; init; }

    /// <summary>
    /// Date within the new cycle (yyyy-MM-dd)
    /// </summary>
    public string Date { get; init; }

    /// <summary>
    /// Tenant identifier of the payroll calendar
    /// </summary>
    public string Tenant { get; init; }

    /// <summary>
    /// Calendar name (default: tenant calendar)
    /// </summary>
    public string Calendar { get; init; }

    /// <summary>
    /// Culture name (default: tenant culture)
    /// </summary>
    public string Culture { get; init; }

    /// <summary>
    /// Execution mode: write or dry run (default: Write)
    /// </summary>
    public LexRolloverMode RolloverMode { get; private init; } = LexRolloverMode.Write;

    /// <inheritdoc />
    public Type[] Toggles => [typeof(LexRolloverMode)];

    /// <inheritdoc />
    public string Test()
    {
        if (string.IsNullOrWhiteSpace(Satellite))
        {
            return "Missing satellite directory";
        }
        if (string.IsNullOrWhiteSpace(Date))
        {
            return "Missing Date within the new cycle (yyyy-MM-dd)";
        }
        if (!EntryRules.IsDate(Date))
        {
            return $"Invalid Date '{Date}', expected yyyy-MM-dd";
        }
        return string.IsNullOrWhiteSpace(Tenant) ? "Missing Tenant" : null;
    }

    /// <summary>
    /// Parse command parameters
    /// </summary>
    public static LexRolloverParameters ParserFrom(CommandLineParser parser) =>
        new()
        {
            Satellite = LexArguments.Positional(parser.Get(2, nameof(Satellite))),
            Date = parser.GetByName(nameof(Date)),
            Tenant = parser.GetByName(nameof(Tenant)),
            Calendar = parser.GetByName(nameof(Calendar)),
            Culture = parser.GetByName(nameof(Culture)),
            RolloverMode = parser.GetEnumToggle(LexRolloverMode.Write)
        };
}
