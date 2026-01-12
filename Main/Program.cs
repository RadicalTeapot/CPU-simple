using Assembler;

class Program
{
    static void Main()
    {
        var program = AssembleProgram(PROG_3);
        var cpu = new CPU.CPU(new CPU.Config());
        cpu.LoadProgram(program);
        cpu.Run(traceEnabled: true);
    }

    static byte[] AssembleProgram(string[] programLines)
    {
        const int memorySize = 240;
        var program = string.Join(Environment.NewLine, programLines);
        var tokens = new Lexer().Tokenize(program);
        var programNode = Parser.ParseProgram(tokens);
        var emitNodes = new Analyser().Run(programNode); // TODO Analyser should also have a memorySize parameter
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
       "JCC [#0x0004]   ; Jump back to CLC if carry not set",
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
}