using System;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using System.Collections.Generic;
using PayrollEngine.Client;
using PayrollEngine.Client.Model;
using PayrollEngine.Client.Command;
using PayrollEngine.Client.Exchange;
using PayrollEngine.Client.Script;
using PayrollEngine.Client.Service.Api;
using PayrollEngine.Client.Scripting.Script;

namespace PayrollEngine.PayrollConsole.Commands.RegulationCommands;

/// <summary>
/// Installs a regulation NuGet package (.nupkg) into a PE backend tenant.
/// The package source can be a local file path, a file wildcard, or an HTTP(S) URL.
/// The package is opened as a ZIP archive; the manifest and import files are read
/// from the <c>regulation/</c> folder within the archive.
/// </summary>
internal sealed class RegulationPackageInstaller
{
    private const string RegulationFolder = "regulation/";
    private const string ManifestFileName = "regulation-package.json";

    private PayrollHttpClient HttpClient { get; }
    private ICommandConsole Console { get; }
    private IScriptParser ScriptParser { get; } = new ScriptParser();

    internal RegulationPackageInstaller(PayrollHttpClient httpClient, ICommandConsole console)
    {
        HttpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
        Console = console ?? throw new ArgumentNullException(nameof(console));
    }

    /// <summary>
    /// Installs the regulation package into the specified tenant.
    /// </summary>
    /// <param name="packageSource">Local file path, file wildcard, or HTTP(S) URL to the .nupkg</param>
    /// <param name="tenantIdentifier">Target tenant identifier</param>
    /// <param name="installMode">New or Overwrite</param>
    /// <param name="dryRun">DryRun validates without importing; Execute performs the import</param>
    /// <returns>0 on success, non-zero on error</returns>
    internal async Task<int> InstallAsync(
        string packageSource,
        string tenantIdentifier,
        RegulationInstallMode installMode,
        RegulationDryRun dryRun)
    {
        // --- resolve to a local file (download if URL) ---
        string localFile;
        bool downloadedTemp;
        try
        {
            (localFile, downloadedTemp) = await ResolveLocalFileAsync(packageSource);
        }
        catch (Exception ex)
        {
            Console.DisplayErrorLine($"Cannot load package: {ex.GetBaseException().Message}");
            return (int)ProgramExitCode.InvalidInput;
        }

        try
        {
            return await InstallLocalAsync(localFile, tenantIdentifier, installMode, dryRun);
        }
        finally
        {
            if (downloadedTemp)
            {
                TryDelete(localFile);
            }
        }
    }

    // -----------------------------------------------------------------------
    // Source resolution — local file or URL download
    // -----------------------------------------------------------------------

    /// <summary>
    /// Returns the path to a local .nupkg file.
    /// If <paramref name="source"/> is an HTTP(S) URL the package is downloaded
    /// to a temp file. The caller is responsible for deleting it when done.
    /// </summary>
    private async Task<(string localFile, bool isTemp)> ResolveLocalFileAsync(string source)
    {
        if (Uri.TryCreate(source, UriKind.Absolute, out var uri) &&
            (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps))
        {
            return await DownloadPackageAsync(uri);
        }

        // local path — already resolved by the command
        return (source, false);
    }

    private async Task<(string localFile, bool isTemp)> DownloadPackageAsync(Uri uri)
    {
        Console.DisplayTextLine($"Downloading      {uri}");

        using var http = new HttpClient();
        http.DefaultRequestHeaders.Add("User-Agent", "PayrollEngine.PayrollConsole");

        var response = await http.GetAsync(uri, HttpCompletionOption.ResponseHeadersRead);
        response.EnsureSuccessStatusCode();

        var tempFile = Path.Combine(Path.GetTempPath(),
            $"pe-regulation-{Guid.NewGuid():N}.nupkg");

        await using var fs = new FileStream(tempFile, FileMode.Create, FileAccess.Write, FileShare.None);
        await response.Content.CopyToAsync(fs);

        Console.DisplayTextLine($"Downloaded       {tempFile}");
        return (tempFile, true);
    }

    // -----------------------------------------------------------------------
    // Main install logic (operates on a guaranteed local file)
    // -----------------------------------------------------------------------

    private async Task<int> InstallLocalAsync(
        string packageFile,
        string tenantIdentifier,
        RegulationInstallMode installMode,
        RegulationDryRun dryRun)
    {
        RegulationPackageManifest manifest;
        try
        {
            manifest = await ReadManifestAsync(packageFile);
        }
        catch (Exception ex)
        {
            Console.DisplayErrorLine($"Cannot read manifest from package: {ex.GetBaseException().Message}");
            return (int)ProgramExitCode.InvalidInput;
        }

        Console.DisplayTextLine($"Package          {manifest.PackageId}");
        Console.DisplayTextLine($"Regulation       {manifest.RegulationName}");
        Console.DisplayTextLine($"Version          {manifest.Version}");
        Console.DisplayTextLine($"Tenant           {tenantIdentifier}");
        Console.DisplayTextLine($"Install mode     {installMode}");
        Console.DisplayTextLine($"Dry run          {dryRun}");
        Console.DisplayNewLine();

        // --- dependency check — always runs, searches across all tenants ---
        var depResult = await CheckDependenciesAsync(manifest);
        if (depResult != 0)
        {
            return depResult;
        }

        // --- resolve tenant (optional — import creates it if absent) ---
        var tenant = await new TenantService(HttpClient).GetAsync<Tenant>(new(), tenantIdentifier);
        if (tenant != null)
        {
            // --- version / existence check (only when tenant already exists) ---
            var versionResult = await CheckVersionAsync(tenant, manifest, installMode);
            if (versionResult != 0)
            {
                return versionResult;
            }
        }
        else
        {
            Console.DisplayTextLine($"Tenant '{tenantIdentifier}' not found — will be created by the import.");
        }

        // --- install files ---
        Console.DisplayTextLine($"Install files    {manifest.InstallFiles.Count}");
        foreach (var file in manifest.InstallFiles)
        {
            Console.DisplayTextLine($"  {file}");
        }
        Console.DisplayNewLine();

        if (dryRun == RegulationDryRun.DryRun)
        {
            Console.DisplayInfoLine("Dry run complete — no changes made.");
            return (int)ProgramExitCode.Ok;
        }

        return await ImportFilesAsync(packageFile, manifest);
    }

    // -----------------------------------------------------------------------
    // Manifest
    // -----------------------------------------------------------------------

    private static async Task<RegulationPackageManifest> ReadManifestAsync(string packageFile)
    {
        var fileStream = new FileStream(packageFile, FileMode.Open, FileAccess.Read, FileShare.Read,
            bufferSize: 4096, useAsync: true);
        await using var zip = new ZipArchive(fileStream, ZipArchiveMode.Read, leaveOpen: false);

        var entry = zip.Entries.FirstOrDefault(e =>
            e.FullName.Equals(RegulationFolder + ManifestFileName, StringComparison.OrdinalIgnoreCase));

        if (entry == null)
        {
            throw new InvalidOperationException(
                $"Manifest '{ManifestFileName}' not found in package under '{RegulationFolder}'. " +
                $"Ensure the .csproj includes regulation-package.json with PackagePath=regulation/.");
        }

        // ReSharper disable once MethodHasAsyncOverload — ZipArchiveEntry.Open() has no async overload in .NET BCL
        await using var stream = entry.Open();
        return await JsonSerializer.DeserializeAsync<RegulationPackageManifest>(stream,
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true })
               ?? throw new InvalidOperationException("Manifest deserialization returned null.");
    }

    // -----------------------------------------------------------------------
    // Version / existence check
    // -----------------------------------------------------------------------

    private async Task<int> CheckVersionAsync(Tenant tenant, RegulationPackageManifest manifest,
        RegulationInstallMode installMode)
    {
        var regulation = await new RegulationService(HttpClient)
            .GetAsync<Regulation>(new(tenant.Id), manifest.RegulationName);

        if (regulation != null)
        {
            if (installMode == RegulationInstallMode.New)
            {
                Console.DisplayErrorLine(
                    $"Regulation '{manifest.RegulationName}' already exists in tenant '{tenant.Identifier}'. " +
                    $"Use /overwrite to replace existing objects.");
                return (int)ProgramExitCode.InvalidInput;
            }

            Console.DisplayTextLine(
                $"Regulation '{manifest.RegulationName}' exists — overwrite mode active.");
        }
        else
        {
            Console.DisplayTextLine(
                $"Regulation '{manifest.RegulationName}' not found — installing fresh.");
        }

        return (int)ProgramExitCode.Ok;
    }

    // -----------------------------------------------------------------------
    // Dependency check
    // -----------------------------------------------------------------------

    private async Task<int> CheckDependenciesAsync(RegulationPackageManifest manifest)
    {
        if (manifest.BaseRegulations.Count == 0)
        {
            return (int)ProgramExitCode.Ok;
        }

        Console.DisplayTextLine("Checking dependencies...");

        // Load all tenants once and build a set of all known regulation names across
        // the entire backend — base regulations may live in different tenants.
        var tenants = await new TenantService(HttpClient).QueryAsync<Tenant>(new());
        var regulationService = new RegulationService(HttpClient);
        var allRegulationNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var t in tenants)
        {
            var regulations = await regulationService.QueryAsync<Regulation>(new(t.Id));
            foreach (var reg in regulations)
            {
                allRegulationNames.Add(reg.Name);
            }
        }

        var missing = new List<string>();
        foreach (var dep in manifest.BaseRegulations)
        {
            if (allRegulationNames.Contains(dep))
            {
                Console.DisplayTextLine($"  Found:   {dep}");
            }
            else
            {
                missing.Add(dep);
                Console.DisplayErrorLine($"  Missing: {dep}");
            }
        }

        if (missing.Count > 0)
        {
            Console.DisplayNewLine();
            Console.DisplayErrorLine(
                $"Dependency check failed — {missing.Count} required regulation(s) not installed: " +
                string.Join(", ", missing));
            return (int)ProgramExitCode.InvalidInput;
        }

        Console.DisplayTextLine("All dependencies satisfied.");
        Console.DisplayNewLine();
        return (int)ProgramExitCode.Ok;
    }

    // -----------------------------------------------------------------------
    // Import
    // -----------------------------------------------------------------------

    private async Task<int> ImportFilesAsync(string packageFile, RegulationPackageManifest manifest)
    {
        var tempDir = Path.Combine(Path.GetTempPath(), $"pe-regulation-{Guid.NewGuid():N}");
        Directory.CreateDirectory(tempDir);

        try
        {
            // Extract all files under regulation/ to tempDir maintaining relative structure
            var zipStream = new FileStream(packageFile, FileMode.Open, FileAccess.Read, FileShare.Read,
                bufferSize: 4096, useAsync: true);
            await using (var zip = new ZipArchive(zipStream, ZipArchiveMode.Read, leaveOpen: false))
            {
                foreach (var entry in zip.Entries)
                {
                    if (!entry.FullName.StartsWith(RegulationFolder, StringComparison.OrdinalIgnoreCase))
                    {
                        continue;
                    }

                    var relativePath = entry.FullName[RegulationFolder.Length..];
                    if (string.IsNullOrEmpty(relativePath) || relativePath.EndsWith('/'))
                    {
                        continue; // directory entry
                    }

                    var destPath = Path.Combine(tempDir,
                        relativePath.Replace('/', Path.DirectorySeparatorChar));
                    Directory.CreateDirectory(Path.GetDirectoryName(destPath)!);

                    // ReSharper disable once MethodHasAsyncOverload — ZipArchiveEntry.Open() has no async overload in .NET BCL
                    await using var entryStream = entry.Open();
                    var destStream = new FileStream(destPath, FileMode.Create, FileAccess.Write,
                        FileShare.None, bufferSize: 4096, useAsync: true);
                    await using (destStream)
                    {
                        await entryStream.CopyToAsync(destStream);
                    }
                }
            }

            // Import each file in manifest order
            var importOptions = new ExchangeImportOptions();
            var previousDir = Directory.GetCurrentDirectory();
            var errorCount = 0;

            foreach (var file in manifest.InstallFiles)
            {
                var filePath = Path.Combine(tempDir,
                    file.Replace('/', Path.DirectorySeparatorChar));

                if (!File.Exists(filePath))
                {
                    Console.DisplayErrorLine($"  Missing file in package: {file}");
                    errorCount++;
                    continue;
                }

                try
                {
                    Directory.SetCurrentDirectory(Path.GetDirectoryName(filePath)!);
                    var exchange = await FileReader.ReadAsync<Exchange>(filePath);
                    var import = new ExchangeImport(HttpClient, exchange, ScriptParser,
                        importOptions, DataImportMode.Bulk);
                    await import.ImportAsync();
                    Console.DisplaySuccessLine($"  Imported: {file}");
                }
                catch (Exception ex)
                {
                    Console.DisplayErrorLine($"  Failed:   {file} — {ex.GetBaseException().Message}");
                    errorCount++;
                }
                finally
                {
                    Directory.SetCurrentDirectory(previousDir);
                }
            }

            if (errorCount > 0)
            {
                Console.DisplayNewLine();
                Console.DisplayErrorLine($"Installation completed with {errorCount} error(s).");
                return (int)ProgramExitCode.GenericError;
            }

            Console.DisplayNewLine();
            Console.DisplaySuccessLine(
                $"Regulation '{manifest.RegulationName}' v{manifest.Version} successfully installed.");
            return (int)ProgramExitCode.Ok;
        }
        finally
        {
            TryDelete(tempDir, recursive: true);
        }
    }

    // -----------------------------------------------------------------------
    // Helpers
    // -----------------------------------------------------------------------

    private static void TryDelete(string path, bool recursive = false)
    {
        try
        {
            if (recursive && Directory.Exists(path))
            {
                Directory.Delete(path, recursive: true);
            }
            else if (File.Exists(path))
            {
                File.Delete(path);
            }
        }
        catch
        {
            // best-effort cleanup
        }
    }
}
