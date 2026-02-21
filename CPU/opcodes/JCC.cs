using CPU.components;
using CPU.microcode;

namespace CPU.opcodes
{
    [Opcode(OpcodeBaseCode.JCC, OpcodeGroupBaseCode.SystemAndJump)]
    internal class JCC : BaseOpcode
    {
        public JCC(byte instructionByte, State state, Memory memory, Stack stack)
        {
            _state = state;
            _memory = memory;
#if x16
            SetPhases(MicroPhase.MemoryRead, Read1, Read2);
#else
            SetPhases(MicroPhase.MemoryRead, Read1);
#endif
        }

        private MicroPhase Read1()
        {
#if x16
            _addressLow = _memory.ReadByte(_state.GetPC());
            _state.IncrementPC();
            return MicroPhase.MemoryRead;
#else
            var address = _memory.ReadByte(_state.GetPC());
            _state.IncrementPC();
            if (!_state.C)
                _state.SetPC(address);
            return MicroPhase.Done;
#endif
        }

#if x16
        private MicroPhase Read2()
        {
            var addressHigh = _memory.ReadByte(_state.GetPC());
            var address = ByteConversionHelper.ToUShort(addressHigh, _addressLow);
            _state.IncrementPC();
            if (!_state.C)
                _state.SetPC(address);
            return MicroPhase.Done;
        }

        private byte _addressLow;
#endif

        private readonly State _state;
        private readonly Memory _memory;
    }
}
