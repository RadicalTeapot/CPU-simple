using CPU.components;
using CPU.opcodes;
using System.Diagnostics;

namespace CPU.microcode
{
    internal record TickHandlerConfig(
        State State,
        IBus Bus,
        Stack Stack,
        OpcodeFactory OpcodeFactory,
        int IrqVectorAddress
    ) { }

    internal class TickHandler
    {
        public TickHandler(TickHandlerConfig context)
        {
            _state = context.State;
            _bus = context.Bus;
            _stack = context.Stack;
            _opcodeFactory = context.OpcodeFactory;
            _irqVectorAddress = context.IrqVectorAddress;
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
            if (_pendingInterrupt && !_state.I)
            {
                JumpToInterrupt();
                return;
            }

            _phaseCount = 0;

            var instruction = _bus.ReadByte(_state.GetPC());
            _currentBaseCode = _opcodeFactory.GetOpcodeBaseCodeFromInstruction(instruction);
            _currentOpcode = _opcodeFactory.CreateOpcode(instruction, _state, _bus, _stack);

            _state.IncrementPC();
        }

        private void JumpToInterrupt()
        {
            _pendingInterrupt = false;
            _phaseCount = 0;
            _currentBaseCode = OpcodeBaseCode.NOP;
            _currentOpcode = new InterruptServiceRoutine(_state, _stack, _irqVectorAddress);
            _currentPhase = _currentOpcode.GetStartPhaseType();
        }

        private MicroPhase _currentPhase = MicroPhase.FetchOpcode;
        private uint _phaseCount = 0;
        private ulong _tickCounter = 0;
        private bool _pendingInterrupt = false;
        private OpcodeBaseCode _currentBaseCode = OpcodeBaseCode.NOP;
        private IOpcode? _currentOpcode = null;
        private readonly State _state;
        private readonly IBus _bus;
        private readonly Stack _stack;
        private readonly OpcodeFactory _opcodeFactory;
        private readonly int _irqVectorAddress;
    }
}
