using System;
using PayrollEngine.Client;
using PayrollEngine.PayrollConsole.Shared;

namespace PayrollEngine.PayrollConsole.Arguments;

public static class UserVariableArguments
{
    public static string VariableName =>
        ConsoleArguments.Get(2);

    public static string VariableValue =>
        ConsoleArguments.Get(3);

    public static UserVariableMode VariableMode(UserVariableMode defaultValue = UserVariableMode.View) =>
        ConsoleArguments.GetEnumToggle(defaultValue);

    public static Type[] Toggles => new[]
    {
        typeof(UserVariableMode),
    };

    public static string TestArguments() =>
        string.IsNullOrWhiteSpace(VariableName) ? "Missing user variable name" : null;
}