using CPU.components;
using CPU.microcode;

namespace CPU.opcodes
{
    [Opcode(OpcodeBaseCode.ORA, OpcodeGroupBaseCode.SingleRegisterLogicOne)]
    internal class ORA : BaseOpcode
    {
        public ORA(byte instructionByte, State state, Memory memory, Stack stack)
        {
            _state = state;
            _memory = memory;
            _registerIdx = OpcodeHelpers.GetDestinationRegisterIdx(instructionByte);
#if x16
            SetPhases(MicroPhase.MemoryRead, Read1, Read2, GetMemoryValue, AluOp);
#else
            SetPhases(MicroPhase.MemoryRead, Read1, GetMemoryValue, AluOp);
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

        private MicroPhase GetMemoryValue()
        {
            _addressValue = _memory.ReadByte(_effectiveAddress);
            return MicroPhase.AluOp;
        }

        private MicroPhase AluOp()
        {
            var registerValue = _state.GetRegister(_registerIdx);
            var value = (byte)(registerValue | _addressValue);
            _state.SetRegister(_registerIdx, value);
            _state.SetZeroFlag(value == 0);
            return MicroPhase.Done;
        }

#if x16
        private byte _addressLow;
        private ushort _effectiveAddress;
#else
        private byte _effectiveAddress;
#endif
        private byte _addressValue;
        private readonly byte _registerIdx;
        private readonly State _state;
        private readonly Memory _memory;
    }
}
