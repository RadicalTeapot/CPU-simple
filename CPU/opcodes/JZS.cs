using CPU.components;
using CPU.microcode;

namespace CPU.opcodes
{
    [Opcode(OpcodeBaseCode.JZS, OpcodeGroupBaseCode.SystemAndJump)]
    internal class JZS : BaseOpcode
    {
        public JZS(byte instructionByte, State state, Memory memory, Stack stack)
        {
            _state = state;
            _memory = memory;
#if x16
            SetPhases(MicroPhase.FetchOperand16Low, Read1, Read2, ComposeAddress);
#else
            SetPhases(MicroPhase.FetchOperand, Read1);
#endif
        }

        private MicroPhase Read1()
        {
#if x16
            _addressLow = _memory.ReadByte(_state.GetPC());
            _state.IncrementPC();
            return MicroPhase.FetchOperand16High;
#else
            var address = _memory.ReadByte(_state.GetPC());
            _state.IncrementPC();
            if (_state.Z)
                _state.SetPC(address);
            return MicroPhase.Done;
#endif
        }

#if x16
        private MicroPhase Read2()
        {
            _addressHigh = _memory.ReadByte(_state.GetPC());
            _state.IncrementPC();
            return MicroPhase.ValueComposition;
        }

        private MicroPhase ComposeAddress()
        {
            var address = ByteConversionHelper.ToUShort(_addressHigh, _addressLow);
            if (_state.Z)
                _state.SetPC(address);
            return MicroPhase.Done;
        }

        private byte _addressLow;
        private byte _addressHigh;
#endif

        private readonly State _state;
        private readonly Memory _memory;
    }
}
