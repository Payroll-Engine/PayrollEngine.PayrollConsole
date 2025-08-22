using System.Threading.Tasks;
using PayrollEngine.Client.Command;

namespace PayrollEngine.PayrollConsole.Commands;

/// <summary>
/// Test command base
/// </summary>
/// <typeparam name="TArgs">Command argument type</typeparam>
internal abstract class TestCommandBase<TArgs> :
    TestCommandBase where TArgs : ICommandParameters
{
    protected abstract Task<int> Execute(CommandContext context, TArgs parameters);
    protected override async Task<int> OnExecute(CommandContext context, ICommandParameters parameters) =>
        await Execute(context, (TArgs)parameters);
}