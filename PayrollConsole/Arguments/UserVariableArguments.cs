using System;
using PayrollEngine.Client;
using PayrollEngine.PayrollConsole.Shared;

namespace PayrollEngine.PayrollConsole.Arguments;

public static class UserVariableArguments
{
    public static string VariableName =>
        ConsoleArguments.GetMember(typeof(UserVariableArguments), 2);

    public static string VariableValue =>
        ConsoleArguments.GetMember(typeof(UserVariableArguments), 3);

    public static UserVariableMode VariableMode(UserVariableMode defaultValue = UserVariableMode.View) =>
        ConsoleArguments.GetEnumToggle(defaultValue);

    public static Type[] Toggles =>
    [
        typeof(UserVariableMode)
    ];

    public static string TestArguments() =>
        string.IsNullOrWhiteSpace(VariableName) ? "Missing user variable name" : null;
}