using CPU.components;
using CPU.microcode;

namespace CPU.opcodes
{
    [Opcode(OpcodeBaseCode.CAL, OpcodeGroupBaseCode.SystemAndJump)]
    internal class CAL : BaseOpcode
    {
        public CAL(byte instructionByte, State state, Memory memory, Stack stack)
        {
            _state = state;
            _memory = memory;
            _stack = stack;
#if x16
            SetPhases(MicroPhase.MemoryRead, Read1, Read2, Push1, Push2);
#else
            SetPhases(MicroPhase.MemoryRead, Read1, Push1);
#endif
        }

        private MicroPhase Read1()
        {
#if x16
            _addressLow = _memory.ReadByte(_state.GetPC());
            _state.IncrementPC();
            return MicroPhase.MemoryRead;
#else
            _address = _memory.ReadByte(_state.GetPC());
            _state.IncrementPC();
            return MicroPhase.MemoryWrite;
#endif
        }

#if x16
        private MicroPhase Read2()
        {
            var addressHigh = _memory.ReadByte(_state.GetPC());
            _address = ByteConversionHelper.ToUShort(addressHigh, _addressLow);
            _state.IncrementPC();
            return MicroPhase.MemoryWrite;
        }
#endif


        private MicroPhase Push1()
        {
#if x16
            var returnAddress = _state.GetPC();
            _stack.PushByte((byte)(returnAddress >> 8)); // high byte pushed first
            return MicroPhase.MemoryWrite;
#else
            _stack.PushByte(_state.GetPC());
            _state.SetPC(_address);
            return MicroPhase.Done;
#endif
        }

#if x16
        private MicroPhase Push2()
        {
            var returnAddress = _state.GetPC();
            _stack.PushByte((byte)(returnAddress & 0xFF)); // low byte pushed last, popped first by RET
            _state.SetPC(_address);
            return MicroPhase.Done;
        }
#endif

#if x16
        private byte _addressLow;
        private ushort _address;
#else
        private byte _address;
#endif
        private readonly State _state;
        private readonly Memory _memory;
        private readonly Stack _stack;
    }
}
