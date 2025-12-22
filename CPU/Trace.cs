using CPU.opcodes;

namespace CPU
{
    internal class Trace
    {
        public ushort PcBefore; // ushort for compatibility with 16-bit architectures
        public ushort PcAfter;  // ushort for compatibility with 16-bit architectures
        public byte Instruction;
        public string InstructionName = "";
        public string Args = "";
        public byte[] RBefore = [];
        public byte[] RAfter = [];
        public byte MemoryBefore;
        public byte MemoryAfter;

        public Trace(ushort pcBefore, ushort pcAfter, DecodedInstruction decoded)
        {
            PcBefore = pcBefore;
            PcAfter = pcAfter;
            Instruction = decoded.RawInstruction;
            InstructionName = decoded.Metadata.BaseCode.ToString();
            Args = FormatArgs(decoded);
            //RBefore = GetRegisterValues(decoded, before: true);
            //RAfter = GetRegisterValues(decoded, before: false);
            //MemoryBefore = GetMemoryValue(decoded, before: true);
            //MemoryAfter = GetMemoryValue(decoded, before: false);
        }


        /// <summary>
        /// Formats the decoded arguments for trace output.
        /// </summary>
        private static string FormatArgs(DecodedInstruction decoded)
        {
            var args = decoded.Args;
            var meta = decoded.Metadata;

            var parts = new List<string>();

            if (meta.RegisterArgsCount == RegisterArgsCount.Two)
            {
                parts.Add($"RD: {args.LowRegisterIdx}");
                parts.Add($"RS: {args.HighRegisterIdx}");
            }
            else if (meta.RegisterArgsCount == RegisterArgsCount.One)
            {
                parts.Add($"RD: {args.LowRegisterIdx}");
            }

            if (meta.OperandType == OperandType.Address)
            {
                parts.Add($"ADDR: {args.AddressValue:X2}");
            }
            else if (meta.OperandType == OperandType.Immediate)
            {
                parts.Add($"IMM: {args.ImmediateValue:X2}");
            }

            return parts.Count > 0 ? string.Join(", ", parts) : "-";
        }

        public void Dump()
        {
            Console.WriteLine("=== Trace Info ===");
            Console.WriteLine($"Instruction: {InstructionName} ({Instruction:X2}) Args: {Args}");
            Console.WriteLine($"PC before: {PcBefore:X2} after: {PcAfter:X2}");
            //for (int i = 0; i < RBefore.Length; i++)
            //{
            //    var regType = i == 0 ? "RD" : "RS";
            //    Console.WriteLine($"{regType} before: {RBefore[i]:X2} after: {RAfter[i]:X2}");
            //}
            //Console.WriteLine($"Memory before: {MemoryBefore:X2} after: {MemoryAfter:X2}");
            Console.WriteLine("===================");
            Console.WriteLine();
        }
    }
}
