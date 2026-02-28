using CPU.components;
using CPU.microcode;

namespace CPU.opcodes
{
    [Opcode(OpcodeBaseCode.LDI, OpcodeGroupBaseCode.Load)]
    internal class LDI : BaseOpcode
    {
        public LDI(byte instructionByte, State state, IBus bus, Stack stack)
        {
            _state = state;
            _bus = bus;
            _registerIdx = OpcodeHelpers.GetDestinationRegisterIdx(instructionByte);
            SetPhases(MicroPhase.FetchOperand, ReadImmediateValue);
        }

        private MicroPhase ReadImmediateValue()
        {
            var value = _bus.ReadByte(_state.GetPC());
            _state.IncrementPC();
            _state.SetRegister(_registerIdx, value);
            return MicroPhase.Done;
        }

        private readonly byte _registerIdx;
        private readonly IBus _bus;
        private readonly State _state;
    }
}