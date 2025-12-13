class Program
{
    static void Main(string[] args)
    {
        var cpu = new CPU.CPU();
        cpu.LoadProgram(
        [
            0x20, 0x01, // LDI R0, 0x01
            0x24, 0x02, // LDI R1, 0x02
            0x18, // MOV R0, R2
            0x1D, // ADD R1, R3
            0xFF
        ]);
        cpu.Run(traceEnabled: true);
    }
}