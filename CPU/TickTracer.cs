using CPU.components;
using CPU.microcode;

namespace CPU
{
    internal class TickTracer
    {
        public TickTrace[] LastTraces => [.. _traces];

        public TickTracer(State state, Stack stack, Memory memory)
        {
            _state = state;
            _stack = stack;
            _memory = memory;
            _registersBefore = new byte[_state.RegisterCount];
            _busRecorder = new BusRecorder();
            _memory.Recorder = _busRecorder;
            _stack.Recorder = _busRecorder;
        }

        public void Prepare()
        {
            _busRecorder.Clear();
            _pcBefore = (int)_state.GetPC();
            _spBefore = (int)_stack.SP;
            _zBefore = _state.Z;
            _cBefore = _state.C;
            SnapshotRegisters();
        }

        public void Record(MicrocodeTickResult result)
        {
            var trace = new TickTrace(
                TickNumber: result.TickCount,
                Type: ClassifyPhase(result.ExecutedPhase),
                NextPhase: result.NextPhase,
                PcBefore: _pcBefore,
                PcAfter: _state.GetPC(),
                SpBefore: _spBefore,
                SpAfter: _stack.SP,
                Instruction: result.CurrentOpcode.ToString(),
                RegisterChanges: DiffRegisters(),
                ZeroFlagBefore: _zBefore,
                ZeroFlagAfter: _state.Z,
                CarryFlagBefore: _cBefore,
                CarryFlagAfter: _state.C,
                Bus: _busRecorder.LastAccess
            );
            _traces.Add(trace);
        }

        public void Clear()
        {
            _traces.Clear();
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

        private int _pcBefore;
        private int _spBefore;
        private bool _zBefore;
        private bool _cBefore;
        private readonly List<TickTrace> _traces = [];
        private readonly byte[] _registersBefore;
        private readonly BusRecorder _busRecorder;
        private readonly State _state;
        private readonly Stack _stack;
        private readonly Memory _memory;
    }
}
