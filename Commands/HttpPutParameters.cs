using System;
using PayrollEngine.Client.Command;

namespace PayrollEngine.PayrollConsole.Commands;

public class HttpPutParameters : ICommandParameters
{
    public string Url { get; set; }
    public string FileName { get; init; }
    public Type[] Toggles => null;

    public string Test() =>
        string.IsNullOrWhiteSpace(Url) ? "Missing http url" : null;

    public static HttpPutParameters ParserFrom(CommandLineParser parser) =>
        new()
        {
            Url = parser.Get(2, nameof(Url)),
            FileName = parser.Get(3, nameof(FileName))
        };
}