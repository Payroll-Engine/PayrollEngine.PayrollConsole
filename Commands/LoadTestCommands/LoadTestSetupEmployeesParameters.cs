using System;
using PayrollEngine.Client.Command;

namespace PayrollEngine.PayrollConsole.Commands.LoadTestCommands;

/// <summary>Load test employee setup parameters</summary>
public class LoadTestSetupEmployeesParameters : ICommandParameters
{
    /// <summary>Path to Setup-Employees.json exchange file</summary>
    public string EmployeesFile { get; init; }

    /// <inheritdoc />
    public Type[] Toggles => null;

    /// <inheritdoc />
    public string Test()
    {
        if (string.IsNullOrWhiteSpace(EmployeesFile))
        {
            return "Missing employees file";
        }
        return null;
    }

    /// <summary>Parse command parameters</summary>
    public static LoadTestSetupEmployeesParameters ParserFrom(CommandLineParser parser) =>
        new()
        {
            EmployeesFile = parser.Get(2, nameof(EmployeesFile))
        };
}
