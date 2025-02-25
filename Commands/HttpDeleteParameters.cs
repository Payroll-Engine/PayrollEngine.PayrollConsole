using System;
using PayrollEngine.Client.Command;

namespace PayrollEngine.PayrollConsole.Commands;

/// <summary>
/// Http delete command parameters
/// </summary>
public class HttpDeleteParameters : ICommandParameters
{
    /// <summary>
    /// Url
    /// </summary>
    public string Url { get; set; }
  
    /// <inheritdoc />
    public Type[] Toggles => null;

    /// <inheritdoc />
    public string Test() =>
        string.IsNullOrWhiteSpace(Url) ? "Missing http url" : null;

    /// <summary>
    /// Parse command parameters
    /// </summary>
    /// <param name="parser">Parameter parser</param>
    public static HttpDeleteParameters ParserFrom(CommandLineParser parser) =>
        new()
        {
            Url = parser.Get(2, nameof(Url))
        };
}