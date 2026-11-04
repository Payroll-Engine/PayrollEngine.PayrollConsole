using System.Runtime.Loader;
using PayrollEngine.Client.Command;
using PayrollEngine.PayrollConsole.Commands.ExtensionCommands;
using Xunit;

namespace PayrollEngine.PayrollConsole.Commands.Tests;

public class ExtensionLoaderTests
{
    [Fact]
    public void Extension_WithManifest_IsLoaded()
    {
        using var extensions = new TestExtensions();
        extensions.AddExtension("MyExtension", ["MyCommand", "MyOtherCommand"]);
        var commandManager = TestExtensions.NewCommandManager();

        var result = new ExtensionLoader(commandManager).Load(extensions.ExtensionsPath);

        var info = Assert.Single(result);
        Assert.Equal(ExtensionStatus.Loaded, info.Status);
        Assert.Equal("MyExtension", info.Name);
        Assert.Equal("1.0.0", info.Version);
        Assert.Equal(["MyCommand", "MyOtherCommand"], info.Commands.Order());
        // ReSharper disable once StringLiteralTypo
        Assert.NotNull(commandManager.GetCommand("mycommand"));
        var origin = commandManager.GetCommandOrigin("MyCommand");
        Assert.True(origin.IsExtension);
        Assert.Equal("MyExtension", origin.Name);
        Assert.False(commandManager.GetCommandOrigin("Help").IsExtension);
    }

    [Fact]
    public void Extension_ContractTypes_AreSharedWithDefaultContext()
    {
        using var extensions = new TestExtensions();
        var extensionPath = extensions.AddExtension("MyExtension", ["MyCommand"], manifest: false);
        // a private copy of the contract assembly must be ignored
        File.Copy(TestExtensions.CoreAssembly.Location,
            Path.Combine(extensionPath, Path.GetFileName(TestExtensions.CoreAssembly.Location)));
        TestExtensions.WriteManifest(extensionPath);
        var commandManager = TestExtensions.NewCommandManager();

        var info = Assert.Single(new ExtensionLoader(commandManager).Load(extensions.ExtensionsPath));

        Assert.Equal(ExtensionStatus.Loaded, info.Status);
        var command = commandManager.GetCommand("MyCommand");
        var commandType = command.GetType();
        Assert.Contains(typeof(ICommand), commandType.GetInterfaces());
        Assert.Same(typeof(CommandBase), commandType.BaseType?.BaseType);
        Assert.Same(AssemblyLoadContext.Default, AssemblyLoadContext.GetLoadContext(TestExtensions.CoreAssembly));
        var extensionContext = AssemblyLoadContext.GetLoadContext(commandType.Assembly);
        Assert.NotSame(AssemblyLoadContext.Default, extensionContext);
        Assert.IsType<ExtensionLoadContext>(extensionContext);
    }

    [Fact]
    public void Extension_DuplicateCoreCommand_IsRejectedAtomically()
    {
        using var extensions = new TestExtensions();
        extensions.AddExtension("MyExtension", ["MyCommand", "help"]);
        var commandManager = TestExtensions.NewCommandManager();

        var info = Assert.Single(new ExtensionLoader(commandManager).Load(extensions.ExtensionsPath));

        Assert.Equal(ExtensionStatus.Rejected, info.Status);
        Assert.Contains("Duplicate command help", info.Reason);
        Assert.Contains("extension MyExtension 1.0.0", info.Reason);
        Assert.Contains("conflicts with core", info.Reason);
        Assert.Null(commandManager.GetCommand("MyCommand"));
        Assert.False(commandManager.GetCommandOrigin("Help").IsExtension);
    }

    [Fact]
    public void Extension_DuplicateExtensionCommand_RejectsLaterExtension()
    {
        using var extensions = new TestExtensions();
        extensions.AddExtension("SecondExtension", ["SharedCommand", "SecondCommand"]);
        extensions.AddExtension("FirstExtension", ["SharedCommand"]);
        var commandManager = TestExtensions.NewCommandManager();

        var result = new ExtensionLoader(commandManager).Load(extensions.ExtensionsPath);

        Assert.Equal(["FirstExtension", "SecondExtension"], result.Select(x => x.Name));
        Assert.Equal(ExtensionStatus.Loaded, result[0].Status);
        Assert.Equal(ExtensionStatus.Rejected, result[1].Status);
        Assert.Contains("extension SecondExtension", result[1].Reason);
        Assert.Contains("extension FirstExtension", result[1].Reason);
        Assert.Equal("FirstExtension", commandManager.GetCommandOrigin("SharedCommand").Name);
        Assert.Null(commandManager.GetCommand("SecondCommand"));
    }

    [Fact]
    public void Extension_HashMismatch_IsRejected()
    {
        using var extensions = new TestExtensions();
        var extensionPath = extensions.AddExtension("MyExtension", ["MyCommand"]);
        File.AppendAllText(Path.Combine(extensionPath, "MyExtension.dll"), "x");
        var commandManager = TestExtensions.NewCommandManager();

        var info = Assert.Single(new ExtensionLoader(commandManager).Load(extensions.ExtensionsPath));

        Assert.Equal(ExtensionStatus.Rejected, info.Status);
        Assert.Contains("Hash mismatch of file MyExtension.dll", info.Reason);
        Assert.Null(commandManager.GetCommand("MyCommand"));
    }

    [Fact]
    public void Extension_UndeclaredAssembly_IsRejected()
    {
        using var extensions = new TestExtensions();
        var extensionPath = extensions.AddExtension("MyExtension", ["MyCommand"]);
        TestExtensions.Compile(extensionPath, "Unknown", "public class Unknown { }");
        var commandManager = TestExtensions.NewCommandManager();

        var info = Assert.Single(new ExtensionLoader(commandManager).Load(extensions.ExtensionsPath));

        Assert.Equal(ExtensionStatus.Rejected, info.Status);
        Assert.Contains("Undeclared file(s) Unknown.dll", info.Reason);
    }

    [Fact]
    public void Extension_MissingDependency_IsRejected()
    {
        using var extensions = new TestExtensions();
        var dependency = TestExtensions.Compile(extensions.BuildPath, "MyDependency",
            "namespace MyDependency { public class DependencyBase { } }");
        extensions.AddExtension("MyExtension", ["MyCommand"],
            extraSource: "public class MyHelper : MyDependency.DependencyBase { }",
            references: [dependency]);
        var commandManager = TestExtensions.NewCommandManager();

        var info = Assert.Single(new ExtensionLoader(commandManager).Load(extensions.ExtensionsPath));

        Assert.Equal(ExtensionStatus.Rejected, info.Status);
        Assert.Contains("MyDependency", info.Reason);
        Assert.Null(commandManager.GetCommand("MyCommand"));
    }

    [Fact]
    public void Extension_DependencyVersions_AreIsolated()
    {
        using var extensions = new TestExtensions();
        const string librarySource = "namespace MyLibrary { public static class Info { public static string Version => \"{0}\"; } }";
        const string probeSource = "public static class Probe { public static string LibraryVersion() => MyLibrary.Info.Version; }";
        foreach (var (name, version) in new[] { ("FirstExtension", "1.0.0"), ("SecondExtension", "2.0.0") })
        {
            var extensionPath = extensions.ExtensionPath(name);
            Directory.CreateDirectory(extensionPath);
            var library = TestExtensions.Compile(extensionPath, "MyLibrary",
                librarySource.Replace("{0}", version), version: version);
            TestExtensions.Compile(extensionPath, name,
                TestExtensions.CommandSource(name, [$"{name}Command"]) + probeSource, references: [library]);
            TestExtensions.WriteManifest(extensionPath);
        }
        var commandManager = TestExtensions.NewCommandManager();

        var result = new ExtensionLoader(commandManager).Load(extensions.ExtensionsPath);

        Assert.All(result, x => Assert.Equal(ExtensionStatus.Loaded, x.Status));
        Assert.Equal("1.0.0", ProbeVersion(commandManager.GetCommand("FirstExtensionCommand")));
        Assert.Equal("2.0.0", ProbeVersion(commandManager.GetCommand("SecondExtensionCommand")));
    }

    private static string ProbeVersion(ICommand command) =>
        (string)command.GetType().Assembly.GetType("Probe")!.GetMethod("LibraryVersion")!.Invoke(null, null);

    [Fact]
    public void LooseAssembly_WithoutManifest_IsRejected()
    {
        using var extensions = new TestExtensions();
        TestExtensions.Compile(extensions.ExtensionsPath, "MyExtension",
            TestExtensions.CommandSource("MyExtension", ["MyCommand"]));
        var commandManager = TestExtensions.NewCommandManager();

        var info = Assert.Single(new ExtensionLoader(commandManager).Load(extensions.ExtensionsPath));

        Assert.Equal(ExtensionStatus.Rejected, info.Status);
        Assert.Contains("without extension folder", info.Reason);
        Assert.Null(commandManager.GetCommand("MyCommand"));
    }

    [Fact]
    public void ExtensionFolder_WithoutManifest_IsRejected()
    {
        using var extensions = new TestExtensions();
        extensions.AddExtension("MyExtension", ["MyCommand"], manifest: false);
        var commandManager = TestExtensions.NewCommandManager();

        var info = Assert.Single(new ExtensionLoader(commandManager).Load(extensions.ExtensionsPath));

        Assert.Equal(ExtensionStatus.Rejected, info.Status);
        Assert.Contains("Missing manifest extension.json", info.Reason);
        Assert.Null(commandManager.GetCommand("MyCommand"));
    }

    [Fact]
    public void Extensions_AreLoadedInAlphabeticalFolderOrder()
    {
        using var extensions = new TestExtensions();
        extensions.AddExtension("Zeta", ["ZetaCommand"]);
        extensions.AddExtension("alpha", ["AlphaCommand"]);
        extensions.AddExtension("Beta", ["BetaCommand"]);

        var first = new ExtensionLoader(TestExtensions.NewCommandManager()).Load(extensions.ExtensionsPath);
        var second = new ExtensionLoader(TestExtensions.NewCommandManager()).Load(extensions.ExtensionsPath);

        Assert.Equal(["alpha", "Beta", "Zeta"], first.Select(x => x.Name));
        Assert.Equal(first.Select(x => x.Name), second.Select(x => x.Name));
    }

    [Theory]
    [InlineData("1.99")]
    [InlineData("2.0")]
    [InlineData("0.9")]
    public void Extension_UnsupportedConsoleVersion_IsRejected(string requiresConsole)
    {
        using var extensions = new TestExtensions();
        extensions.AddExtension("MyExtension", ["MyCommand"], requiresConsole: requiresConsole);
        var commandManager = TestExtensions.NewCommandManager();

        var info = Assert.Single(new ExtensionLoader(commandManager).Load(extensions.ExtensionsPath));

        Assert.Equal(ExtensionStatus.Rejected, info.Status);
        Assert.Contains($"Requires console extension API {requiresConsole}", info.Reason);
    }

    [Fact]
    public void Extension_ManifestNameMismatch_IsRejected()
    {
        using var extensions = new TestExtensions();
        var extensionPath = extensions.AddExtension("MyExtension", ["MyCommand"]);
        var manifestFile = Path.Combine(extensionPath, ExtensionManifest.FileName);
        var manifest = ExtensionManifest.Load(manifestFile);
        manifest.Name = "OtherExtension";
        manifest.Save(manifestFile);

        var info = Assert.Single(new ExtensionLoader(TestExtensions.NewCommandManager()).Load(extensions.ExtensionsPath));

        Assert.Equal(ExtensionStatus.Rejected, info.Status);
        Assert.Contains("does not match the folder name", info.Reason);
    }

    [Fact]
    public void Extension_ManifestPathOutsideFolder_IsRejected()
    {
        using var extensions = new TestExtensions();
        var extensionPath = extensions.AddExtension("MyExtension", ["MyCommand"]);
        var manifestFile = Path.Combine(extensionPath, ExtensionManifest.FileName);
        var manifest = ExtensionManifest.Load(manifestFile);
        manifest.Files.Add(new() { Path = "../Other.dll", Sha256 = new string('0', 64) });
        manifest.Save(manifestFile);

        var info = Assert.Single(new ExtensionLoader(TestExtensions.NewCommandManager()).Load(extensions.ExtensionsPath));

        Assert.Equal(ExtensionStatus.Rejected, info.Status);
        Assert.Contains("outside of the extension folder", info.Reason);
    }

    [Fact]
    public void FailingExtension_DoesNotBlockOtherExtensions()
    {
        using var extensions = new TestExtensions();
        var brokenPath = extensions.AddExtension("Broken", ["BrokenCommand"]);
        File.WriteAllText(Path.Combine(brokenPath, ExtensionManifest.FileName), "{ invalid json");
        extensions.AddExtension("MyExtension", ["MyCommand"]);
        var commandManager = TestExtensions.NewCommandManager();

        var result = new ExtensionLoader(commandManager).Load(extensions.ExtensionsPath);

        Assert.Equal(ExtensionStatus.Rejected, result.Single(x => x.Name == "Broken").Status);
        Assert.Equal(ExtensionStatus.Loaded, result.Single(x => x.Name == "MyExtension").Status);
        Assert.NotNull(commandManager.GetCommand("MyCommand"));
    }

    [Fact]
    public void MissingExtensionsFolder_LoadsNothing()
    {
        var result = new ExtensionLoader(TestExtensions.NewCommandManager())
            .Load(Path.Combine(Path.GetTempPath(), "exttest-" + Guid.NewGuid().ToString("N")));
        Assert.Empty(result);
    }
}
