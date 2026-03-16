using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using PayrollEngine.Client.Model;

// ReSharper disable InconsistentNaming

namespace PayrollEngine.PayrollConsole.Commands.LoadTestCommands;

/// <summary>Writes payrun load test results to a Markdown report</summary>
internal sealed class PayrunLoadTestMarkdownWriter
{
    private readonly string path;
    private readonly PayrunLoadTestParameters parameters;
    private readonly List<PayrunLoadTestResult> results;
    private readonly BackendInformation backendInfo;

    internal PayrunLoadTestMarkdownWriter(
        string path,
        PayrunLoadTestParameters parameters,
        List<PayrunLoadTestResult> results,
        BackendInformation backendInfo)
    {
        this.path = path;
        this.parameters = parameters;
        this.results = results;
        this.backendInfo = backendInfo;
    }

    /// <summary>Write the Markdown report</summary>
    internal void Write()
    {
        var sb = new StringBuilder();

        WriteTitle(sb);
        WriteSummary(sb);
        WriteInfrastructure(sb);

        var directory = Path.GetDirectoryName(Path.GetFullPath(path));
        if (!string.IsNullOrEmpty(directory))
        {
            Directory.CreateDirectory(directory);
        }
        File.WriteAllText(path, sb.ToString(), Encoding.UTF8);
    }

    private void WriteTitle(StringBuilder sb)
    {
        sb.AppendLine("# Payrun Load Test Report");
        sb.AppendLine();
    }

    private void WriteSummary(StringBuilder sb)
    {
        sb.AppendLine("## Test Summary");
        sb.AppendLine();

        var periods = results.Select(r => r.Period).Distinct().Count();
        var testDate = results.FirstOrDefault()?.Timestamp.ToString("yyyy-MM-dd HH:mm") ?? "—";

        var runTotals = results
            .GroupBy(r => r.RunNumber)
            .Select(g => (
                ServerTotal: g.Sum(r => r.ServerJobDurationMs),
                EmployeeTotal: g.Sum(r => r.EmployeeCount)))
            .OrderBy(r => r.ServerTotal)
            .ToList();
        var median = runTotals[runTotals.Count / 2];
        var medianAvg = median.EmployeeTotal > 0
            ? median.ServerTotal / (double)median.EmployeeTotal
            : 0;

        sb.AppendLine("| Metric | Value |");
        sb.AppendLine("|:---|:---|");
        sb.AppendLine($"| Test Date | {testDate} |");
        sb.AppendLine($"| Invocation File | `{parameters.InvocationFile}` |");
        sb.AppendLine($"| Periods | {periods} |");
        sb.AppendLine($"| Employee Count | {parameters.EmployeeCount} |");
        sb.AppendLine($"| Repetitions | {parameters.Repetitions} |");
        sb.AppendLine($"| Median Server Total | {median.ServerTotal} ms |");
        sb.AppendLine($"| Median Avg ms/Employee | {medianAvg:F1} |");
        sb.AppendLine();

        // per-run breakdown
        sb.AppendLine("### Run Results");
        sb.AppendLine();
        sb.AppendLine("| Run | Server Total (ms) | Employees | Avg ms/Employee |");
        sb.AppendLine("|:---:|---:|---:|---:|");
        foreach (var rt in results.GroupBy(r => r.RunNumber).OrderBy(g => g.Key))
        {
            var serverTotal = rt.Sum(r => r.ServerJobDurationMs);
            var empTotal = rt.Sum(r => r.EmployeeCount);
            var avg = empTotal > 0 ? serverTotal / (double)empTotal : 0;
            sb.AppendLine($"| {rt.Key} | {serverTotal} | {empTotal} | {avg:F1} |");
        }
        sb.AppendLine();
    }

    private void WriteInfrastructure(StringBuilder sb)
    {
        sb.AppendLine("## Test Infrastructure");
        sb.AppendLine();

        WriteComputerSection(sb);
        WriteConsoleSection(sb);
        WriteBackendSection(sb);
    }

    private static void WriteComputerSection(StringBuilder sb)
    {
        sb.AppendLine("### Computer");
        sb.AppendLine();
        sb.AppendLine("| Property | Value |");
        sb.AppendLine("|:---|:---|");
        sb.AppendLine($"| Machine Name | {Environment.MachineName} |");
        sb.AppendLine($"| OS | {RuntimeInformation.OSDescription} |");
        sb.AppendLine($"| Framework | {RuntimeInformation.FrameworkDescription} |");
        sb.AppendLine($"| CPU Cores | {Environment.ProcessorCount} |");

        // RAM — P/Invoke on Windows, GC fallback on other platforms
        try
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                var (totalGb, availGb) = GetWindowsRamInfo();
                sb.AppendLine($"| RAM Total | {totalGb:F1} GB |");
                sb.AppendLine($"| RAM Available | {availGb:F1} GB |");
            }
            else
            {
                var memInfo = GC.GetGCMemoryInfo();
                var totalGb = memInfo.TotalAvailableMemoryBytes / (1024.0 * 1024 * 1024);
                sb.AppendLine($"| RAM Total | {totalGb:F1} GB |");
                sb.AppendLine("| RAM Available | — |");
            }
        }
        catch
        {
            sb.AppendLine("| RAM Total | — |");
            sb.AppendLine("| RAM Available | — |");
        }

        // Disk (drive of current working directory)
        try
        {
            var drive = new DriveInfo(Path.GetPathRoot(Directory.GetCurrentDirectory()) ?? "C:\\");
            var totalGb = drive.TotalSize / (1024.0 * 1024 * 1024);
            var freeGb = drive.AvailableFreeSpace / (1024.0 * 1024 * 1024);
            sb.AppendLine($"| Disk ({drive.Name}) Total | {totalGb:F0} GB |");
            sb.AppendLine($"| Disk ({drive.Name}) Free | {freeGb:F1} GB |");
        }
        catch
        {
            sb.AppendLine("| Disk | — |");
        }

        sb.AppendLine();
    }

    private static void WriteConsoleSection(StringBuilder sb)
    {
        sb.AppendLine("### Console");
        sb.AppendLine();
        sb.AppendLine("| Property | Value |");
        sb.AppendLine("|:---|:---|");

        var assembly = Assembly.GetEntryAssembly() ?? Assembly.GetExecutingAssembly();
        var version = assembly
            .GetCustomAttribute<AssemblyInformationalVersionAttribute>()
            ?.InformationalVersion ?? "unknown";
        var buildDate = File.Exists(assembly.Location)
            ? File.GetLastWriteTimeUtc(assembly.Location).ToString("yyyy-MM-dd")
            : "—";

        sb.AppendLine($"| Version | {version} |");
        sb.AppendLine($"| Build Date | {buildDate} |");
        sb.AppendLine();
    }

    private void WriteBackendSection(StringBuilder sb)
    {
        sb.AppendLine("### Backend");
        sb.AppendLine();

        if (backendInfo == null)
        {
            sb.AppendLine("*Backend information not available.*");
            sb.AppendLine();
            return;
        }

        sb.AppendLine("| Property | Value |");
        sb.AppendLine("|:---|:---|");
        sb.AppendLine($"| Version | {backendInfo.Version} |");
        sb.AppendLine($"| Build Date | {backendInfo.BuildDate:yyyy-MM-dd} |");
        sb.AppendLine($"| API Version | {backendInfo.ApiVersion} |");

        if (backendInfo.Authentication != null)
        {
            sb.AppendLine($"| Auth Mode | {backendInfo.Authentication.Mode} |");
        }

        if (backendInfo.Runtime != null)
        {
            sb.AppendLine($"| Max Parallel Employees | {backendInfo.Runtime.MaxParallelEmployees} |");
            sb.AppendLine($"| Max Retro Periods | {(backendInfo.Runtime.MaxRetroPayrunPeriods == 0 ? "unlimited" : backendInfo.Runtime.MaxRetroPayrunPeriods.ToString())} |");
            sb.AppendLine($"| DB Command Timeout | {backendInfo.Runtime.DbCommandTimeoutSeconds} s |");
            sb.AppendLine($"| DB Transaction Timeout | {backendInfo.Runtime.DbTransactionTimeoutSeconds} s |");
            sb.AppendLine($"| Assembly Cache Timeout | {backendInfo.Runtime.AssemblyCacheTimeoutSeconds} s |");
            sb.AppendLine($"| Webhook Timeout | {backendInfo.Runtime.WebhookTimeoutSeconds} s |");
            sb.AppendLine($"| Script Safety Analysis | {backendInfo.Runtime.ScriptSafetyAnalysis} |");
        }

        if (backendInfo.Database != null)
        {
            sb.AppendLine();
            sb.AppendLine("#### Database");
            sb.AppendLine();
            sb.AppendLine("| Property | Value |");
            sb.AppendLine("|:---|:---|");
            sb.AppendLine($"| Type | {backendInfo.Database.Type} |");
            sb.AppendLine($"| Name | {backendInfo.Database.Name} |");
            sb.AppendLine($"| Version | {backendInfo.Database.Version} |");
        }

        if (backendInfo.Runtime?.AuditTrail != null)
        {
            var at = backendInfo.Runtime.AuditTrail;
            sb.AppendLine();
            sb.AppendLine("#### Audit Trail");
            sb.AppendLine();
            sb.AppendLine("| Area | Enabled |");
            sb.AppendLine("|:---|:---:|");
            sb.AppendLine($"| Script | {at.Script} |");
            sb.AppendLine($"| Lookup | {at.Lookup} |");
            sb.AppendLine($"| Input | {at.Input} |");
            sb.AppendLine($"| Payrun | {at.Payrun} |");
            sb.AppendLine($"| Report | {at.Report} |");
        }

        if (backendInfo.Runtime?.Cors is { IsActive: true })
        {
            sb.AppendLine();
            sb.AppendLine("#### CORS");
            sb.AppendLine();
            sb.AppendLine("| Allowed Origin |");
            sb.AppendLine("|:---|");
            foreach (var origin in backendInfo.Runtime.Cors.AllowedOrigins ?? [])
            {
                sb.AppendLine($"| {origin} |");
            }
        }

        if (backendInfo.Runtime?.RateLimiting is { IsActive: true })
        {
            var rl = backendInfo.Runtime.RateLimiting;
            sb.AppendLine();
            sb.AppendLine("#### Rate Limiting");
            sb.AppendLine();
            sb.AppendLine("| Policy | Permit Limit | Window (s) |");
            sb.AppendLine("|:---|---:|---:|");
            if (rl.GlobalPermitLimit > 0)
            {
                sb.AppendLine($"| Global | {rl.GlobalPermitLimit} | {rl.GlobalWindowSeconds} |");
            }
            if (rl.PayrunJobStartPermitLimit > 0)
            {
                sb.AppendLine($"| Payrun Job Start | {rl.PayrunJobStartPermitLimit} | {rl.PayrunJobStartWindowSeconds} |");
            }
        }

        sb.AppendLine();
    }

    #region Windows Memory P/Invoke

    [StructLayout(LayoutKind.Sequential)]
    private struct MEMORYSTATUSEX
    {
        internal uint dwLength;
        internal uint dwMemoryLoad;
        internal ulong ullTotalPhys;
        internal ulong ullAvailPhys;
        internal ulong ullTotalPageFile;
        internal ulong ullAvailPageFile;
        internal ulong ullTotalVirtual;
        internal ulong ullAvailVirtual;
        internal ulong ullAvailExtendedVirtual;
    }

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern bool GlobalMemoryStatusEx(ref MEMORYSTATUSEX lpBuffer);

    /// <summary>Returns (totalGb, availableGb) via GlobalMemoryStatusEx</summary>
    private static (double totalGb, double availGb) GetWindowsRamInfo()
    {
        var ms = new MEMORYSTATUSEX { dwLength = (uint)Marshal.SizeOf<MEMORYSTATUSEX>() };
        if (!GlobalMemoryStatusEx(ref ms))
            return (0, 0);
        const double gb = 1024.0 * 1024 * 1024;
        return (ms.ullTotalPhys / gb, ms.ullAvailPhys / gb);
    }

    #endregion
}
