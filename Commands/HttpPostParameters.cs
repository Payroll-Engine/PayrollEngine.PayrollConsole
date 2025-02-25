using System;
using PayrollEngine.Client.Command;

namespace PayrollEngine.PayrollConsole.Commands;

/// <summary>
/// Http post command parameters
/// </summary>
public class HttpPostParameters : ICommandParameters
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
    public static HttpPostParameters ParserFrom(CommandLineParser parser) =>
        new()
        {
            Url = parser.Get(2, nameof(Url)),
            FileName = parser.Get(3, nameof(FileName))
        };
}