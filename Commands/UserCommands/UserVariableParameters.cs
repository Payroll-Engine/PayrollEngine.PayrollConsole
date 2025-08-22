using System;
using PayrollEngine.Client.Command;

namespace PayrollEngine.PayrollConsole.Commands.UserCommands;

/// <summary>
/// User variable command parameters
/// </summary>
public class UserVariableParameters : ICommandParameters
{
    /// <summary>
    /// Variable name
    /// </summary>
    public string VariableName { get; init; }

    /// <summary>
    /// Variable value
    /// </summary>
    public string VariableValue { get; set; }

    /// <summary>
    /// Variable mode (default: view)
    /// </summary>
    public UserVariableMode VariableMode { get; set; } = UserVariableMode.View;

    /// <inheritdoc />
    public Type[] Toggles =>
    [
        typeof(UserVariableMode)
    ];

    /// <inheritdoc />
    public string Test() =>
        string.IsNullOrWhiteSpace(VariableName) ? "Missing user variable name" : null;

    /// <summary>
    /// Parse command parameters
    /// </summary>
    /// <param name="parser">Parameter parser</param>
    public static UserVariableParameters ParserFrom(CommandLineParser parser) =>
        new()
        {
            VariableName = parser.Get(2, nameof(VariableName)),
            VariableValue = parser.Get(3, nameof(VariableValue)),
            VariableMode = parser.GetEnumToggle(UserVariableMode.View)
        };
}