using System;

namespace PayrollEngine.PayrollConsole.Commands.LoadTestCommands;

/// <summary>Point-in-time system resource snapshot</summary>
internal sealed record SystemSnapshot
{
    /// <summary>Snapshot timestamp</summary>
    internal DateTimeOffset Timestamp { get; init; }

    /// <summary>Disk free space in GB (drive of working directory)</summary>
    internal double DiskFreeGb { get; init; }

    /// <summary>Available physical RAM in GB (Windows only, 0 on other platforms)</summary>
    internal double RamAvailableGb { get; init; }
}
