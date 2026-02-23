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

    internal class TickHandler
    {
        public TickHandler(TickHandlerConfig context)
        {
            _state = context.State;
            _memory = context.Memory;
            _stack = context.Stack;
            _opcodeFactory = context.OpcodeFactory;
        }

        public void RequestInterrupt()
        {
            _pendingInterrupt = true;
        }

        public MicrocodeTickResult Tick()
        {
            _tickCounter++;

            var executedPhase = _currentPhase;
            var isInstructionComplete = false;

            if (_currentPhase == MicroPhase.FetchOpcode)
            {
                FetchCurrentInstruction();

                var nextPhaseType = _currentOpcode!.GetStartPhaseType();
                if (nextPhaseType == MicroPhase.Done)
                {
                    // Zero-execute-tick instruction (NOP, MOV, CLC, etc.)
                    _currentOpcode.Tick(0);
                    isInstructionComplete = true;
                    _currentPhase = MicroPhase.FetchOpcode;
                }
                else
                {
                    _currentPhase = nextPhaseType;
                }
            }
            else
            {
                Debug.Assert(_currentOpcode != null, "Current opcode should not be null when executing a phase.");
                var nextPhase = _currentOpcode.Tick(_phaseCount);
                _phaseCount++;

                if (nextPhase == MicroPhase.Done)
                {
                    isInstructionComplete = true;
                    _currentPhase = MicroPhase.FetchOpcode;
                }
                else
                {
                    _currentPhase = nextPhase;
                }
            }

            return new MicrocodeTickResult(_tickCounter, executedPhase, _currentPhase, _phaseCount, _currentBaseCode, isInstructionComplete);
        }

        private void FetchCurrentInstruction()
        {
            if (_pendingInterrupt)
            {
                JumpToInterrupt();
                return;
            }

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
        }

        private MicroPhase _currentPhase = MicroPhase.FetchOpcode;
        private uint _phaseCount = 0;
        private ulong _tickCounter = 0;
        private bool _pendingInterrupt = false;
        private OpcodeBaseCode _currentBaseCode = OpcodeBaseCode.NOP;
        private IOpcode? _currentOpcode = null;
        private readonly State _state;
        private readonly Memory _memory;
        private readonly Stack _stack;
        private readonly OpcodeFactory _opcodeFactory;
    }
}
