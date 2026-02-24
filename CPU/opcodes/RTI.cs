using CPU.components;
using CPU.microcode;

namespace CPU.opcodes
{
    [Opcode(OpcodeBaseCode.RTI, OpcodeGroupBaseCode.SystemAndJump)]
    internal class RTI : BaseOpcode
    {
        public RTI(byte instructionByte, State state, Memory memory, Stack stack)
        {
            _state = state;
            _stack = stack;
#if x16
            SetPhases(MicroPhase.MemoryRead, PopPCLow, PopPCHigh, ComposePC, PopStatus);
#else
            SetPhases(MicroPhase.MemoryRead, PopPC, PopStatus);
#endif
        }

#if x16
        private MicroPhase PopPCLow()
        {
            _pcLow = _stack.PopByte();
            return MicroPhase.MemoryRead;
        }

        private MicroPhase PopPCHigh()
        {
            _pcHigh = _stack.PopByte();
            return MicroPhase.ValueComposition;
        }

        private MicroPhase ComposePC()
        {
            _state.SetPC(ByteConversionHelper.ToUShort(_pcHigh, _pcLow));
            return MicroPhase.MemoryRead;
        }
#else
        private MicroPhase PopPC()
        {
            var returnAddress = _stack.PopByte();
            _state.SetPC(returnAddress);
            return MicroPhase.MemoryRead;
        }
#endif

        private MicroPhase PopStatus()
        {
            var status = _stack.PopByte();
            _state.SetZeroFlag((status & 0x01) != 0);
            _state.SetCarryFlag((status & 0x02) != 0);
            _state.SetInterruptDisableFlag((status & 0x04) != 0);
            return MicroPhase.Done;
        }

#if x16
        private byte _pcLow;
        private byte _pcHigh;
#endif
        private readonly State _state;
        private readonly Stack _stack;
    }
}
