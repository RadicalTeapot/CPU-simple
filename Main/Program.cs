using Assembler;
using CPU;

namespace Main
{
    public class Trace: IProgress<CpuInspector>
    {
        public void Report(CpuInspector value)
        {
            Console.WriteLine($"Cycle: {value.Cycle}, PC: {value.PC:X4}, " +
                $"Last Instruction: {string.Join(' ', value.LastInstruction)}");
            Console.WriteLine($"Registers: R0={value.Registers[0]:X2}, R1={value.Registers[1]:X2}, " +
                $"R2={value.Registers[2]:X2}, R3={value.Registers[3]:X2}");
            Console.WriteLine($"Flags: Z={(value.ZeroFlag ? 1 : 0)}, C={(value.CarryFlag ? 1 : 0)}");
            Console.WriteLine(new string('-', 40));
        }
    }

    public class Program
    {
        static void Main()
        {
            var program = AssembleProgram(PROG_2);
            var cpu = new CPU.CPU(new Config())
            {
                ProgressInspector = new Trace()
            };
            cpu.LoadProgram(program);
            cpu.Run();
        }

        static byte[] AssembleProgram(string[] programLines)
        {
            const int memorySize = 240;
            var program = string.Join(Environment.NewLine, programLines);
            var tokens = new Lexer().Tokenize(program);
            var programNode = Parser.ParseProgram(tokens);
            var emitNodes = new Analyser(memorySize).Run(programNode);
            var outputBytes = new Emitter(memorySize).Emit(emitNodes);
            return outputBytes;
        }

        private static readonly string[] PROG_1 = [
            "LDI R0, #0x01  ; Load 1 into R0",
        "LDI R1, #0x02  ; Load 2 into R1",
        "MOV R2, R0     ; Copy R0 into R2",
        "MOV R3, R1     ; Copy R1 into R3",
        "HLT"
        ];

#if x16
        /// <summary>
        /// Count to 4 in R0 using JCC and CMP
        /// </summary>
        /// <remarks>This is not an efficent way of doing this but it is a good test</remarks>
        private static readonly string[] PROG_2 = [
           "LDI R0, #0x00   ; Accumulator",
       "LDI R1, #0x03   ; Limit",
       "CLC             ; Clear carry flag (for ADI to work correctly when jumping after CMP)",
       "ADI R0, #0x02   ; Increment R0",
       "CMP R0, R1      ; Set carry when R0 >= R1",
       "JCC [#0x04]   ; Jump back to CLC if carry not set",
       "HLT"
        ];
#else
    /// <summary>
    /// Count to 4 in R0 using JCC and CMP
    /// </summary>
    /// <remarks>This is not an efficent way of doing this but it is a good test</remarks>
    private static readonly string[] PROG_2 = [
       "LDI R0, #0x00   ; Accumulator",
       "LDI R1, #0x03   ; Limit",
       "CLC             ; Clear carry flag (for ADI to work correctly when jumping after CMP)",
       "ADI R0, #0x02   ; Increment R0",
       "CMP R0, R1      ; Set carry when R0 >= R1",
       "JCC [#0x04]     ; Jump back to CLC if carry not set",
       "HLT"
    ];
#endif

        /// <summary>
        /// Same as PROG_2 but using labels
        /// </summary>
        private static readonly string[] PROG_3 = [
           "LDI R0, #0x00       ; Accumulator",
       "LDI R1, #0x03       ; Limit",
       "LOOP_START:         ; Label for the start of the loop",
       "CLC                 ; Clear carry flag (for ADI to work correctly when jumping after CMP)",
       "ADI R0, #0x02       ; Increment R0",
       "CMP R0, R1          ; Set carry when R0 >= R1",
       "JCC [LOOP_START]    ; Jump back to CLC if carry not set",
       "HLT"
        ];

        /// <summary>
        /// Test indirect addressing modes
        /// </summary>
        private static readonly string[] PROG_4 = [
            ".text",
        "LDI R0, start",
        "LDX R1, [r0]",
        "LDX R2, [r0 + #0x01]",
        "LDX R3, [r0 + #0x02]",
        "HLT",
        ".data",
        "start: .byte #0x01",
        ".byte #0x02",
        ".byte #0x03",
    ];

        /// <summary>
        /// Test multi text sections and org directive
        /// </summary>
        private static readonly string[] PROG_5 = [
            ".text",
        "LDI R0, #0x00",
        ".data",
        "myData: .byte #0x42",
        ".text",
        "LDA R1, [myData]",
        ".data",
        ".org #0x10 moreData: .byte #0x99",
        ".text",
        "LDA R2, [moreData]",
        "HLT"
        ];
    }
}