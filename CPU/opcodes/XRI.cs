using CPU.components;
using CPU.microcode;

namespace CPU.opcodes
{
    [Opcode(OpcodeBaseCode.XRI, OpcodeGroupBaseCode.SingleRegisterLogicOne)]
    internal class XRI : BaseOpcode
    {
        public XRI(byte instructionByte, State state, Memory memory, Stack stack)
        {
            _state = state;
            _memory = memory;
            _registerIdx = OpcodeHelpers.GetDestinationRegisterIdx(instructionByte);
            SetPhases(MicroPhase.MemoryRead, ReadImmediateValue, AluOp);
        }

        private MicroPhase ReadImmediateValue()
        {
            _immediateValue = _memory.ReadByte(_state.GetPC());
            _state.IncrementPC();
            return MicroPhase.AluOp;
        }

        private MicroPhase AluOp()
        {
            var registerValue = _state.GetRegister(_registerIdx);
            var value = (byte)(registerValue ^ _immediateValue);
            _state.SetRegister(_registerIdx, value);
            _state.SetZeroFlag(value == 0);
            return MicroPhase.Done;
        }

        private byte _immediateValue;
        private readonly byte _registerIdx;
        private readonly State _state;
        private readonly Memory _memory;
    }
}
