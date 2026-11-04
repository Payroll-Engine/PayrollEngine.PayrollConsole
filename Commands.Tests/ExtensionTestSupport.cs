using System.Reflection;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using PayrollEngine.Client.Command;
using PayrollEngine.PayrollConsole.Commands.ExtensionCommands;

namespace PayrollEngine.PayrollConsole.Commands.Tests;

/// <summary>
/// Temporary extensions folder with extensions compiled at test runtime
/// </summary>
internal sealed class TestExtensions : IDisposable
{
    private readonly string root = Path.Combine(Path.GetTempPath(), "exttest-" + Guid.NewGuid().ToString("N"));

    internal string ExtensionsPath => Path.Combine(root, ExtensionLoader.ExtensionsFolderName);

    /// <summary>Folder for build outputs which are not within the extensions folder</summary>
    internal string BuildPath => Path.Combine(root, "build");

    internal TestExtensions()
    {
        Directory.CreateDirectory(ExtensionsPath);
        Directory.CreateDirectory(BuildPath);
    }

    internal string ExtensionPath(string name) => Path.Combine(ExtensionsPath, name);

    /// <summary>
    /// Create an extension folder with a command assembly and a generated manifest
    /// </summary>
    internal string AddExtension(string name, string[] commandNames, string requiresConsole = null,
        string extraSource = null, string[] references = null, bool manifest = true)
    {
        var extensionPath = ExtensionPath(name);
        Directory.CreateDirectory(extensionPath);
        Compile(extensionPath, name, CommandSource(name, commandNames) + extraSource, references: references);
        if (manifest)
        {
            WriteManifest(extensionPath, requiresConsole);
        }
        return extensionPath;
    }

    internal static void WriteManifest(string extensionPath, string requiresConsole = null) =>
        ExtensionManifestBuilder.Build(new()
        {
            Path = extensionPath,
            RequiresConsole = requiresConsole
        }).Save(Path.Combine(extensionPath, ExtensionManifest.FileName));

    /// <summary>
    /// Source code of an extension assembly with commands
    /// </summary>
    internal static string CommandSource(string ns, IEnumerable<string> commandNames)
    {
        var commands = string.Concat(commandNames.Select(command => $$"""

            [Command("{{command}}")]
            public sealed class {{command}}Command : CommandBase<{{command}}Parameters>
            {
                protected override Task<int> Execute(CommandContext context, {{command}}Parameters parameters) =>
                    Task.FromResult(0);
                public override bool BackendCommand => false;
                public override ICommandParameters GetParameters(CommandLineParser parser) => new {{command}}Parameters();
                public override void ShowHelp(ICommandConsole console) { }
            }

            public sealed class {{command}}Parameters : ICommandParameters
            {
                public Type[] Toggles => null;
                public string Test() => null;
            }
            """));
        return $$"""
            using System;
            using System.Threading.Tasks;
            using PayrollEngine.Client.Command;

            [assembly: System.Reflection.AssemblyVersion("1.0.0")]

            namespace {{ns}}
            {
            {{commands}}
            }

            """;
    }

    /// <summary>
    /// Compile an assembly into a folder
    /// </summary>
    internal static string Compile(string outputPath, string assemblyName, string source,
        string version = null, string[] references = null)
    {
        var platformReferences = ((string)AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES"))!
            .Split(Path.PathSeparator)
            .Select(x => MetadataReference.CreateFromFile(x));
        var extraReferences = (references ?? []).Select(x => MetadataReference.CreateFromFile(x));
        if (version != null)
        {
            source = $"[assembly: System.Reflection.AssemblyVersion(\"{version}\")]\n" + source;
        }

        var compilation = CSharpCompilation.Create(assemblyName,
            [CSharpSyntaxTree.ParseText(source)],
            platformReferences.Concat(extraReferences),
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));
        var path = Path.Combine(outputPath, $"{assemblyName}.dll");
        var result = compilation.Emit(path);
        if (!result.Success)
        {
            throw new InvalidOperationException(string.Join(Environment.NewLine,
                result.Diagnostics.Where(x => x.Severity == DiagnosticSeverity.Error)));
        }
        return path;
    }

    internal static CommandManager NewCommandManager()
    {
        var commandManager = new CommandManager(new CommandConsole());
        commandManager.RegisterAssembly(typeof(HelpCommand).Assembly);
        return commandManager;
    }

    internal static Assembly CoreAssembly => typeof(ICommand).Assembly;

    public void Dispose()
    {
        try
        {
            Directory.Delete(root, true);
        }
        catch (IOException)
        {
            // assemblies loaded into a non-collectible context stay locked
        }
        catch (UnauthorizedAccessException)
        {
            // assemblies loaded into a non-collectible context stay locked
        }
    }
}
