using CPU.components;
using CPU.microcode;

namespace CPU.opcodes
{
    [Opcode(OpcodeBaseCode.LDA, OpcodeGroupBaseCode.Load)]
    internal class LDA : BaseOpcode
    {
        public LDA(byte instructionByte, State state, Memory memory, Stack stack)
        {
            _state = state;
            _memory = memory;
            _registerIdx = OpcodeHelpers.GetLowRegisterIdx(instructionByte);

#if x16
            SetPhases(Read1, Read2, GetMemoryValue);
#else
            SetPhases(Read1, GetMemoryValue);
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
            _state.SetRegister(_registerIdx, _addressValue);
            return MicroPhase.MemoryRead;
        }

        //public void Tick(ExecutionContext executionContext)
        //{
        //    cpuState.SetRegister(args.LowRegisterIdx, memory.ReadByte(args.AddressValue));
        //}

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