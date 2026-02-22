using CPU.components;
using CPU.microcode;

namespace CPU.opcodes
{
    [Opcode(OpcodeBaseCode.ADI, OpcodeGroupBaseCode.SingleRegisterALU)]
    internal class ADI : BaseOpcode
    {
        public ADI(byte instructionByte, State state, Memory memory, Stack stack)
        {
            _state = state;
            _memory = memory;
            _registerIdx = OpcodeHelpers.GetDestinationRegisterIdx(instructionByte);
            SetPhases(MicroPhase.FetchOperand, ReadImmediateValue, AluOp);
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
            var result = registerValue + _immediateValue + _state.GetCarryFlagAsInt();
            _state.SetRegister(_registerIdx, (byte)result); // Wrap around on overflow
            _state.SetCarryFlag(result > 0xFF);
            _state.SetZeroFlag(result == 0);
            return MicroPhase.Done;
        }

        private byte _immediateValue;
        private readonly byte _registerIdx;
        private readonly State _state;
        private readonly Memory _memory;
    }
}