using System;
using PayrollEngine.Client.Command;

namespace PayrollEngine.PayrollConsole.Commands.RegulationCommands;

/// <summary>
/// LexSnapshot command parameters
/// </summary>
public class LexSnapshotParameters : ICommandParameters
{
    /// <summary>
    /// File name or file mask (e.g. lex.json or *.json)
    /// </summary>
    public string FileMask { get; init; }

    /// <summary>
    /// Output directory for snapshot.json and copied files (default: directory of lex.json)
    /// </summary>
    public string TargetPath { get; init; }

    /// <summary>
    /// Dry-run mode: validate and report without writing anything (default: Execute)
    /// </summary>
    public LexSnapshotMode SnapshotMode { get; private init; } = LexSnapshotMode.Execute;

    /// <summary>
    /// Directory scan mode: top-level only or recursive (default: Top)
    /// </summary>
    public DirectoryMode DirectoryMode { get; private init; } = DirectoryMode.Top;

    /// <summary>
    /// Existing snapshot.json: skip or regenerate (default: Skip)
    /// </summary>
    public LexOverwriteMode OverwriteMode { get; private init; } = LexOverwriteMode.Skip;

    /// <summary>
    /// Print JSON Schema for lex.json and exit
    /// </summary>
    public LexSchemaMode SchemaMode { get; private init; } = LexSchemaMode.NoSchema;

    /// <inheritdoc />
    public Type[] Toggles =>
    [
        typeof(LexSnapshotMode),
        typeof(DirectoryMode),
        typeof(LexOverwriteMode),
        typeof(LexSchemaMode)
    ];

    /// <inheritdoc />
    public string Test()
    {
        if (SchemaMode == LexSchemaMode.Schema)
        {
            return null;
        }
        return string.IsNullOrWhiteSpace(FileMask) ? "Missing file mask (e.g. lex.json)" : null;
    }

    /// <summary>
    /// Parse command parameters
    /// </summary>
    public static LexSnapshotParameters ParserFrom(CommandLineParser parser) =>
        new()
        {
            FileMask = parser.Get(2, nameof(FileMask)),
            TargetPath = parser.GetByName(nameof(TargetPath)),
            SnapshotMode = parser.GetEnumToggle(LexSnapshotMode.Execute),
            DirectoryMode = parser.GetEnumToggle(DirectoryMode.Top),
            OverwriteMode = parser.GetEnumToggle(LexOverwriteMode.Skip),
            SchemaMode = parser.GetEnumToggle(LexSchemaMode.NoSchema)
        };
}
