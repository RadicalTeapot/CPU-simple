class Program
{
    static void Main(string[] args)
    {
        var cpu = new CPU.CPU(new CPU.Config());
        cpu.LoadProgram(PROG_2);
        cpu.Run(traceEnabled: true);
    }

    private static readonly byte[] PROG_1 = [
        0x10, 0x01, // LDI R0, 0x01
        0x11, 0x02, // LDI R1, 0x02
        0x32,       // MOV R0, R2
        0x37,       // MOV R1, R3
        0x01        // HLT
    ];

#if x16
    /// <summary>
    /// Count to 4 in R0 using JCC and CMP
    /// </summary>
    /// <remarks>This is not an efficent way of doing this but it is a good test</remarks>
    private static readonly byte[] PROG_2 = [
        0x10, 0x00,         // LDI R0, 0x00     ;accumulator
        0x11, 0x03,         // LDI R1, 0x03     ;limit
        0x02,               // CLC              ;clear carry flag (for ADI to work correctly when jumping after CMP)
        0x40, 0x02,         // ADI R0, 0x02     ;Increment R0
        0x94,               // CMP R1, R0       ;Set carry when R0 >= R1
        0x0A, 0x04, 0x00,   // JCC 0x0004       ;Jump back to CLC if carry not set (little-endian)
        0x01                // HLT
    ];
#else
    /// <summary>
    /// Count to 4 in R0 using JCC and CMP
    /// </summary>
    /// <remarks>This is not an efficent way of doing this but it is a good test</remarks>
    private static readonly byte[] PROG_2 = [
        0x10, 0x00, // LDI R0, 0x00     ;accumulator
        0x11, 0x03, // LDI R1, 0x03     ;limit
        0x02,       // CLC              ;clear carry flag (for ADI to work correctly when jumping after CMP)
        0x40, 0x02, // ADI R0, 0x02     ;Increment R0
        0x94,       // CMP R1, R0       ;Set carry when R0 >= R1
        0x0A, 0x04, // JCC 0x04         ;Jump back to CLC if carry not set
        0x01        // HLT
    ];
#endif
}