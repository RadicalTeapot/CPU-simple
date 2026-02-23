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
            _registersBefore = new byte[_state.RegisterCount];
            _busRecorder = new BusRecorder();
            _memory.Recorder = _busRecorder;
            _stack.Recorder = _busRecorder;
        }

        public void RequestInterrupt()
        {
            _pendingInterrupt = true;
        }

        public MicrocodeTickResult Tick()
        {
            _tickCounter++;
            _busRecorder.Clear();

            var pcBefore = (int)_state.GetPC();
            var spBefore = (int)_stack.SP;
            var zBefore = _state.Z;
            var cBefore = _state.C;
            SnapshotRegisters();

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

            var trace = new TickTrace(
                TickNumber: _tickCounter,
                Type: ClassifyPhase(executedPhase),
                Phase: executedPhase,
                PcBefore: pcBefore,
                PcAfter: _state.GetPC(),
                SpBefore: spBefore,
                SpAfter: _stack.SP,
                Instruction: _currentBaseCode.ToString(),
                RegisterChanges: DiffRegisters(),
                ZeroFlagBefore: zBefore,
                ZeroFlagAfter: _state.Z,
                CarryFlagBefore: cBefore,
                CarryFlagAfter: _state.C,
                Bus: _busRecorder.LastAccess
            );

            return new MicrocodeTickResult(_tickCounter, _currentPhase, _phaseCount, _currentBaseCode, isInstructionComplete, trace);
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

        private void SnapshotRegisters()
        {
            for (int i = 0; i < _state.RegisterCount; i++)
            {
                _registersBefore[i] = _state.GetRegister(i);
            }
        }

        private RegisterChange[] DiffRegisters()
        {
            var changes = new List<RegisterChange>();
            for (int i = 0; i < _state.RegisterCount; i++)
            {
                var current = _state.GetRegister(i);
                if (_registersBefore[i] != current)
                {
                    changes.Add(new RegisterChange(i, _registersBefore[i], current));
                }
            }
            return [.. changes];
        }

        private static TickType ClassifyPhase(MicroPhase phase) => phase switch
        {
            MicroPhase.FetchOpcode => TickType.Bus,
            MicroPhase.FetchOperand => TickType.Bus,
            MicroPhase.FetchOperand16Low => TickType.Bus,
            MicroPhase.FetchOperand16High => TickType.Bus,
            MicroPhase.MemoryRead => TickType.Bus,
            MicroPhase.MemoryWrite => TickType.Bus,
            _ => TickType.Internal,
        };

        private MicroPhase _currentPhase = MicroPhase.FetchOpcode;
        private uint _phaseCount = 0;
        private ulong _tickCounter = 0;
        private bool _pendingInterrupt = false;
        private OpcodeBaseCode _currentBaseCode = OpcodeBaseCode.NOP;
        private IOpcode? _currentOpcode = null;
        private readonly BusRecorder _busRecorder;
        private readonly byte[] _registersBefore;
        private readonly State _state;
        private readonly Memory _memory;
        private readonly Stack _stack;
        private readonly OpcodeFactory _opcodeFactory;
    }
}
