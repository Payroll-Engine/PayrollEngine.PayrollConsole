using System;
using System.IO;
using System.Text.Json;
using System.Diagnostics;
using System.Threading.Tasks;
using System.Collections.Generic;
using PayrollEngine.Client.Command;

namespace PayrollEngine.PayrollConsole.Commands;

/// <summary>
/// User variable command
/// </summary>
[Command("UserVariable")]
// ReSharper disable once UnusedType.Global
internal sealed class UserVariableCommand : CommandBase<UserVariableParameters>
{
    /// <summary>Process the variable</summary>
    /// <param name="context">Command execution context</param>
    /// <param name="parameters">Command parameters</param>
    /// <returns>Program exit ok, 0 on success</returns>
    protected override async Task<int> Execute(CommandContext context, UserVariableParameters parameters)
    {
        DisplayTitle(context.Console, "User variable");

        // variable value expressions
        parameters.VariableValue = await ParseVariableValueAsync(parameters.VariableName, parameters.VariableValue);

        // mode adjustment
        UserVariableMode? changedMode = null;
        var hasValue = !string.IsNullOrWhiteSpace(parameters.VariableValue);
        if (hasValue && parameters.VariableMode != UserVariableMode.Set)
        {
            changedMode = parameters.VariableMode;
            parameters.VariableMode = UserVariableMode.Set;
        }

        context.Console.DisplayTextLine($"Variable name    {parameters.VariableName}");
        if (!string.IsNullOrWhiteSpace(parameters.VariableValue))
        {
            context.Console.DisplayTextLine($"Variable value   {parameters.VariableValue}");
        }
        context.Console.DisplayTextLine(changedMode.HasValue
            ? $"Variable mode    {parameters.VariableMode} <- {changedMode}"
            : $"Variable mode    {parameters.VariableMode}");
        context.Console.DisplayNewLine();

        try
        {
            // mode preparation
            switch (parameters.VariableMode)
            {
                case UserVariableMode.Remove:
                    // set nul value to remove the environment variable
                    parameters.VariableValue = null;
                    break;
                case UserVariableMode.View:
                    // adjust the default mode in case of present variable value
                    if (!string.IsNullOrWhiteSpace(parameters.VariableValue))
                    {
                        parameters.VariableMode = UserVariableMode.Set;
                    }
                    break;
            }

            var existing = GetUserVariable(parameters.VariableName);
            switch (parameters.VariableMode)
            {
                case UserVariableMode.View:
                    if (!string.IsNullOrWhiteSpace(existing))
                    {
                        context.Console.DisplaySuccessLine($"{parameters.VariableName} -> {existing}");
                    }
                    else
                    {
                        context.Console.DisplayInfoLine($"User variable {parameters.VariableName} is not available");
                    }
                    break;
                case UserVariableMode.Set:
                case UserVariableMode.Remove:
                    // ensure null for empty variable
                    if (string.IsNullOrWhiteSpace(parameters.VariableValue))
                    {
                        parameters.VariableValue = null;
                    }

                    // unchanged variable
                    if (string.Equals(existing, parameters.VariableValue))
                    {
                        context.Console.DisplayInfoLine($"{parameters.VariableName} -> {parameters.VariableValue}");
                    }
                    else
                    {
                        // change variable
                        SetUserVariable(parameters.VariableName, parameters.VariableValue);
                        // test the change
                        var newValue = GetUserVariable(parameters.VariableName);
                        if (string.Equals(newValue, parameters.VariableValue))
                        {
                            if (parameters.VariableValue == null)
                            {
                                context.Console.DisplaySuccessLine($"User variable {parameters.VariableName} -> {existing} removed");
                            }
                            else
                            {
                                context.Console.DisplaySuccessLine(existing == null
                                    ? $"{parameters.VariableName} -> {parameters.VariableValue}"
                                    : $"{parameters.VariableName} {existing} -> {parameters.VariableValue}");
                            }
                        }
                        else
                        {
                            context.Console.DisplayErrorLine($"Error updating user variable {parameters.VariableName} ({GetUserVariable(parameters.VariableName)})");
                        }
                    }
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(parameters.VariableMode), parameters.VariableMode, null);
            }
            return (int)ProgramExitCode.Ok;
        }
        catch (Exception exception)
        {
            ProcessError(context.Console, exception);
            return (int)ProgramExitCode.GenericError;
        }
    }

    private static async Task<string> ParseVariableValueAsync(string variableName, string variableValue)
    {
        if (string.IsNullOrWhiteSpace(variableValue))
        {
            return null;
        }

        // json file
        if (variableValue.EndsWith(".json", StringComparison.InvariantCultureIgnoreCase))
        {
            if (!File.Exists(variableValue))
            {
                return null;
            }

            try
            {
                var json = await File.ReadAllTextAsync(variableValue);
                var jsonContent = JsonSerializer.Deserialize<Dictionary<string, string>>(json);
                if (jsonContent != null && jsonContent.TryGetValue(variableName, out var value))
                {
                    return value;
                }
            }
            catch (Exception exception)
            {
                Log.Error(exception, exception.GetBaseMessage());
                return null;
            }
            return null;
        }

        var assemblyFile = typeof(UserVariableCommand).Assembly.Location;
        var versionInfo = FileVersionInfo.GetVersionInfo(assemblyFile);

        switch (variableValue)
        {
            case "$FileVersion$":
                return versionInfo.FileVersion;
            case "$ProductVersion$":
                return versionInfo.ProductVersion;
            case "$ProductMajorPart$":
                return versionInfo.ProductMajorPart.ToString();
            case "$ProductMinorPart$":
                return versionInfo.ProductMinorPart.ToString();
            case "$ProductMajorBuild$":
                return versionInfo.ProductBuildPart.ToString();
            case "$ProductName$":
                return versionInfo.ProductName;
        }

        // variable value without expression
        return variableValue;
    }

    private static string GetUserVariable(string variableName) =>
        Environment.GetEnvironmentVariable(variableName, EnvironmentVariableTarget.User);

    private static void SetUserVariable(string variableName, string variableValue) =>
        Environment.SetEnvironmentVariable(variableName, variableValue, EnvironmentVariableTarget.User);

    /// <inheritdoc />
    public override bool BackendCommand => false;

    /// <inheritdoc />
    public override ICommandParameters GetParameters(CommandLineParser parser) =>
        UserVariableParameters.ParserFrom(parser);

    /// <inheritdoc />
    public override void ShowHelp(ICommandConsole console)
    {
        console.DisplayTitleLine("- UserVariable");
        console.DisplayTextLine("      View and change the environment user variable");
        console.DisplayTextLine("      Arguments:");
        console.DisplayTextLine("          1. Variable name [VariableName]");
        console.DisplayTextLine("          2. Variable value (optional) [VariableValue]");
        console.DisplayTextLine("             - simple value");
        console.DisplayTextLine("             - JSON file name, variable name is the field name");
        console.DisplayTextLine("             - predefined expressions:");
        console.DisplayTextLine("                  $FileVersion$: file version");
        console.DisplayTextLine("                  $ProductVersion$: product version");
        console.DisplayTextLine("                  $ProductMajorPart$: product version major part");
        console.DisplayTextLine("                  $ProductMinorPart$: product version minor part");
        console.DisplayTextLine("                  $ProductMajorBuild$: product version build part");
        console.DisplayTextLine("                  $ProductName$: product name");
        console.DisplayTextLine("      Toggles:");
        console.DisplayTextLine("          variable mode: /view, /set or /remove");
        console.DisplayTextLine("                         (default without value: view)");
        console.DisplayTextLine("                         (default with value: set)");
        console.DisplayTextLine("      Examples:");
        console.DisplayTextLine("          UserVariable MyVariable");
        console.DisplayTextLine("          UserVariable MyVariable MyValue");
        console.DisplayTextLine("          UserVariable MyVariable /remove");
    }
}