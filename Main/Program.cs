class Program
{
    static void Main(string[] args)
    {
        var cpu = new CPU.CPU();
        cpu.LoadProgram(
        [
            0x10, 0x01, // LDI R0, 0x01
            0x11, 0x02, // LDI R1, 0x02
            0x32,       // MOV R0, R2
            0x37,       // MOV R1, R3
            0x01        // HLT
        ]);
        cpu.Run(traceEnabled: true);
    }
}