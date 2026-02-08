namespace LanguageServer;

public static class InstructionDescriptions
{
    public static readonly Dictionary<string, (string Description, string Syntax)> Entries = new(StringComparer.OrdinalIgnoreCase)
    {
        // No operand
        ["nop"] = ("No operation. Does nothing for one cycle.", "nop"),
        ["hlt"] = ("Halt. Stops the CPU.", "hlt"),
        ["clc"] = ("Clear carry flag.", "clc"),
        ["sec"] = ("Set carry flag.", "sec"),
        ["clz"] = ("Clear zero flag.", "clz"),
        ["sez"] = ("Set zero flag.", "sez"),
        ["ret"] = ("Return from subroutine. Pops the return address from the stack and jumps to it.", "ret"),

        // Single memory address (jumps/calls)
        ["jmp"] = ("Unconditional jump to address.", "jmp address"),
        ["jcc"] = ("Jump if carry clear.", "jcc address"),
        ["jcs"] = ("Jump if carry set.", "jcs address"),
        ["jzc"] = ("Jump if zero clear (not zero).", "jzc address"),
        ["jzs"] = ("Jump if zero set.", "jzs address"),
        ["cal"] = ("Call subroutine. Pushes return address onto stack and jumps to address.", "cal address"),

        // Single register
        ["pop"] = ("Pop value from stack into register.", "pop rN"),
        ["pek"] = ("Peek at top of stack into register without popping.", "pek rN"),
        ["psh"] = ("Push register value onto stack.", "psh rN"),
        ["lsh"] = ("Logical shift left register by one bit.", "lsh rN"),
        ["rsh"] = ("Logical shift right register by one bit.", "rsh rN"),
        ["lrt"] = ("Left rotate register by one bit.", "lrt rN"),
        ["rrt"] = ("Right rotate register by one bit.", "rrt rN"),
        ["inc"] = ("Increment register by one.", "inc rN"),
        ["dec"] = ("Decrement register by one.", "dec rN"),

        // Register + immediate
        ["ldi"] = ("Load immediate value into register.", "ldi rN, #value"),
        ["adi"] = ("Add immediate value to register.", "adi rN, #value"),
        ["sbi"] = ("Subtract immediate value from register.", "sbi rN, #value"),
        ["cpi"] = ("Compare register with immediate value. Sets flags.", "cpi rN, #value"),
        ["ani"] = ("Bitwise AND register with immediate value.", "ani rN, #value"),
        ["ori"] = ("Bitwise OR register with immediate value.", "ori rN, #value"),
        ["xri"] = ("Bitwise XOR register with immediate value.", "xri rN, #value"),
        ["bti"] = ("Bit test register with immediate mask. Sets zero flag if result is zero.", "bti rN, #mask"),

        // Register + memory address
        ["lda"] = ("Load value from memory address into register.", "lda rN, address"),
        ["sta"] = ("Store register value to memory address.", "sta rN, address"),
        ["ada"] = ("Add value at memory address to register.", "ada rN, address"),
        ["sba"] = ("Subtract value at memory address from register.", "sba rN, address"),
        ["cpa"] = ("Compare register with value at memory address. Sets flags.", "cpa rN, address"),
        ["ana"] = ("Bitwise AND register with value at memory address.", "ana rN, address"),
        ["ora"] = ("Bitwise OR register with value at memory address.", "ora rN, address"),
        ["xra"] = ("Bitwise XOR register with value at memory address.", "xra rN, address"),
        ["bta"] = ("Bit test register with value at memory address. Sets zero flag if result is zero.", "bta rN, address"),
        ["ldx"] = ("Load value from memory address indexed by register into register.", "ldx rN, [address + rM]"),
        ["stx"] = ("Store register value to memory address indexed by register.", "stx rN, [address + rM]"),

        // Two registers
        ["mov"] = ("Copy value from source register to destination register.", "mov rDst, rSrc"),
        ["add"] = ("Add source register to destination register.", "add rDst, rSrc"),
        ["sub"] = ("Subtract source register from destination register.", "sub rDst, rSrc"),
        ["cmp"] = ("Compare two registers. Sets flags.", "cmp rA, rB"),
        ["and"] = ("Bitwise AND two registers, result in destination.", "and rDst, rSrc"),
        ["or"] = ("Bitwise OR two registers, result in destination.", "or rDst, rSrc"),
        ["xor"] = ("Bitwise XOR two registers, result in destination.", "xor rDst, rSrc"),
    };
}
