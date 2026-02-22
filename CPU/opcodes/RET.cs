using CPU.components;
using CPU.microcode;

namespace CPU.opcodes
{
    [Opcode(OpcodeBaseCode.RET, OpcodeGroupBaseCode.SystemAndJump)]
    internal class RET : BaseOpcode
    {
        public RET(byte instructionByte, State state, Memory memory, Stack stack)
        {
            _state = state;
            _stack = stack;
#if x16
            SetPhases(MicroPhase.MemoryRead, Read1, Read2);
#else
            SetPhases(MicroPhase.MemoryRead, Read1);
#endif
        }

        public MicroPhase Read1()
        {
#if x16
            _returnAddressLow = _stack.PopByte();
            return MicroPhase.MemoryRead;
#else
            var returnAddress = _stack.PopByte();
            _state.SetPC(returnAddress);
            return MicroPhase.Done;
#endif
        }

#if x16
        public MicroPhase Read2()
        {
            var returnAddressHigh = _stack.PopByte();
            var returnAddress = ByteConversionHelper.ToUShort(returnAddressHigh, _returnAddressLow);
            _state.SetPC(returnAddress);
            return MicroPhase.Done;
        }

        private byte _returnAddressLow;
#endif

        private readonly State _state;
        private readonly Stack _stack;
    }
}