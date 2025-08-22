using PayrollEngine.Client.Command;

namespace PayrollEngine.PayrollConsole.Commands.PayrunCommands;

/// <summary>
/// Payrun test command base
/// </summary>
/// <typeparam name="TArgs">Command argument type</typeparam>
internal abstract class PayrunTestCommandBase<TArgs> : PayrunTestCommandBase where TArgs : ICommandParameters
{
    protected abstract System.Threading.Tasks.Task<int> Execute(CommandContext context, TArgs parameters);
    protected override async System.Threading.Tasks.Task<int> OnExecute(CommandContext context, ICommandParameters parameters) =>
        await Execute(context, (TArgs)parameters);
}