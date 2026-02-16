using CPU.components;
using CPU.microcode;

namespace CPU.opcodes
{
    [Opcode(OpcodeBaseCode.DEC, OpcodeGroupBaseCode.SingleRegisterLogicOne, RegisterArgsCount.One, OperandType.None)]
    internal class DEC : IOpcode
    {
        public DEC(byte instructionByte, State state, Memory memory, Stack stack)
        {
            _registerIdx = OpcodeHelpers.GetLowRegisterIdx(instructionByte);
            _state = state;
            _phases = [Phase1, () => MicroPhase.Done];
        }

        public MicroPhase Tick(int phaseCount)
        {
            return _phases[phaseCount].Invoke();
        }

        private MicroPhase Phase1()
        {
            var registerValue = _state.GetRegister(_registerIdx);
            var newValue = (byte)(registerValue - 1);
            _state.SetRegister(_registerIdx, newValue);
            _state.SetZeroFlag(newValue == 0);
            return MicroPhase.AluOp;
        }

        private readonly byte _registerIdx;
        private readonly State _state;
        private readonly Func<MicroPhase>[] _phases;
    }
}
