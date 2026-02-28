using CPU.components;
using CPU.microcode;

namespace CPU.opcodes
{
    [Opcode(OpcodeBaseCode.LDA, OpcodeGroupBaseCode.Load)]
    internal class LDA : BaseOpcode
    {
        public LDA(byte instructionByte, State state, IBus bus, Stack stack)
        {
            _state = state;
            _bus = bus;
            _registerIdx = OpcodeHelpers.GetDestinationRegisterIdx(instructionByte);

#if x16
            SetPhases(MicroPhase.FetchOperand16Low, Read1, Read2, ComposeAddress, GetMemoryValue);
#else
            SetPhases(MicroPhase.FetchOperand, Read1, GetMemoryValue);
#endif
        }

        private MicroPhase Read1()
        {
#if x16
            _addressLow = _bus.ReadByte(_state.GetPC());
            _state.IncrementPC();
            return MicroPhase.FetchOperand16High;
#else
            _effectiveAddress = _bus.ReadByte(_state.GetPC());
            _state.IncrementPC();
            return MicroPhase.MemoryRead;
#endif
        }

#if x16
        private MicroPhase Read2()
        {
            _addressHigh = _bus.ReadByte(_state.GetPC());
            _state.IncrementPC();
            return MicroPhase.ValueComposition;
        }

        private MicroPhase ComposeAddress()
        {
            _effectiveAddress = ByteConversionHelper.ToUShort(_addressHigh, _addressLow);
            return MicroPhase.MemoryRead;
        }
#endif

        private MicroPhase GetMemoryValue()
        {
            _addressValue = _bus.ReadByte(_effectiveAddress);
            _state.SetRegister(_registerIdx, _addressValue);
            return MicroPhase.Done;
        }

        //public void Tick(ExecutionContext executionContext)
        //{
        //    cpuState.SetRegister(args.LowRegisterIdx, memory.ReadByte(args.AddressValue));
        //}

#if x16
        private byte _addressLow;
        private byte _addressHigh;
        private ushort _effectiveAddress;
#else
        private byte _effectiveAddress;
#endif
        private byte _addressValue;
        private readonly byte _registerIdx;
        private readonly State _state;
        private readonly IBus _bus;

    }
}