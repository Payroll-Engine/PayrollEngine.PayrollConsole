using System;
using PayrollEngine.Client.Command;

namespace PayrollEngine.PayrollConsole.Commands;

public class HttpDeleteParameters : ICommandParameters
{
    public string Url { get; set; }
    public Type[] Toggles => null;

    public string Test() =>
        string.IsNullOrWhiteSpace(Url) ? "Missing http url" : null;

    public static HttpDeleteParameters ParserFrom(CommandLineParser parser) =>
        new()
        {
            Url = parser.Get(2, nameof(Url))
        };
}