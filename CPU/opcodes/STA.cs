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
            _registerIdx = OpcodeHelpers.GetDestinationRegisterIdx(instructionByte);
#if x16
            SetPhases(MicroPhase.FetchOperand16Low, Read1, Read2, Write);
#else
            SetPhases(MicroPhase.FetchOperand, Read1, Write);
#endif
        }

        private MicroPhase Read1()
        {
#if x16
            _addressLow = _memory.ReadByte(_state.GetPC());
            _state.IncrementPC();
            return MicroPhase.FetchOperand16High;
#else
            _effectiveAddress = _memory.ReadByte(_state.GetPC());
            _state.IncrementPC();
            return MicroPhase.MemoryWrite;
#endif
        }

#if x16
        private MicroPhase Read2()
        {
            var addressHigh = _memory.ReadByte(_state.GetPC());
            _effectiveAddress = ByteConversionHelper.ToUShort(addressHigh, _addressLow);
            _state.IncrementPC();
            return MicroPhase.MemoryWrite;
        }
#endif

        private MicroPhase Write()
        {
            _memory.WriteByte(_effectiveAddress, _state.GetRegister(_registerIdx));
            return MicroPhase.Done;
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
