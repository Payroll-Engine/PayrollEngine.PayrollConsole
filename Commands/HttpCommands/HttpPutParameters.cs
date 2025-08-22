using System;
using PayrollEngine.Client.Command;

namespace PayrollEngine.PayrollConsole.Commands.HttpCommands;

/// <summary>
/// Http put command parameters
/// </summary>
public class HttpPutParameters : ICommandParameters
{
    /// <summary>
    /// Url
    /// </summary>
    public string Url { get; set; }

    /// <summary>
    /// File name
    /// </summary>
    public string FileName { get; init; }

    /// <inheritdoc />
    public Type[] Toggles => null;

    /// <inheritdoc />
    public string Test() =>
        string.IsNullOrWhiteSpace(Url) ? "Missing http url" : null;

    /// <summary>
    /// Parse command parameters
    /// </summary>
    /// <param name="parser">Parameter parser</param>
    public static HttpPutParameters ParserFrom(CommandLineParser parser) =>
        new()
        {
            Url = parser.Get(2, nameof(Url)),
            FileName = parser.Get(3, nameof(FileName))
        };
}