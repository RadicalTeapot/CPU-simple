using Backend.CpuStates;
using CPU.opcodes;

namespace Backend.Commands.StateCommands
{
    [Command(CommandType.State, "stepover", ["so"],
        description: "Step over the current instruction (steps into non-call instructions, runs to return address for calls).",
        helpText: "Usage: 'stepover'")]
    [ValidInState([typeof(IdleState), typeof(RunningState)])]
    internal class StepOver(CommandContext context) : BaseStateCommand(context)
    {
        protected override StateCommandResult ExecuteCore(CpuStateFactory stateFactory, string[] args)
        {
            var inspector = stateFactory.GetInspector();
            var currentOpcode = inspector.MemoryContents[inspector.PC];

            if (currentOpcode == (byte)OpcodeBaseCode.CAL)
            {
                var nextAddress = inspector.PC + 1 + CPU.CPU.AddressSize;
                return new StateCommandResult(
                    Success: true,
                    NextState: stateFactory.CreateRunningState(new Run.Config(Run.Mode.ToAddress, nextAddress)),
                    Message: $"Stepping over call, will stop at address 0x{nextAddress:X4}"
                );
            }

            return new StateCommandResult(
                Success: true,
                NextState: stateFactory.CreateSteppingState(1),
                Message: "Stepping one instruction"
            );
        }
    }
}
