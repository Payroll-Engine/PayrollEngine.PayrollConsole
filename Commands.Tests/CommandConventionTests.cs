using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;
using Xunit;

namespace PayrollEngine.PayrollConsole.Commands.Tests;

/// <summary>
/// Source conventions of the console commands
/// </summary>
public class CommandConventionTests
{
    /// <summary>
    /// Commands without title and parameter display: output commands used within command files
    /// (Write, Stopwatch) and the help screen
    /// </summary>
    private static readonly HashSet<string> DisplayExceptions = new(StringComparer.OrdinalIgnoreCase)
    {
        "Help",
        "Stopwatch",
        "Write"
    };

    [Fact]
    public void Commands_DisplayTitleAndParameters()
    {
        var commandFiles = CommandSourceFiles();
        Assert.NotEmpty(commandFiles);

        var violations = new List<string>();
        foreach (var (name, source) in commandFiles)
        {
            if (DisplayExceptions.Contains(name))
            {
                continue;
            }
            if (!source.Contains("DisplayTitle("))
            {
                violations.Add($"{name}: missing DisplayTitle");
            }
            if (!source.Contains("DisplayLevel == DisplayLevel.Full"))
            {
                violations.Add($"{name}: missing parameter display for DisplayLevel.Full");
            }
        }
        Assert.True(violations.Count == 0, string.Join(Environment.NewLine, violations));
    }

    [Fact]
    public void DisplayExceptions_AreExistingCommands()
    {
        var names = CommandSourceFiles().Select(x => x.Name).ToHashSet(StringComparer.OrdinalIgnoreCase);
        Assert.All(DisplayExceptions, x => Assert.Contains(x, names));
    }

    private static List<(string Name, string Source)> CommandSourceFiles([CallerFilePath] string testFile = "")
    {
        var commandsPath = Path.GetFullPath(Path.Combine(Path.GetDirectoryName(testFile)!, "..", "Commands"));
        var regex = new Regex("\\[Command\\(\"(?<name>[^\"]+)\"\\)\\]");
        return Directory.GetFiles(commandsPath, "*.cs", SearchOption.AllDirectories)
            .Where(x => !x.Contains($"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}") &&
                        !x.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}"))
            .Select(x => (File: x, Source: File.ReadAllText(x)))
            .Select(x => (Match: regex.Match(x.Source), x.Source))
            .Where(x => x.Match.Success)
            .Select(x => (x.Match.Groups["name"].Value, x.Source))
            .ToList();
    }
}
