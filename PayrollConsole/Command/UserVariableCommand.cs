using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;
using PayrollEngine.PayrollConsole.Shared;

namespace PayrollEngine.PayrollConsole.Command;

internal sealed class UserVariableCommand : CommandBase
{
    /// <summary>Process the variable</summary>
    /// <param name="variableName">The variable name</param>
    /// <param name="variableValue">The variable value</param>
    /// <param name="mode">The variable mode</param>
    internal async Task<ProgramExitCode> ProcessVariableAsync(string variableName, string variableValue = null,
        UserVariableMode mode = UserVariableMode.View)
    {
        DisplayTitle("User variable");

        // variable value expressions
        variableValue = await ParseVariableValueAsync(variableName, variableValue);

        // mode adjustment
        UserVariableMode? changedMode = null;
        var hasValue = !string.IsNullOrWhiteSpace(variableValue);
        if (hasValue && mode != UserVariableMode.Set)
        {
            changedMode = mode;
            mode = UserVariableMode.Set;
        }

        ConsoleTool.DisplayTextLine($"Variable name    {variableName}");
        if (!string.IsNullOrWhiteSpace(variableValue))
        {
            ConsoleTool.DisplayTextLine($"Variable value   {variableValue}");
        }
        ConsoleTool.DisplayTextLine(changedMode.HasValue
            ? $"Variable mode    {mode} <- {changedMode}"
            : $"Variable mode    {mode}");
        ConsoleTool.DisplayNewLine();

        try
        {
            // mode preparation
            switch (mode)
            {
                case UserVariableMode.Remove:
                    // set nul value to remove the environment variable
                    variableValue = null;
                    break;
                case UserVariableMode.View:
                    // adjust the default mode in case of present variable value
                    if (!string.IsNullOrWhiteSpace(variableValue))
                    {
                        mode = UserVariableMode.Set;
                    }
                    break;
            }

            var existing = GetUserVariable(variableName);
            switch (mode)
            {
                case UserVariableMode.View:
                    if (!string.IsNullOrWhiteSpace(existing))
                    {
                        ConsoleTool.DisplaySuccessLine($"{variableName} -> {existing}");
                    }
                    else
                    {
                        ConsoleTool.DisplayInfoLine($"User variable {variableName} is not available");
                    }
                    break;
                case UserVariableMode.Set:
                case UserVariableMode.Remove:
                    // ensure null for empty variable
                    if (string.IsNullOrWhiteSpace(variableValue))
                    {
                        variableValue = null;
                    }

                    // unchanged variable
                    if (string.Equals(existing, variableValue))
                    {
                        ConsoleTool.DisplayInfoLine($"{variableName} -> {variableValue}");
                    }
                    else
                    {
                        // change variable
                        SetUserVariable(variableName, variableValue);
                        // test the change
                        var newValue = GetUserVariable(variableName);
                        if (string.Equals(newValue, variableValue))
                        {
                            if (variableValue == null)
                            {
                                ConsoleTool.DisplaySuccessLine($"User variable {variableName} -> {existing} removed");
                            }
                            else
                            {
                                ConsoleTool.DisplaySuccessLine(existing == null
                                    ? $"{variableName} -> {variableValue}"
                                    : $"{variableName} {existing} -> {variableValue}");
                            }
                        }
                        else
                        {
                            ConsoleTool.DisplayErrorLine($"Error updating user variable {variableName} ({GetUserVariable(variableName)})");
                        }
                    }
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(mode), mode, null);
            }
            return ProgramExitCode.Ok;
        }
        catch (Exception exception)
        {
            Log.Error(exception, exception.GetBaseMessage());
            ConsoleTool.DisplayErrorLine($"User variable failed: {exception.GetBaseMessage()}");
            return ProgramExitCode.GenericError;
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

    /// <summary>Show the application help</summary>
    internal static void ShowHelp()
    {
        ConsoleTool.DisplayTitleLine("- UserVariable");
        ConsoleTool.DisplayTextLine("      View and change the environment user variable");
        ConsoleTool.DisplayTextLine("      Arguments:");
        ConsoleTool.DisplayTextLine("          1. Variable name [VariableName]");
        ConsoleTool.DisplayTextLine("          2. Variable value (optional) [VariableValue]");
        ConsoleTool.DisplayTextLine("             - simple value");
        ConsoleTool.DisplayTextLine("             - JSON file name, variable name is the field name");
        ConsoleTool.DisplayTextLine("             - predefined expressions:");
        ConsoleTool.DisplayTextLine("                  $FileVersion$: file version");
        ConsoleTool.DisplayTextLine("                  $ProductVersion$: product version");
        ConsoleTool.DisplayTextLine("                  $ProductMajorPart$: product version major part");
        ConsoleTool.DisplayTextLine("                  $ProductMinorPart$: product version minor part");
        ConsoleTool.DisplayTextLine("                  $ProductMajorBuild$: product version build part");
        ConsoleTool.DisplayTextLine("                  $ProductName$: product name");
        ConsoleTool.DisplayTextLine("      Toggles:");
        ConsoleTool.DisplayTextLine("          variable mode: /view, /set or /remove");
        ConsoleTool.DisplayTextLine("                         (default without value: view)");
        ConsoleTool.DisplayTextLine("                         (default with value: set)");
        ConsoleTool.DisplayTextLine("      Examples:");
        ConsoleTool.DisplayTextLine("          UserVariable MyVariable");
        ConsoleTool.DisplayTextLine("          UserVariable MyVariable MyValue");
        ConsoleTool.DisplayTextLine("          UserVariable MyVariable /remove");
    }
}