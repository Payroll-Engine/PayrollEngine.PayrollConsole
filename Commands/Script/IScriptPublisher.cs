using System.Threading.Tasks;

namespace PayrollEngine.PayrollConsole.Commands.Script;

internal interface IScriptPublisher
{
    public Task<bool> PublishAsync(PublishContext context);
}