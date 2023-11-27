using System;
using PayrollEngine.Client;

namespace PayrollEngine.PayrollConsole.Command;

internal abstract class HttpCommandBase(PayrollHttpClient httpClient) : CommandBase
{
    internal PayrollHttpClient HttpClient { get; } = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
}