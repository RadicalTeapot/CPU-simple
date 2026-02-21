using CPU.components;
using CPU.microcode;

namespace CPU.opcodes
{
    [Opcode(OpcodeBaseCode.STA, OpcodeGroupBaseCode.StoreAndIndirect)]
    internal class STA : BaseOpcode
    {
        public STA(byte instructionByte, State state, Memory memory, Stack stack)
        {
            _state = state;
            _memory = memory;
            _registerIdx = OpcodeHelpers.GetLowRegisterIdx(instructionByte);
#if x16
            SetPhases(Read1, Read2, Write);
#else
            SetPhases(Read1, Write);
#endif
        }

        private MicroPhase Read1()
        {
#if x16
            _addressLow = _memory.ReadByte(_state.GetPC());
#else
            _effectiveAddress = _memory.ReadByte(_state.GetPC());
#endif
            _state.IncrementPC();
            return MicroPhase.MemoryRead;
        }

#if x16
        private MicroPhase Read2()
        {
            var addressHigh = _memory.ReadByte(_state.GetPC());
            _effectiveAddress = ByteConversionHelper.ToUShort(addressHigh, _addressLow);
            _state.IncrementPC();
            return MicroPhase.MemoryRead;
        }
#endif

        private MicroPhase Write()
        {
            _memory.WriteByte(_effectiveAddress, _state.GetRegister(_registerIdx));
            return MicroPhase.MemoryWrite;
        }

#if x16
        private byte _addressLow;
        private ushort _effectiveAddress;
#else
        private byte _effectiveAddress;
#endif
        private readonly byte _registerIdx;
        private readonly State _state;
        private readonly Memory _memory;
    }
}
