using System.Threading.Tasks;

namespace PayrollEngine.PayrollConsole.Script;

internal interface IScriptPublisher
{
    public Task<bool> PublishAsync(PublishContext context);
}