using PayrollEngine.Client.Command;
using PayrollEngine.PayrollConsole.Commands.ExtensionCommands;
using Xunit;

namespace PayrollEngine.PayrollConsole.Commands.Tests;

public class ExtensionManifestTests
{
    [Fact]
    public void Builder_NewManifest_UsesDefaults()
    {
        using var extensions = new TestExtensions();
        var extensionPath = extensions.AddExtension("MyExtension", ["MyCommand"], manifest: false);
        File.WriteAllText(Path.Combine(extensionPath, "settings.json"), "{}");

        var manifest = ExtensionManifestBuilder.Build(new() { Path = extensionPath });

        Assert.Equal("MyExtension", manifest.Name);
        Assert.Equal("1.0.0", manifest.Version);
        Assert.Equal(ExtensionManifest.ConsoleApiVersion, manifest.RequiresConsole);
        Assert.Equal(["MyExtension.dll"], manifest.Assemblies);
        Assert.Equal(["MyExtension.dll", "settings.json"], manifest.Files.Select(x => x.Path));
        Assert.Equal(ExtensionManifest.FileHash(Path.Combine(extensionPath, "MyExtension.dll")),
            manifest.Files[0].Sha256);
        Assert.Null(manifest.Validate(extensionPath));
        Assert.Null(manifest.VerifyFiles(extensionPath));
    }

    [Fact]
    public void Builder_ExistingManifest_KeepsValuesAndRefreshesHashes()
    {
        using var extensions = new TestExtensions();
        var extensionPath = extensions.AddExtension("MyExtension", ["MyCommand"], manifest: false);
        var manifestFile = Path.Combine(extensionPath, ExtensionManifest.FileName);
        var initial = ExtensionManifestBuilder.Build(new()
        {
            Path = extensionPath,
            Version = "3.1.0",
            Description = "My extension"
        });
        initial.Save(manifestFile);
        File.WriteAllText(Path.Combine(extensionPath, "settings.json"), "{}");

        var manifest = ExtensionManifestBuilder.Build(new() { Path = extensionPath });

        Assert.Equal("3.1.0", manifest.Version);
        Assert.Equal("My extension", manifest.Description);
        Assert.DoesNotContain(manifest.Files, x => x.Path == ExtensionManifest.FileName);
        Assert.Contains(manifest.Files, x => x.Path == "settings.json");
    }

    [Fact]
    public void Builder_MissingAssembly_Fails()
    {
        using var extensions = new TestExtensions();
        var extensionPath = extensions.ExtensionPath("MyExtension");
        Directory.CreateDirectory(extensionPath);

        Assert.Throws<PayrollException>(() => ExtensionManifestBuilder.Build(new() { Path = extensionPath }));
    }

    [Theory]
    [InlineData("1.0", true)]
    [InlineData("1.2", true)]
    [InlineData("1.3", false)]
    [InlineData("2.0", false)]
    [InlineData("0.9", false)]
    public void ExtensionApiVersion_Compatibility(string required, bool supported)
    {
        Assert.Equal(new Version(1, 2), CommandManager.ExtensionApiVersion);
        Assert.Equal(supported, CommandManager.IsExtensionApiSupported(Version.Parse(required)));
    }

    [Fact]
    public void CommandManager_DuplicateCommandName_IgnoresCase()
    {
        var commandManager = TestExtensions.NewCommandManager();
        using var extensions = new TestExtensions();
        var path = TestExtensions.Compile(extensions.BuildPath, "MyExtension",
            TestExtensions.CommandSource("MyExtension", ["MyCommand", "HELP"]));
        var assembly = System.Reflection.Assembly.LoadFile(path);

        var exception = Assert.Throws<CommandException>(() =>
            commandManager.RegisterAssemblies([assembly], new CommandOrigin("MyExtension", "1.0.0", path)));

        Assert.Contains("Duplicate command HELP", exception.Message);
        Assert.Null(commandManager.GetCommand("MyCommand"));
        Assert.NotNull(commandManager.GetCommand("help"));
    }
}
