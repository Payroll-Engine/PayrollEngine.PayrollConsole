using System.Text.RegularExpressions;

namespace PayrollEngine.PayrollConsole.Commands.RegulationCommands;

/// <summary>
/// Argument helpers of the lex commands
/// </summary>
internal static class LexArguments
{
    private static readonly Regex NamedArgument = new("^[A-Za-z]+:(?!//)");

    /// <summary>
    /// A positional argument is missing when a named argument (Name:value) takes its place
    /// </summary>
    internal static string Positional(string argument) =>
        argument != null && NamedArgument.IsMatch(argument) ? null : argument;
}
