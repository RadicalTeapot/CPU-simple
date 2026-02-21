using CPU.components;
using CPU.opcodes;
using System.Diagnostics;

namespace CPU.microcode
{
    internal record TickHandlerConfig(
        State State,
        Memory Memory,
        Stack Stack,
        OpcodeFactory OpcodeFactory
    ) { }

    internal class TickHandler(TickHandlerConfig context)
    {
        public void RequestInterrupt()
        {
            _pendingInterrupt = true;
        }

        public MicrocodeTickResult Tick()
        {
            _tickCounter++;
            var isInstructionComplete = false;
            if (_currentOpcode != null)
            {
                TickCurrentInstruction();
                isInstructionComplete = _currentPhase == MicroPhase.Done;
            }

            // If the current phase is Done, it means the instruction has completed its execution and we need to fetch the next instruction.
            // Note: When testing, this means that a bunch of NOPs should keep the _currentPhase at FetchOp8
            if (_currentPhase == MicroPhase.Done)
            {
                FetchCurrentInstruction();
            }

            return new MicrocodeTickResult(_tickCounter, _currentPhase, _phaseCount, _currentBaseCode, isInstructionComplete);
        }

        private void FetchCurrentInstruction()
        {
            if (_pendingInterrupt)
            {
                JumpToInterrupt();
                return;
            }

            _currentPhase = MicroPhase.FetchOp;
            _phaseCount = 0;

            var instruction = _memory.ReadByte(_state.GetPC());
            _currentBaseCode = _opcodeFactory.GetOpcodeBaseCodeFromInstruction(instruction);
            _currentOpcode = _opcodeFactory.CreateOpcode(instruction, _state, _memory, _stack);
            
            _state.IncrementPC();
        }

        private void JumpToInterrupt()
        {
            _pendingInterrupt = false;
            _currentPhase = MicroPhase.JumpToInterrupt;
            // Implementation of jumping to the interrupt handler would go here.
        }

        private void TickCurrentInstruction()
        {
            Debug.Assert(_currentOpcode != null, "Current opcode should not be null when ticking an instruction.");
            _currentPhase = _currentOpcode.Tick(_phaseCount);
            _phaseCount++;
        }

        private MicroPhase _currentPhase = MicroPhase.Done;
        private int _phaseCount = 0;
        private ulong _tickCounter = 0;
        private bool _pendingInterrupt = false;
        private OpcodeBaseCode _currentBaseCode = OpcodeBaseCode.NOP;
        private IOpcode? _currentOpcode = null;

        // Convenience references to context properties
        private readonly State _state = context.State;
        private readonly Memory _memory = context.Memory;
        private readonly Stack _stack = context.Stack;
        private readonly OpcodeFactory _opcodeFactory = context.OpcodeFactory;
    }
}
