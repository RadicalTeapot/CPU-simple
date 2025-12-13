namespace CPU
{
    public class Trace
    {
        public byte PcBefore;
        public byte PcAfter;
        public byte Instruction;
        public string InstructionName = "";
        public string Args = "";
        public byte[] RBefore = [];
        public byte[] RAfter = [];
        public byte MemoryBefore;
        public byte MemoryAfter;

        public void Dump()
        {
            Console.WriteLine("=== Trace Info ===");
            Console.WriteLine($"Instruction: {InstructionName} ({Instruction:X2}) Args: {Args}");
            Console.WriteLine($"PC before: {PcBefore:X2} after: {PcAfter:X2}");
            for (int i = 0; i < RBefore.Length; i++)
            {
                var regType = i == 0 ? "RD" : "RS";
                Console.WriteLine($"{regType} before: {RBefore[i]:X2} after: {RAfter[i]:X2}");
            }
            Console.WriteLine($"Memory before: {MemoryBefore:X2} after: {MemoryAfter:X2}");
            Console.WriteLine("===================");
            Console.WriteLine();
        }
    }
}
