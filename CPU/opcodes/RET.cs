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
            SetPhases(Read1, Read2);
#else
            SetPhases(Read1);
#endif
        }

        public MicroPhase Read1()
        {
#if x16
            _returnAddressLow = _stack.PopByte();
            _state.IncrementPC();
#else
            var returnAddress = _stack.PopByte(); 
            _state.SetPC(returnAddress);
#endif
            return MicroPhase.MemoryRead;
        }

#if x16
        public MicroPhase Read2()
        {
            var returnAddressHigh = _stack.PopByte();
            var returnAddress = ByteConversionHelper.ToUShort(returnAddressHigh, _returnAddressLow);
            _state.SetPCHigh(returnAddress);
            return MicroPhase.MemoryRead;
        }

        private byte _returnAddressLow;
#endif

        private readonly State _state;
        private readonly Stack _stack;
    }
}