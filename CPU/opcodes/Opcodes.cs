namespace CPU.opcodes
{
    internal class NOP: BaseOpcode
    {
        public NOP() : base(1, Opcode.NOP)
        {
        }

        public override void Execute(State state, byte[] args, Trace trace)
        {
            // No operation
            trace.InstructionName = nameof(NOP);
            trace.Args = "-";
        }
    }

    internal class MOV: BaseOpcode
    {
        public MOV() : base(1, Opcode.MOV)
        {
        }

        public override void Execute(State state, byte[] args, Trace trace)
        {
            ValidateArgs(args, nameof(MOV));

            var destReg = (byte)((args[0] >> 2) & 0x03);
            var srcReg = (byte)(args[0] & 0x03);

            trace.InstructionName = nameof(MOV);
            trace.Args = $"RD: {destReg}, RS: {srcReg}";
            trace.RBefore = [state.GetRegister(destReg), state.GetRegister(srcReg)];
            
            var value = state.GetRegister(srcReg);
            state.SetRegister(destReg, value);

            trace.RAfter = [state.GetRegister(destReg), state.GetRegister(srcReg)];
        }
    }

    internal class LDI: BaseOpcode
    {
        public LDI() : base(2, Opcode.LDI)
        {
        }
        public override void Execute(State state, byte[] args, Trace trace)
        {
            ValidateArgs(args, nameof(LDI));

            var destReg = (byte)((args[0] >> 2) & 0x03);
            var immediateValue = args[1];

            trace.InstructionName = nameof(LDI);
            trace.RBefore = [state.GetRegister(destReg)];
            trace.Args = $"RD: {destReg}, IMM: {immediateValue}";
            state.SetRegister(destReg, immediateValue);
            trace.RAfter = [state.GetRegister(destReg)];
        }
    }

    internal class HLT: BaseOpcode
    {
        public HLT() : base(1, Opcode.HLT)
        {
        }
        public override void Execute(State state, byte[] args, Trace trace)
        {
            ValidateArgs(args, nameof(HLT));
            trace.InstructionName = nameof(HLT);
            trace.Args = "-";
            throw new OpcodeException.HaltException();
        }
    }
}
