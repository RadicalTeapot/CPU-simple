using Backend.CpuStates;

namespace Backend.Commands.StateCommands
{
    [Command(CommandType.State, "stepout", ["sout"],
        description: "Step out of the current subroutine by running to the return address on the stack.",
        helpText: "Usage: 'stepout'")]
    [ValidInState([typeof(IdleState), typeof(RunningState)])]
    internal class StepOut(CommandContext context) : BaseStateCommand(context)
    {
        protected override StateCommandResult ExecuteCore(CpuStateFactory stateFactory, string[] args)
        {
            var inspector = stateFactory.GetInspector();

            // SP at initial position means stack is empty — no return address to step out to
            if (inspector.SP == inspector.StackContents.Length - 1)
            {
                return new StateCommandResult(
                    Success: false,
                    Message: "Cannot step out: stack is empty (no return address)."
                );
            }

#if x16
            int returnAddress = inspector.StackContents[inspector.SP + 1]
                              | (inspector.StackContents[inspector.SP + 2] << 8);
#else
            int returnAddress = inspector.StackContents[inspector.SP + 1];
#endif

            return new StateCommandResult(
                Success: true,
                NextState: stateFactory.CreateRunningState(
                    new Run.Config(Run.Mode.ToAddress, returnAddress)),
                Message: $"Stepping out, will stop at return address 0x{returnAddress:X4}"
            );
        }
    }
}
