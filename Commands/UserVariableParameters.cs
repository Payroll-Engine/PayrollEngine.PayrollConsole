using System;
using PayrollEngine.Client.Command;

namespace PayrollEngine.PayrollConsole.Commands;

public class UserVariableParameters : ICommandParameters
{
    public string VariableName { get; init; }
    public string VariableValue { get; set; }
    public UserVariableMode VariableMode { get; set; } = UserVariableMode.View;
    public Type[] Toggles =>
    [
        typeof(UserVariableMode)
    ];

    public string Test() =>
        string.IsNullOrWhiteSpace(VariableName) ? "Missing user variable name" : null;

    public static UserVariableParameters ParserFrom(CommandLineParser parser) =>
        new()
        {
            VariableName = parser.Get(2, nameof(VariableName)),
            VariableValue = parser.Get(3, nameof(VariableValue)),
            VariableMode = parser.GetEnumToggle(UserVariableMode.View)
        };
}