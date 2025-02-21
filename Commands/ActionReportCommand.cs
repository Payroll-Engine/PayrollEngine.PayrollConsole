﻿using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.Json;
using System.Threading.Tasks;
using Task = System.Threading.Tasks.Task;
using System.Collections.Generic;
using PayrollEngine.Client.Model;
using PayrollEngine.Client.Scripting;
using PayrollEngine.Client.Command;

namespace PayrollEngine.PayrollConsole.Commands;

[Command("ActionReport")]
// ReSharper disable once UnusedType.Global
internal sealed class ActionReportCommand : CommandBase<ActionReportParameters>
{
    /// <summary>Report actions</summary>
    protected override Task<int> Execute(CommandContext context, ActionReportParameters parameters)
    {
        try
        {
            // file
            var displayName = new FileInfo(parameters.FileName).Name;
            if (!File.Exists(parameters.FileName))
            {
                context.Console.DisplayErrorLine($"Missing assembly {parameters.FileName}");
                return Task.FromResult((int)ProgramExitCode.GenericError);
            }
            context.Console.DisplayText($"Analyzing actions in assembly {displayName}...");
            var actions = GetActions(context.Console, parameters.FileName);
            context.Console.DisplayNewLine();

            if (actions == null)
            {
                return Task.FromResult((int)ProgramExitCode.GenericError);
            }
            if (!actions.Any())
            {
                context.Console.DisplayInfoLine($"Assembly {parameters.FileName} without actions");
            }
            else
            {
                // action report
                WriteActions(context.Console, actions);

                // save actions to json
                var fileInfo = new FileInfo(parameters.FileName);
                var jsonFileName = fileInfo.Name.Replace(fileInfo.Extension, ".json");
                SaveActions(jsonFileName, actions);

                context.Console.DisplaySuccessLine($"{actions.Count} actions in assembly {displayName}");
            }
            return Task.FromResult((int)ProgramExitCode.Ok);
        }
        catch (Exception exception)
        {
            ProcessError(context.Console, exception);
            return Task.FromResult((int)ProgramExitCode.GenericError);
        }
    }

    private static void SaveActions(string fileName, List<ActionInfo> actions)
    {
        if (string.IsNullOrWhiteSpace(fileName))
        {
            throw new ArgumentException(nameof(fileName));
        }

        if (File.Exists(fileName))
        {
            File.Delete(fileName);
        }

        File.WriteAllText(fileName, JsonSerializer.Serialize(actions,
            new JsonSerializerOptions { WriteIndented = true }));
    }

    private static List<ActionInfo> GetActions(ICommandConsole console, string fileName)
    {
        Assembly assembly;
        try
        {
            assembly = Assembly.LoadFrom(fileName);
        }
        catch (Exception exception)
        {
            console.DisplayErrorLine($"Error in assembly {fileName}: {exception.GetBaseMessage()}");
            return null;
        }

        // setup action cache
        var actions = new List<ActionInfo>();
        foreach (var type in assembly.GetTypes().Where(x => !x.IsGenericType && !x.IsNested))
        {

            // action provider attribute
            var providerAttribute = GetProviderAttribute(type);
            if (providerAttribute == null)
            {
                continue;
            }

            foreach (var typeMethod in type.GetMethods(BindingFlags.Public | BindingFlags.Instance))
            {
                // action attribute
                var actionAttribute = GetActionAttribute(typeMethod);
                if (actionAttribute == null)
                {
                    continue;
                }

                // action attribute
                var actionInfo = new ActionInfo(providerAttribute.Type)
                {
                    Namespace = providerAttribute.Namespace,
                    Name = GetTypeValue<string>(actionAttribute, nameof(ActionAttribute.Name)),
                    Description = GetTypeValue<string>(actionAttribute, nameof(ActionAttribute.Description)),
                    Categories = [.. GetTypeValue<string[]>(actionAttribute, nameof(ActionAttribute.Categories))],
                    Parameters = [],
                    Issues = []
                };
                actions.Add(actionInfo);

                // action parameter attributes (optional)
                var parameterAttributes = GetActionParameterAttributes(typeMethod, typeof(ActionParameterAttribute));
                if (parameterAttributes != null)
                {
                    foreach (var parameterAttribute in parameterAttributes)
                    {
                        var name = GetTypeValue<string>(parameterAttribute, nameof(ActionParameterAttribute.Name));

                        // test parameter
                        if (!typeMethod.GetParameters().Any(x => string.Equals(x.Name, name)))
                        {
                            console.DisplayErrorLine($"Invalid action parameter {actionInfo.Name}.{name}");
                            return null;
                        }

                        var actionParameterInfo = new ActionParameterInfo
                        {
                            Name = name,
                            Description = GetTypeValue<string>(parameterAttribute, nameof(ActionParameterAttribute.Description)),
                            ValueReferences = [],
                            ValueSources = [],
                            ValueTypes = []
                        };

                        var valueReferences = GetTypeValue<string[]>(parameterAttribute, nameof(ActionParameterAttribute.ValueReferences));
                        if (valueReferences != null)
                        {
                            actionParameterInfo.ValueReferences.AddRange(valueReferences);
                        }
                        var valueSources = GetTypeValue<string[]>(parameterAttribute, nameof(ActionParameterAttribute.ValueSources));
                        if (valueSources != null)
                        {
                            actionParameterInfo.ValueSources.AddRange(valueSources);
                        }
                        var valueTypes = GetTypeValue<string[]>(parameterAttribute, nameof(ActionParameterAttribute.ValueTypes));
                        if (valueTypes != null)
                        {
                            actionParameterInfo.ValueTypes.AddRange(valueTypes);
                        }

                        actionInfo.Parameters.Add(actionParameterInfo);
                    }
                }

                // action issue attributes (optional)
                var issuesAttributes = GetActionParameterAttributes(typeMethod, typeof(ActionIssueAttribute));
                if (issuesAttributes != null)
                {
                    foreach (var issueAttribute in issuesAttributes)
                    {
                        actionInfo.Issues.Add(new()
                        {
                            Name = GetTypeValue<string>(issueAttribute, nameof(ActionIssueAttribute.Name)),
                            Message = GetTypeValue<string>(issueAttribute, nameof(ActionIssueAttribute.Message)),
                            ParameterCount = GetTypeValue<int>(issueAttribute, nameof(ActionIssueAttribute.ParameterCount))
                        });
                    }
                }
            }
        }

        // order by action name
        actions.Sort((x, y) => string.CompareOrdinal(x.FullName, y.FullName));
        return actions;
    }

    private static ActionProviderAttribute GetProviderAttribute(MemberInfo type)
    {
        var providerAttributeName = typeof(ActionProviderAttribute).FullName;
        foreach (var typeAttribute in type.GetCustomAttributes())
        {
            // provider attribute type
            if (string.Equals(providerAttributeName, typeAttribute.GetType().FullName))
            {
                return typeAttribute as ActionProviderAttribute;
            }
        }
        return null;
    }

    private static Attribute GetActionAttribute(MemberInfo method)
    {
        var actionAttributeName = nameof(ActionAttribute);
        var actionAttributeNamespace = typeof(ActionAttribute).Namespace;
        if (actionAttributeNamespace == null)
        {
            return null;
        }
        foreach (var methodAttribute in method.GetCustomAttributes())
        {
            // provider attribute type
            var methodTypeName = methodAttribute.GetType().FullName;
            if (methodTypeName != null &&
                methodTypeName.StartsWith(actionAttributeNamespace) &&
                methodTypeName.EndsWith(actionAttributeName))
            {
                return methodAttribute;
            }
        }
        return null;
    }

    private static List<Attribute> GetActionParameterAttributes(MemberInfo method, Type attributeType)
    {
        var attributes = new List<Attribute>();
        var attributeName = attributeType.FullName;
        foreach (var methodAttribute in method.GetCustomAttributes())
        {
            // provider attribute type
            if (string.Equals(attributeName, methodAttribute.GetType().FullName))
            {
                attributes.Add(methodAttribute);
            }
        }
        return attributes.Any() ? attributes : null;
    }

    private static void WriteActions(ICommandConsole console, List<ActionInfo> actions)
    {
        WriteActionLine(console);
        foreach (var action in actions)
        {
            // action
            WriteAction(console, action);

            // action parameters
            foreach (var parameter in action.Parameters)
            {
                WriteActionParameter(console, parameter);
            }

            // action issues
            foreach (var issue in action.Issues)
            {
                WriteActionIssue(console, issue);
            }
            WriteActionLine(console);
        }
        console.DisplayNewLine();
    }

    private static void WriteActionLine(ICommandConsole console) =>
        console.DisplayInfoLine(new string('-', 3 + 20 + 35 + 30 + 20 + 20));

    private static void WriteAction(ICommandConsole console, ActionInfo action)
    {
        console.DisplayText($"{action.Namespace}.{action.Name} [{action.Source}]");
        console.DisplayInfoLine($" - {action.Description} ({action.FunctionType})");
    }

    private static void WriteActionParameter(ICommandConsole console, ActionParameterInfo actionParameter)
    {
        console.DisplayTextLine($" > {actionParameter.Name,-20}{actionParameter.Description,-35}" +
                                    $"{string.Join(",", actionParameter.ValueTypes),-30}" +
                                    $"{string.Join(",", actionParameter.ValueReferences),-20}" +
                                    $"{string.Join(",", actionParameter.ValueSources),-20}");
    }

    private static void WriteActionIssue(ICommandConsole console, ActionIssueInfo actionIssue)
    {
        var name = $"{actionIssue.Name} [{actionIssue.ParameterCount}]";
        console.DisplayTextLine($" ! {name,-55}{actionIssue.Message}");
    }

    private static T GetTypeValue<T>(object source, string propertyName)
    {
        if (source == null)
        {
            return default;
        }
        var property = source.GetType().GetProperty(propertyName);
        if (property == null)
        {
            return default;
        }
        return (T)property.GetValue(source);
    }

    public override ICommandParameters GetParameters(CommandLineParser parser) =>
        ActionReportParameters.ParserFrom(parser);

    public override void ShowHelp(ICommandConsole console)
    {
        console.DisplayTitleLine("- ActionReport");
        console.DisplayTextLine("      Report actions from an assembly");
        console.DisplayTextLine("      Arguments:");
        console.DisplayTextLine("          1. action assembly file name [FileName]");
        console.DisplayTextLine("      Examples:");
        console.DisplayTextLine("          ActionReport MyAssembly.dll");
    }
}