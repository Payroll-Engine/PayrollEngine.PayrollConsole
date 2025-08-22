using System;
using System.Text;
using System.Linq;
using System.Reflection;
using System.Collections.Generic;
using PayrollEngine.Client.Model;

namespace PayrollEngine.PayrollConsole.Commands.ActionCommands;

internal static class ActionMarkdownWriter
{
    internal static string Write(List<ActionInfo> actions, Assembly assembly)
    {
        var buffer = new StringBuilder();

        // title
        var name = assembly.GetName();
        buffer.AppendLine($"# {name.Name} Actions");
        buffer.AppendLine("<br />");
        buffer.AppendLine();
        buffer.AppendLine($"> Assembly `{assembly.FullName}`");
        buffer.AppendLine();
        buffer.AppendLine("<br />");

        foreach (var action in actions)
        {
            buffer.Append(GetActionMarkdown(action));
        }

        return buffer.ToString();
    }

    private static string GetActionMarkdown(ActionInfo action)
    {
        var buffer = new StringBuilder();

        // action
        buffer.AppendLine($"{Environment.NewLine}---");
        buffer.AppendLine($"### {action.Namespace}.{action.Name}");

        buffer.AppendLine("| | |");
        buffer.AppendLine("|:-- |:-- |");
        buffer.AppendLine($"| Description      | {action.Description}      |");
        buffer.AppendLine($"| Function type    | `{action.FunctionType}`   |");
        // buffer.AppendLine($"| Source           | {action.Source}           |");

        // categories
        if (action.Categories.Any())
        {
            buffer.AppendLine($"| Categories       | {GetCategoriesMarkdown(action.Categories)} |");
        }

        // parameters
        if (action.Parameters.Any())
        {
            buffer.AppendLine($"| Parameters       | {GetParametersMarkdown(action.Parameters)} |");
        }

        // issues
        if (action.Issues.Any())
        {
            buffer.AppendLine($"| Issues           | {GetIssuesMarkdown(action.Issues)} |");
        }

        return buffer.ToString();
    }

    private static string GetCategoriesMarkdown(List<string> categories) =>
        string.Join(", ", categories.Select(x => $"`{x}`"));

    private static string GetParametersMarkdown(List<ActionParameterInfo> actionParameters)
    {
        var buffer = new StringBuilder();

        buffer.Append("<ul>");
        foreach (var actionParameter in actionParameters)
        {
            buffer.Append($"<li>`{actionParameter.Name}` <i>{actionParameter.Description}</i>");

            var hasTypes = actionParameter.ValueTypes.Any();
            var hasReferences = actionParameter.ValueReferences.Any();
            var hasSources = actionParameter.ValueSources.Any();
            if (hasTypes || hasReferences || hasSources)
            {
                buffer.Append("<ul>");
                if (hasTypes)
                {
                    buffer.Append($"<li>Types {GetRefListMarkdown(actionParameter.ValueTypes)}</li>");
                }
                if (hasReferences)
                {
                    buffer.Append($"<li>References {GetRefListMarkdown(actionParameter.ValueReferences)}</li>");
                }
                if (hasSources)
                {
                    buffer.Append($"<li>Sources {GetRefListMarkdown(actionParameter.ValueSources)}</li>");
                }
                buffer.Append("</ul>");
            }
            buffer.Append("</li>");
        }
        buffer.Append("</ul>");

        return buffer.ToString();
    }

    private static string GetIssuesMarkdown(List<ActionIssueInfo> issues)
    {
        var buffer = new StringBuilder();
        buffer.Append("<ul>");
        foreach (var issue in issues)
        {
            buffer.Append($"<li>`{issue.Name}` <i>{issue.Message}</i></li>");
        }
        buffer.Append("</ul>");
        return buffer.ToString();
    }

    private static string GetRefListMarkdown<T>(IEnumerable<T> items) =>
        string.Join(", ", items.Select(x => $"`{x}`"));
}