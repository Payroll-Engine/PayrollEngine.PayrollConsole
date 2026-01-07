using System;
using PayrollEngine.Client.Command;
using PayrollEngine.Client.Exchange;

namespace PayrollEngine.PayrollConsole.Commands.RegulationCommands;

/// <summary>
/// Lookup  text import parameters
/// </summary>
public class LookupTextImportParameters : ICommandParameters
{
    /// <summary>
    /// Tenant
    /// </summary>
    public string Tenant { get; init; }

    /// <summary>
    /// Regulation
    /// </summary>
    public string Regulation { get; init; }

    /// <summary>
    /// Source file name
    /// </summary>
    public string SourceFileName { get; init; }

    /// <summary>
    /// Mapping file name
    /// </summary>
    public string MappingFileName { get; init; }

    /// <summary>
    /// Target output folder
    /// </summary>
    public string TargetFolder { get; init; }

    /// <summary>
    /// Tax slice size
    /// </summary>
    public int SliceSize { get; init; }

    /// <summary>
    /// Import mode (default: single)
    /// </summary>
    public DataImportMode ImportMode { get; private init; } = DataImportMode.Single;

    /// <summary>
    /// Import target (default: backend)
    /// </summary>
    public ImportMode ImportTarget { get; private init; } = Commands.ImportMode.Backend;

    /// <inheritdoc />
    public Type[] Toggles =>
    [
        typeof(DataImportMode),
        typeof(ImportMode)
    ];

    /// <inheritdoc />
    public string Test()
    {
        if (string.IsNullOrWhiteSpace(Tenant))
        {
            return "Missing tenant";
        }
        if (string.IsNullOrWhiteSpace(Regulation))
        {
            return "Missing regulation";
        }
        if (string.IsNullOrWhiteSpace(SourceFileName))
        {
            return "Missing source file name";
        }
        if (string.IsNullOrWhiteSpace(MappingFileName))
        {
            return "Missing mapping file name";
        }
        return null;
    }

    /// <summary>
    /// Parse command parameters
    /// </summary>
    /// <param name="parser">Parameter parser</param>
    public static LookupTextImportParameters ParserFrom(CommandLineParser parser) =>
        new()
        {
            Tenant = parser.Get(2, nameof(Tenant)),
            Regulation = parser.Get(3, nameof(Regulation)),
            SourceFileName = parser.Get(4, nameof(SourceFileName)),
            MappingFileName = parser.Get(5, nameof(MappingFileName)),
            TargetFolder = parser.Get(6, nameof(TargetFolder)),
            SliceSize = parser.GetInt(7, 0),
            ImportMode = parser.GetEnumToggle(DataImportMode.Single),
            ImportTarget = parser.GetEnumToggle(Commands.ImportMode.Backend)
        };
}