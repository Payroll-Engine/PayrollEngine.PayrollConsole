using System;
using PayrollEngine.Client;

namespace PayrollEngine.PayrollConsole.Command;

internal abstract class HttpCommandBase : CommandBase
{
    internal PayrollHttpClient HttpClient { get; }

    protected HttpCommandBase(PayrollHttpClient httpClient)
    {
        HttpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
    }
}