using System.Threading.Tasks;

namespace PayrollEngine.PayrollConsole.Commands.Script;

internal interface IScriptPublisher
{
    internal Task<bool> PublishAsync(PublishContext context);
}